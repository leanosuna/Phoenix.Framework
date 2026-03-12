using Phoenix.AssetImport.Model.Animation;
using Phoenix.Rendering;
using Phoenix.Rendering.Animation;
using Phoenix.Rendering.Geometry;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Phoenix.AssetImport.Model
{
    internal static class BinaryModelReader
    {
        public static BinaryModel Load(string path)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            var assetType = br.ReadString();
            var ver = br.ReadUInt32();
            var isAnimated = br.ReadBoolean();
            
            var partsCount = br.ReadInt32();
            //Log.Debug($"parts {partsCount}");

            List<BinaryModelPart> parts = new List<BinaryModelPart>();
            for (int p = 0; p < partsCount; p++)
            {
                var partName = br.ReadString();
                var meshCount = br.ReadInt32();
                //Log.Debug($"part {partName}");

                List<BinaryMesh> meshes = new List<BinaryMesh>();
                for (int m = 0; m < meshCount; m++)
                {
                    var meshName = br.ReadString();

                    var index = br.ReadInt32();
                    var transform = br.ReadStruct<Matrix4x4>();
                    var indicesLength = br.ReadInt32();
                    var indices = br.ReadArray<uint>(indicesLength);
                    var verticesLength = br.ReadInt32();
                    var vertices = br.ReadArray<Vertex>(verticesLength);

                    //Log.Debug($"m {meshName}");
                    var tv = vertices[0];
                    //Log.Debug($"test w{tv.Weights} bid {tv.BoneIds}");

                    //foreach (var v in vertices)
                    //{
                    //    Log.Debug($"bid {v.BoneIds.ToStrInt()} w {v.Weights.ToStrF2()}");
                    //}

                    meshes.Add(new BinaryMesh(meshName, vertices, indices, transform, isAnimated));
                }

                parts.Add(new BinaryModelPart(partName, meshes));
            }

            var hasTextures = br.ReadBoolean();
            var texList = new List<string>();
            if (hasTextures)
            {
                var texCount = br.ReadInt32();
                for (var i = 0; i < texCount; i++)
                    texList.Add(br.ReadString());
            }

            

            var animations = new List<BinaryAnimation>();
            var animationNodes = new List<AnimatorNode>();
            var igt = Matrix4x4.Identity;
            var boneCount = 0;
            if (isAnimated)
            {
                igt = br.ReadStruct<Matrix4x4>();

                var nodeCount = br.ReadInt32();
                //Log.Debug($"nodes {nodeCount}");
                for (var i = 0; i < nodeCount; i++)
                {
                    var name = br.ReadString();
                    var isBone = br.ReadBoolean();
                    var parentID = br.ReadInt32();
                    var modelBoneID = br.ReadInt32();
                    var offset = br.ReadStruct<Matrix4x4>();
                    var bindTransform = br.ReadStruct<Matrix4x4>();
                    var transform = br.ReadStruct<Matrix4x4>();

                    var b = isBone ? "B" : "N";
                    //Log.Debug($"{b} {name} {parentID} {modelBoneID} {offset.ToStrF2()} {bindTransform.ToStrF2()} {transform.ToStrF2()}");

                    animationNodes.Add(new AnimatorNode
                    {
                        Name = name,
                        IsBone = isBone,
                        ParentID = parentID,
                        ModelBoneID = modelBoneID,
                        Offset = offset,
                        BindTransform = bindTransform,
                        Transform = transform
                    });
                    //..
                }
                //PrintFlattened(animationNodes);


                var animCount = br.ReadInt32();
                boneCount = br.ReadInt32();
                
                //Log.Debug($"bc {boneCount}");
                for (var i = 0; i < animCount; i++)
                {
                    var name = br.ReadString();
                    var duration = br.ReadSingle();
                    var tps = br.ReadSingle();

                    //Log.Debug($"name {name}");
                    //Log.Debug($"d {duration}");
                    //Log.Debug($"tps {tps}");


                    Keyframe[][] keyFrames = new Keyframe[boneCount][];
                    
                    for (var b = 0; b < boneCount; b++)
                    {
                        var keyFramesLen = br.ReadInt32();
                        keyFrames[b] = new Keyframe[keyFramesLen];

                        //Log.Debug($"b{b} kf {keyFramesLen}");
                        for (var k = 0; k < keyFramesLen; k++)
                        {
                            var timeStamp = br.ReadSingle();
                            var srt = br.ReadStruct<TransformStruct>();

                            keyFrames[b][k] = new Keyframe(timeStamp, srt.Scale, srt.Rotation, srt.Translation);
                            //Log.Debug($"b{b} rot {timeStamp} {srt.Rotation.ToStr()}");
                        }
                    }
                    animations.Add(new BinaryAnimation(name, duration, tps, keyFrames));
                }
            }

            return isAnimated? 
                new BinaryAnimatedModel 
                {
                    Parts = parts,
                    TextureNames = texList,
                    BoneCount = boneCount,
                    Animations = animations,
                    AnimatorNodes = animationNodes.ToArray(),
                    InverseGlobalTransform = igt
                }:
                new BinaryModel
                {
                    Parts = parts,
                    TextureNames = texList
                };
        }

        private static int TabCount(List<AnimatorNode> nodes, AnimatorNode node)
        {
            if (node.ParentID == -1 || node.ParentID == 0)
                return 0;

            return TabCount(nodes, nodes[node.ParentID]) + 1;

        }
        private static void PrintFlattened(List<AnimatorNode> animatorNodes)
        {
            Log.Debug("[Animation Nodes]");
            for (var i = 0; i < animatorNodes.Count; i++)
            {
                var node = animatorNodes[i];
                var str = node.IsBone ? "B" : "N";
                var spc = "";
                for (var j = 0; j < TabCount(animatorNodes, node); j++)
                {
                    spc += "-";
                }
                str += $"{i}{spc} PID {node.ParentID}, MID {node.ModelBoneID}, {node.Name}";
                Log.Debug(str);
            }
            Log.Debug("---");
        }
    }
}
