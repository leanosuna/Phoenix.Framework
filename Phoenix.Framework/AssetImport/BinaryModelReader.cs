using Phoenix.Framework.Rendering;
using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Geometry.Model.Animations;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Deserializes binary model caches directly into GPU-resident Vulkan buffers and animation hierarchies.
/// </summary>
public static class BinaryModelReader
{
    public static Model Read(Graphics graphics, string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        string magic = br.ReadString();
        if (magic != "PHXM")
            throw new InvalidDataException($"Invalid model binary magic: {magic}");

        uint version = br.ReadUInt32();
        if (version != 1 && version != 2)
            throw new NotSupportedException($"Unsupported model binary version: {version}");

        bool isAnimated = br.ReadBoolean();
        bool tangents = br.ReadBoolean();
        int partsCount = br.ReadInt32();

        List<ModelPart> parts = new(partsCount);
        for (int p = 0; p < partsCount; p++)
        {
            string partName = br.ReadString();
            int meshCount = br.ReadInt32();

            List<ModelMesh> meshes = new(meshCount);
            for (int m = 0; m < meshCount; m++)
            {
                string meshName = br.ReadString();
                int materialIndex = br.ReadInt32();
                int normalIndex = version >= 2 ? br.ReadInt32() : -1;
                bool preTransformed = br.ReadBoolean();
                Matrix4x4 transform = preTransformed ? Matrix4x4.Identity : br.ReadStruct<Matrix4x4>();

                int indicesLength = br.ReadInt32();
                uint[] indices = br.ReadArray<uint>(indicesLength);

                int verticesLength = br.ReadInt32();
                ModelVertex[] vertices = new ModelVertex[verticesLength];
                for (int i = 0; i < verticesLength; i++)
                {
                    vertices[i].Position = br.ReadStruct<Vector3>();
                    vertices[i].TexCoords = br.ReadStruct<Vector2>();
                    vertices[i].Normal = br.ReadStruct<Vector3>();
                    if (tangents)
                    {
                        vertices[i].Tangent = br.ReadStruct<Vector3>();
                        vertices[i].Bitangent = br.ReadStruct<Vector3>();
                    }
                    if (isAnimated)
                    {
                        vertices[i].BoneIds = br.ReadStruct<Vector4>();
                        vertices[i].Weights = br.ReadStruct<Vector4>();
                    }
                }

                ulong vertexBufferSize = (ulong)(vertices.Length * Marshal.SizeOf<ModelVertex>());
                var vertexBuffer = graphics.CreateBuffer(
                    vertexBufferSize,
                    BufferUsageFlags.VertexBufferBit,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                vertexBuffer.SetData<ModelVertex>(vertices.AsSpan());

                ulong indexBufferSize = (ulong)(indices.Length * sizeof(uint));
                var indexBuffer = graphics.CreateBuffer(
                    indexBufferSize,
                    BufferUsageFlags.IndexBufferBit,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
                indexBuffer.SetData<uint>(indices.AsSpan());

                meshes.Add(new ModelMesh(meshName, vertexBuffer, indexBuffer, (uint)vertices.Length, (uint)indices.Length, transform, materialIndex, normalIndex));
            }

            parts.Add(new ModelPart(partName, meshes));
        }

        bool hasTextures = br.ReadBoolean();
        List<string> texList = [];
        if (hasTextures)
        {
            int texCount = br.ReadInt32();
            for (int i = 0; i < texCount; i++)
            {
                texList.Add(br.ReadString());
            }
        }

        if (isAnimated)
        {
            Matrix4x4 igt = br.ReadStruct<Matrix4x4>();
            int nodeCount = br.ReadInt32();
            List<AnimatorNode> animationNodes = new(nodeCount);

            for (int i = 0; i < nodeCount; i++)
            {
                string name = br.ReadString();
                bool isBone = br.ReadBoolean();
                int parentID = br.ReadInt32();
                int modelBoneID = br.ReadInt32();
                Matrix4x4 offset = br.ReadStruct<Matrix4x4>();
                Matrix4x4 bindTransform = br.ReadStruct<Matrix4x4>();
                Matrix4x4 transform = br.ReadStruct<Matrix4x4>();

                var node = isBone
                    ? new AnimatorNode(bindTransform, parentID, modelBoneID, offset)
                    : new AnimatorNode(bindTransform, parentID);

                node.Name = name;
                node.Transform = transform;
                animationNodes.Add(node);
            }

            int animCount = br.ReadInt32();
            int boneCount = br.ReadInt32();
            List<Animation> animations = new(animCount);

            for (int i = 0; i < animCount; i++)
            {
                string name = br.ReadString();
                float duration = br.ReadSingle();
                float tps = br.ReadSingle();

                Keyframe[][] keyFrames = new Keyframe[boneCount][];
                for (int b = 0; b < boneCount; b++)
                {
                    int keyFramesLen = br.ReadInt32();
                    keyFrames[b] = new Keyframe[keyFramesLen];

                    for (int k = 0; k < keyFramesLen; k++)
                    {
                        float timeStamp = br.ReadSingle();
                        Transform srt = br.ReadStruct<Transform>();
                        keyFrames[b][k] = new Keyframe(timeStamp, srt);
                    }
                }

                animations.Add(new Animation(name, duration, tps, keyFrames));
            }

            var animatedModel = new AnimatedModel
            {
                Parts = parts,
                TextureNames = texList,
                BoneCount = boneCount,
                Animations = animations,
                AnimatorNodes = animationNodes.ToArray(),
                InverseGlobalTransform = igt
            };

            animatedModel.InitializeGpuResources(graphics);
            return animatedModel;
        }

        return new Model
        {
            Parts = parts,
            TextureNames = texList
        };
    }
}
