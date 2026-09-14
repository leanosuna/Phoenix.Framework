using Phoenix.Framework.Rendering;
using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Geometry.Model.Animations;
using Silk.NET.Assimp;
using System.Numerics;
using System.Runtime.InteropServices;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpScene = Silk.NET.Assimp.Scene;
using ModelAnimation = Phoenix.Framework.Rendering.Geometry.Model.Animations.Animation;
using File = System.IO.File;
using Phoenix.Framework.AssetImport.Processing;
using Phoenix;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Imports 3D models and skeletal animations using Assimp 6.0.1, handles FBX helper chain folding,
/// precomputes animation keyframes, and produces binary disk caches.
/// </summary>
public static unsafe class AssimpImporter
{
    private const int MaxBoneInfluence = 4;

    /// <summary>
    /// Obtains an Assimp API instance with Linux shared library resolution fallback.
    /// </summary>
    public static Assimp GetAssimpApi()
    {
        if (OperatingSystem.IsLinux())
        {
            var candidates = new List<string>();
            var path6 = Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libassimp.so.6");
            var path5 = Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libassimp.so.5");
            if (File.Exists(path6)) candidates.Add(path6);
            if (File.Exists(path5)) candidates.Add(path5);
            candidates.Add("libassimp.so.6");
            candidates.Add("libassimp.so.5");
            candidates.Add("libassimp.so");
            return new Assimp(Assimp.CreateDefaultContext(candidates.ToArray()));
        }
        return Assimp.GetApi();
    }

    /// <summary>
    /// Imports a model from source path, processes geometry and animations, writes a .bin cache, and loads GPU buffers.
    /// Returns the created model along with any extracted in-memory texture payloads for asynchronous background processing.
    /// </summary>
    public static (Model Model, List<EmbeddedTexturePayload> EmbeddedTextures) ImportAndCache(Graphics graphics, string sourcePath, string cachePath, ModelLoadOptions options)
    {
        var assimp = GetAssimpApi();
        var scene = assimp.ImportFile(sourcePath, options.AssimpFlagsMask);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = assimp.GetErrorStringS();
            throw new InvalidOperationException($"Failed to import model '{sourcePath}': {error}");
        }

        var (texNames, payloads, materialDiffuseIndices, materialNormalIndices) = ExtractEmbeddedTextures(assimp, scene, sourcePath, options);

        var boneInfoMap = new Dictionary<string, BoneInfo>(StringComparer.OrdinalIgnoreCase);
        var parts = new List<ProcessedPart>();

        ProcessNode(scene, scene->MRootNode, Matrix4x4.Identity, boneInfoMap, options, parts, materialDiffuseIndices, materialNormalIndices);

        Matrix4x4 inverseGlobalTransform = Matrix4x4.Identity;
        List<AnimatorNode> animatorNodes = [];
        List<ModelAnimation> animations = [];

        if (options.IsAnimated)
        {
            var animFiles = options.AnimationFiles != null && options.AnimationFiles.Count > 0
                ? options.AnimationFiles
                : [sourcePath];

            bool hierarchySet = false;

            for (int a = 0; a < animFiles.Count; a++)
            {
                var animPath = animFiles[a];
                var animScene = string.Equals(animPath, sourcePath, StringComparison.OrdinalIgnoreCase)
                    ? scene
                    : assimp.ImportFile(animPath, 0);

                if (animScene == null || animScene->MRootNode == null)
                {
                    var error = assimp.GetErrorStringS();
                    throw new InvalidOperationException($"Failed to load animation '{animPath}': {error}");
                }

                if (!hierarchySet)
                {
                    var globalTransform = Matrix4x4.Transpose(animScene->MRootNode->MTransformation);
                    inverseGlobalTransform = Matrix4x4.Invert(globalTransform, out var inv) ? inv : Matrix4x4.Identity;

                    var rootFolded = ReadHierarchy(animScene->MRootNode, boneInfoMap);
                    animatorNodes.Clear();
                    FlattenHierarchy(rootFolded, -1, animatorNodes, boneInfoMap);
                    hierarchySet = true;
                }

                if (animScene->MNumAnimations > 0)
                {
                    var animName = Path.GetFileNameWithoutExtension(animPath);
                    var animation = LoadAnimation(animName, animScene, boneInfoMap);
                    animation.Precompute(animatorNodes, inverseGlobalTransform);
                    animations.Add(animation);
                }
            }
        }

        WriteBinary(cachePath, options, parts, boneInfoMap, animatorNodes, animations, inverseGlobalTransform, texNames);

        var model = BinaryModelReader.Read(graphics, cachePath);
        return (model, payloads);
    }

    private static void ProcessNode(AssimpScene* scene, AssimpNode* node, Matrix4x4 parentTransform,
        Dictionary<string, BoneInfo> boneInfoMap, ModelLoadOptions options, List<ProcessedPart> parts,
        Dictionary<int, int> materialDiffuseIndices, Dictionary<int, int> materialNormalIndices)
    {
        var nTransform = node->MTransformation;
        Matrix4x4 currentTransform = parentTransform * nTransform;
        Matrix4x4 absoluteTransform = Matrix4x4.Transpose(currentTransform);

        var meshes = new List<ProcessedMesh>();
        for (int i = 0; i < node->MNumMeshes; i++)
        {
            var assimpMesh = scene->MMeshes[node->MMeshes[i]];
            var mesh = ProcessMesh(assimpMesh, currentTransform, absoluteTransform, boneInfoMap, options, materialDiffuseIndices, materialNormalIndices);
            meshes.Add(mesh);
        }

        string name = !string.IsNullOrEmpty(node->MName) ? node->MName : "part";
        if (meshes.Count > 0)
        {
            parts.Add(new ProcessedPart { Name = name, Meshes = meshes });
        }

        for (int i = 0; i < node->MNumChildren; i++)
        {
            ProcessNode(scene, node->MChildren[i], currentTransform, boneInfoMap, options, parts, materialDiffuseIndices, materialNormalIndices);
        }
    }

    private static ProcessedMesh ProcessMesh(AssimpMesh* mesh, Matrix4x4 currentTransform, Matrix4x4 absoluteTransform,
        Dictionary<string, BoneInfo> boneInfoMap, ModelLoadOptions options,
        Dictionary<int, int> materialDiffuseIndices, Dictionary<int, int> materialNormalIndices)
    {
        var vertices = new List<ModelVertex>((int)mesh->MNumVertices);
        var indices = new List<uint>((int)mesh->MNumFaces * 3);

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            ModelVertex vertex = new()
            {
                BoneIds = new Vector4(-1, -1, -1, -1),
                Weights = Vector4.Zero,
                Position = mesh->MVertices[i]
            };

            if (mesh->MNormals != null)
                vertex.Normal = mesh->MNormals[i];
            if (mesh->MTangents != null)
                vertex.Tangent = mesh->MTangents[i];
            if (mesh->MBitangents != null)
                vertex.Bitangent = mesh->MBitangents[i];
            if (mesh->MTextureCoords[0] != null)
            {
                var tc = mesh->MTextureCoords[0][i];
                vertex.TexCoords = new Vector2(tc.X, tc.Y);
            }

            vertices.Add(vertex);
        }

        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            for (uint j = 0; j < face.MNumIndices; j++)
            {
                indices.Add(face.MIndices[j]);
            }
        }

        if (options.IsAnimated)
        {
            ExtractBoneWeights(vertices, mesh, currentTransform, boneInfoMap);
        }

        string meshName = !string.IsNullOrEmpty(mesh->MName) ? mesh->MName : "mesh";
        int matIdx = (int)mesh->MMaterialIndex;
        int diffuseIdx = materialDiffuseIndices.TryGetValue(matIdx, out int dIdx) ? dIdx : matIdx;
        int normalIdx = materialNormalIndices.TryGetValue(matIdx, out int nIdx) ? nIdx : -1;

        return new ProcessedMesh
        {
            Name = meshName,
            MaterialIndex = diffuseIdx,
            NormalIndex = normalIdx,
            Transform = absoluteTransform,
            Indices = indices.ToArray(),
            Vertices = vertices.ToArray()
        };
    }

    private static void ExtractBoneWeights(List<ModelVertex> vertices, AssimpMesh* mesh, Matrix4x4 currentTransform,
        Dictionary<string, BoneInfo> boneInfoMap)
    {
        var vertexInfluences = new Dictionary<int, List<(int BoneId, float Weight)>>();

        for (int boneID = 0; boneID < mesh->MNumBones; boneID++)
        {
            string boneName = mesh->MBones[boneID]->MName;
            var offset = mesh->MBones[boneID]->MOffsetMatrix;

            if (Matrix4x4.Invert(currentTransform, out var invMeshTransform))
            {
                offset = offset * invMeshTransform;
            }

            var weights = mesh->MBones[boneID]->MWeights;
            var numWeights = mesh->MBones[boneID]->MNumWeights;

            if (!boneInfoMap.TryGetValue(boneName, out var boneInfo))
            {
                int trueBoneId = boneInfoMap.Count;
                boneInfo = new BoneInfo(trueBoneId, offset);
                boneInfoMap.Add(boneName, boneInfo);
            }

            for (int wi = 0; wi < numWeights; wi++)
            {
                int vertexId = (int)weights[wi].MVertexId;
                float weight = weights[wi].MWeight;

                if (!vertexInfluences.TryGetValue(vertexId, out var list))
                {
                    list = [];
                    vertexInfluences[vertexId] = list;
                }

                list.Add((boneInfo.ID, weight));
            }
        }

        foreach (var kvp in vertexInfluences)
        {
            int vertexId = kvp.Key;
            var influences = kvp.Value;

            while (influences.Count < MaxBoneInfluence)
            {
                influences.Add((-1, 0.0f));
            }

            var vertex = vertices[vertexId];
            vertex.BoneIds = new Vector4(influences[0].BoneId, influences[1].BoneId, influences[2].BoneId, influences[3].BoneId);
            vertex.Weights = new Vector4(influences[0].Weight, influences[1].Weight, influences[2].Weight, influences[3].Weight);
            vertices[vertexId] = vertex;
        }
    }

    private static ModelBoneHierarchyNode ReadHierarchy(AssimpNode* node, Dictionary<string, BoneInfo> boneInfoMap)
    {
        if (TryCollectChainThatEndsInBone(node, boneInfoMap, out var foldedTransform, out AssimpNode* boneNode, out string boneName))
        {
            if (boneInfoMap.TryGetValue(boneName, out var info))
            {
                var children = new List<ModelBoneHierarchyNode>();
                for (int i = 0; i < boneNode->MNumChildren; i++)
                {
                    var child = ReadHierarchy(boneNode->MChildren[i], boneInfoMap);
                    if (child != null)
                        children.Add(child);
                }
                return new ModelBoneHierarchyNode(boneName, foldedTransform, children, info.Offset);
            }
        }

        string name = node->MName;
        var nodeTransform = node->MTransformation;
        var nodeChildren = new List<ModelBoneHierarchyNode>();

        for (int i = 0; i < node->MNumChildren; i++)
        {
            var child = ReadHierarchy(node->MChildren[i], boneInfoMap);
            if (child != null)
                nodeChildren.Add(child);
        }

        if (boneInfoMap.TryGetValue(name, out var directInfo))
        {
            return new ModelBoneHierarchyNode(name, nodeTransform, nodeChildren, directInfo.Offset);
        }
        else
        {
            if (nodeChildren.Count == 0)
                return null!;

            return new ModelBoneHierarchyNode(name, nodeTransform, nodeChildren);
        }
    }

    private static bool TryCollectChainThatEndsInBone(AssimpNode* start, Dictionary<string, BoneInfo> boneInfoMap,
        out Matrix4x4 accumulated, out AssimpNode* boneNodeOut, out string boneNameOut)
    {
        accumulated = Matrix4x4.Identity;
        AssimpNode* cur = start;
        boneNodeOut = null;
        boneNameOut = null!;

        while (cur != null)
        {
            var t = cur->MTransformation;
            accumulated = accumulated * t;
            string curName = cur->MName;

            if (boneInfoMap.ContainsKey(curName))
            {
                boneNodeOut = cur;
                boneNameOut = curName;
                return true;
            }

            if (cur->MNumChildren != 1)
            {
                boneNodeOut = cur;
                return false;
            }

            cur = cur->MChildren[0];
        }

        return false;
    }

    private static void FlattenHierarchy(ModelBoneHierarchyNode node, int parentID, List<AnimatorNode> animatorNodes,
        Dictionary<string, BoneInfo> boneInfoMap, int level = -1)
    {
        if (node == null)
            return;

        int currentIndex = animatorNodes.Count;

        if (node.IsBone)
        {
            if (boneInfoMap.TryGetValue(node.Name, out var info))
            {
                var an = new AnimatorNode(node.Transform, parentID, info.ID, info.Offset)
                {
                    Name = TrimBoneName(node.Name),
                    Level = level
                };
                animatorNodes.Add(an);
            }
        }
        else
        {
            var an = new AnimatorNode(node.Transform, parentID)
            {
                Name = TrimBoneName(node.Name),
                Level = level
            };
            animatorNodes.Add(an);
        }

        for (int i = 0; i < node.Children.Count; i++)
        {
            FlattenHierarchy(node.Children[i], currentIndex, animatorNodes, boneInfoMap, level + 1);
        }
    }

    public static string TrimBoneName(string name)
    {
        return name.StartsWith("mixamorig:") ? name[10..] : name;
    }

    private static bool MatchBoneInfo(string nodeName, Dictionary<string, BoneInfo> boneInfoMap, out BoneInfo info)
    {
        var trimmedNodeName = TrimBoneName(nodeName);
        int fbxIdx = trimmedNodeName.IndexOf("_$AssimpFbx$_", StringComparison.OrdinalIgnoreCase);
        if (fbxIdx != -1)
            trimmedNodeName = trimmedNodeName[..fbxIdx];

        foreach (var e in boneInfoMap)
        {
            var trimmedKey = TrimBoneName(e.Key);
            if (string.Equals(trimmedNodeName, trimmedKey, StringComparison.OrdinalIgnoreCase))
            {
                info = e.Value;
                return true;
            }
        }

        info = default!;
        return false;
    }

    private static ModelAnimation LoadAnimation(string name, AssimpScene* scene, Dictionary<string, BoneInfo> boneInfoMap)
    {
        var assAnim = scene->MAnimations[0];
        float duration = (float)assAnim->MDuration;
        float tps = (float)assAnim->MTicksPerSecond;
        if (tps <= 0)
            tps = 25.0f;

        int boneCount = boneInfoMap.Count;
        var keyframes = new List<Keyframe>[boneCount];
        for (int i = 0; i < boneCount; i++)
            keyframes[i] = [];

        for (int c = 0; c < assAnim->MNumChannels; c++)
        {
            var channel = assAnim->MChannels[c];
            string nodeName = channel->MNodeName;

            if (!MatchBoneInfo(nodeName, boneInfoMap, out var info))
                continue;

            uint posKeyCount = channel->MNumPositionKeys;
            uint rotKeyCount = channel->MNumRotationKeys;
            uint sclKeyCount = channel->MNumScalingKeys;

            int maxKeys = (int)Math.Max(Math.Max(posKeyCount, rotKeyCount), sclKeyCount);
            for (int k = 0; k < maxKeys; k++)
            {
                float t = (float)(
                    (k < posKeyCount) ? channel->MPositionKeys[k].MTime :
                    (k < rotKeyCount) ? channel->MRotationKeys[k].MTime :
                    channel->MScalingKeys[k].MTime);

                var pos = (k < posKeyCount) ? channel->MPositionKeys[k].MValue : channel->MPositionKeys[channel->MNumPositionKeys - 1].MValue;
                var rot = (k < rotKeyCount) ? channel->MRotationKeys[k].MValue : channel->MRotationKeys[channel->MNumRotationKeys - 1].MValue;
                var scl = (k < sclKeyCount) ? channel->MScalingKeys[k].MValue : channel->MScalingKeys[channel->MNumScalingKeys - 1].MValue;

                keyframes[info.ID].Add(new Keyframe(t, scl, rot, pos));
            }
        }

        Keyframe[][] result = new Keyframe[boneCount][];
        for (int i = 0; i < boneCount; i++)
            result[i] = keyframes[i].ToArray();

        return new ModelAnimation(name, duration, tps, result);
    }

    private static void WriteBinary(string outputPath, ModelLoadOptions options, List<ProcessedPart> parts,
        Dictionary<string, BoneInfo> boneInfoMap, List<AnimatorNode> animatorNodes, List<ModelAnimation> animations,
        Matrix4x4 inverseGlobalTransform, List<string>? texNames = null)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var fs = File.Create(outputPath);
        using var bw = new BinaryWriter(fs);

        bw.Write("PHXM");
        bw.Write((uint)2);
        bw.Write(options.IsAnimated);
        bw.Write(options.Tangents);
        bw.Write(parts.Count);

        var smx = Matrix4x4.CreateScale(options.Scale);

        foreach (var part in parts)
        {
            bw.Write(part.Name);
            bw.Write(part.Meshes.Count);

            foreach (var mesh in part.Meshes)
            {
                bw.Write(mesh.Name);
                bw.Write(mesh.MaterialIndex);
                bw.Write(mesh.NormalIndex);
                bw.Write(options.PreTransform);

                if (!options.PreTransform)
                    bw.Write(mesh.Transform);

                bw.Write(mesh.Indices.Length);
                bw.Write(mesh.Indices);

                bw.Write(mesh.Vertices.Length);
                foreach (ref readonly var v in mesh.Vertices.AsSpan())
                {
                    var pos = options.PreTransform
                        ? Vector3.Transform(v.Position, mesh.Transform * smx)
                        : v.Position;

                    bw.Write(pos);
                    bw.Write(v.TexCoords);
                    bw.Write(v.Normal);
                    if (options.Tangents)
                    {
                        bw.Write(v.Tangent);
                        bw.Write(v.Bitangent);
                    }
                    if (options.IsAnimated)
                    {
                        bw.Write(v.BoneIds);
                        bw.Write(v.Weights);
                    }
                }
            }
        }

        bool hasTextures = texNames != null && texNames.Count > 0;
        bw.Write(hasTextures);
        if (hasTextures)
        {
            bw.Write(texNames!.Count);
            for (int i = 0; i < texNames.Count; i++)
            {
                bw.Write(texNames[i]);
            }
        }

        if (options.IsAnimated)
        {
            bw.Write(inverseGlobalTransform);
            bw.Write(animatorNodes.Count);

            foreach (var node in animatorNodes)
            {
                bw.Write(node.Name);
                bw.Write(node.IsBone);
                bw.Write(node.ParentID);
                bw.Write(node.ModelBoneID);
                bw.Write(node.Offset);
                bw.Write(node.BindTransform);
                bw.Write(node.Transform);
            }

            bw.Write(animations.Count);
            int boneCount = boneInfoMap.Count;
            bw.Write(boneCount);

            foreach (var an in animations)
            {
                bw.Write(an.Name);
                bw.Write(an.Duration);
                bw.Write(an.TicksPerSecond);

                for (int b = 0; b < boneCount; b++)
                {
                    var boneKeyFrames = an.Keyframes[b];
                    bw.Write(boneKeyFrames.Length);

                    for (int k = 0; k < boneKeyFrames.Length; k++)
                    {
                        var keyFrame = boneKeyFrames[k];
                        bw.Write(keyFrame.TimeStamp);
                        bw.Write(keyFrame.SRT);
                    }
                }
            }
        }
    }

    private static (List<string> TextureNames, List<EmbeddedTexturePayload> Payloads, Dictionary<int, int> MaterialDiffuseIndices, Dictionary<int, int> MaterialNormalIndices) ExtractEmbeddedTextures(
        Assimp assimp, AssimpScene* scene, string sourcePath, ModelLoadOptions options)
    {
        List<string> texNames = [];
        List<EmbeddedTexturePayload> payloads = [];
        Dictionary<int, int> materialDiffuseIndices = new();
        Dictionary<int, int> materialNormalIndices = new();

        int embeddedCount = (int)scene->MNumTextures;
        int materialCount = (int)scene->MNumMaterials;

        if (embeddedCount == 0 && materialCount == 0)
            return (texNames, payloads, materialDiffuseIndices, materialNormalIndices);

        string modelDir = Path.GetDirectoryName(sourcePath) ?? "";
        string extractedDir = Path.Combine(modelDir, "extracted");
        if (options.ExtractTextures && embeddedCount > 0)
        {
            Directory.CreateDirectory(extractedDir);
        }

        Dictionary<string, string> extractedFilesByName = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < embeddedCount; i++)
        {
            var tex = scene->MTextures[i];
            string rawName = Marshal.PtrToStringAnsi((nint)tex->MFilename.Data) ?? "";
            string baseName = Path.GetFileNameWithoutExtension(rawName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = $"{Path.GetFileNameWithoutExtension(sourcePath)}_tex_{i}";
            }

            bool isCompressed = tex->MHeight == 0;
            string savedPng = Path.Combine(extractedDir, $"{baseName}.png");
            string relPath = Path.GetRelativePath(modelDir, savedPng);

            if (options.ExtractTextures)
            {
                int byteLength;
                byte[] data;

                if (isCompressed)
                {
                    byteLength = (int)tex->MWidth;
                    data = new byte[byteLength];
                    Marshal.Copy((nint)tex->PcData, data, 0, byteLength);

                    payloads.Add(new EmbeddedTexturePayload
                    {
                        BaseName = baseName,
                        RelativePath = relPath,
                        FullPath = savedPng,
                        IsCompressed = true,
                        Width = 0,
                        Height = 0,
                        Data = data
                    });
                }
                else
                {
                    int width = (int)tex->MWidth;
                    int height = (int)tex->MHeight;
                    byteLength = width * height * 4;
                    data = new byte[byteLength];
                    byte* pSrc = (byte*)tex->PcData;
                    for (int p = 0; p < width * height; p++)
                    {
                        data[p * 4 + 0] = pSrc[p * 4 + 2];
                        data[p * 4 + 1] = pSrc[p * 4 + 1];
                        data[p * 4 + 2] = pSrc[p * 4 + 0];
                        data[p * 4 + 3] = pSrc[p * 4 + 3];
                    }

                    payloads.Add(new EmbeddedTexturePayload
                    {
                        BaseName = baseName,
                        RelativePath = relPath,
                        FullPath = savedPng,
                        IsCompressed = false,
                        Width = width,
                        Height = height,
                        Data = data
                    });
                }
            }

            extractedFilesByName[baseName] = savedPng;
            extractedFilesByName[$"*{i}"] = savedPng;
            if (!string.IsNullOrEmpty(rawName))
            {
                extractedFilesByName[rawName] = savedPng;
                extractedFilesByName[Path.GetFileName(rawName)] = savedPng;
            }
        }

        Dictionary<string, int> texturePathToIndex = new(StringComparer.OrdinalIgnoreCase);

        int RegisterTex(string fullOrRelPath)
        {
            string rel = Path.IsPathRooted(fullOrRelPath) ? Path.GetRelativePath(modelDir, fullOrRelPath) : fullOrRelPath;
            if (texturePathToIndex.TryGetValue(rel, out int existing))
                return existing;
            int idx = texNames.Count;
            texNames.Add(rel);
            texturePathToIndex[rel] = idx;
            return idx;
        }

        for (int m = 0; m < materialCount; m++)
        {
            var mat = scene->MMaterials[m];

            // 1) Primary Diffuse / BaseColor
            string? diffusePath = null;
            AssimpString str = default;
            if (assimp.GetMaterialTexture(mat, TextureType.Diffuse, 0, &str, null, null, null, null, null, null) == Return.Success ||
                assimp.GetMaterialTexture(mat, TextureType.BaseColor, 0, &str, null, null, null, null, null, null) == Return.Success)
            {
                diffusePath = ResolveTextureFile(str.AsString, modelDir, extractedFilesByName);
            }

            if (diffusePath != null)
            {
                materialDiffuseIndices[m] = RegisterTex(diffusePath);
            }
            else if (m < embeddedCount && extractedFilesByName.TryGetValue($"*{m}", out var emb))
            {
                materialDiffuseIndices[m] = RegisterTex(emb);
            }

            // 2) Primary Normal / Height
            string? normalPath = null;
            if (assimp.GetMaterialTexture(mat, TextureType.Normals, 0, &str, null, null, null, null, null, null) == Return.Success ||
                assimp.GetMaterialTexture(mat, TextureType.Height, 0, &str, null, null, null, null, null, null) == Return.Success ||
                assimp.GetMaterialTexture(mat, TextureType.NormalCamera, 0, &str, null, null, null, null, null, null) == Return.Success)
            {
                normalPath = ResolveTextureFile(str.AsString, modelDir, extractedFilesByName);
            }

            if (normalPath != null)
            {
                materialNormalIndices[m] = RegisterTex(normalPath);
            }

            // 3) All other texture types on this material
            foreach (TextureType type in Enum.GetValues<TextureType>())
            {
                uint count = assimp.GetMaterialTextureCount(mat, type);
                for (uint ti = 0; ti < count; ti++)
                {
                    AssimpString pStr = default;
                    if (assimp.GetMaterialTexture(mat, type, ti, &pStr, null, null, null, null, null, null) == Return.Success)
                    {
                        string? matched = ResolveTextureFile(pStr.AsString, modelDir, extractedFilesByName);
                        if (matched != null)
                        {
                            RegisterTex(matched);
                        }
                    }
                }
            }
        }

        // 4) Include all embedded textures and payloads
        foreach (var payload in payloads)
        {
            RegisterTex(payload.FullPath);
        }

        foreach (var (_, fullPath) in extractedFilesByName)
        {
            RegisterTex(fullPath);
        }

        // 5) Ensure every material has a fallback diffuse index
        for (int m = 0; m < materialCount; m++)
        {
            if (!materialDiffuseIndices.ContainsKey(m))
            {
                if (m < texNames.Count)
                    materialDiffuseIndices[m] = m;
                else if (texNames.Count > 0)
                    materialDiffuseIndices[m] = 0;
            }
        }

        return (texNames, payloads, materialDiffuseIndices, materialNormalIndices);
    }

    private static string? ResolveTextureFile(string textureString, string modelDir, Dictionary<string, string> extractedMap)
    {
        if (string.IsNullOrWhiteSpace(textureString))
            return null;

        string baseName = Path.GetFileNameWithoutExtension(textureString);
        string fileName = Path.GetFileName(textureString);

        if (extractedMap.TryGetValue(textureString, out var matched))
            return matched;
        if (extractedMap.TryGetValue(fileName, out matched))
            return matched;
        if (extractedMap.TryGetValue(baseName, out matched))
            return matched;

        string candidate = Path.Combine(modelDir, textureString);
        if (File.Exists(candidate))
            return candidate;

        string fromExtracted = Path.Combine(modelDir, "extracted", fileName);
        if (File.Exists(fromExtracted))
            return fromExtracted;

        string fromExtractedPng = Path.Combine(modelDir, "extracted", $"{baseName}.png");
        if (File.Exists(fromExtractedPng))
            return fromExtractedPng;

        return null;
    }
}

