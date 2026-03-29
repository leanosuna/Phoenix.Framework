using Phoenix.AssetImport;
using Phoenix.Rendering.Geometry.Model.Animations;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Rendering.Geometry.Model
{
    internal static class BinaryModelReader
    {
        public static Model Load(GL gl, string path)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            var assetType = br.ReadString();
            var ver = br.ReadUInt32();
            var isAnimated = br.ReadBoolean();
            
            var partsCount = br.ReadInt32();
            
            List<ModelPart> parts = new List<ModelPart>();
            for (int p = 0; p < partsCount; p++)
            {
                var partName = br.ReadString();
                var meshCount = br.ReadInt32();
            
                List<ModelMesh> meshes = new List<ModelMesh>();
                for (int m = 0; m < meshCount; m++)
                {
                    var meshName = br.ReadString();

                    var index = br.ReadInt32();
                    var transform = br.ReadStruct<Matrix4x4>();
                    var indicesLength = br.ReadInt32();
                    var indices = br.ReadArray<uint>(indicesLength);
                    var verticesLength = br.ReadInt32();
                    var vertices = br.ReadArray<Vertex>(verticesLength);

                    var tv = vertices[0];
                    
                    meshes.Add(new ModelMesh(gl, meshName, vertices, indices, transform, isAnimated));
                }

                parts.Add(new ModelPart(partName, meshes));
            }

            var hasTextures = br.ReadBoolean();
            var texList = new List<string>();
            if (hasTextures)
            {
                var texCount = br.ReadInt32();
                for (var i = 0; i < texCount; i++)
                    texList.Add(br.ReadString());
            }

            var animations = new List<Animation>();
            var animationNodes = new List<AnimatorNode>();
            var igt = Matrix4x4.Identity;
            var boneCount = 0;
            if (isAnimated)
            {
                igt = br.ReadStruct<Matrix4x4>();

                var nodeCount = br.ReadInt32();
                for (var i = 0; i < nodeCount; i++)
                {
                    var name = br.ReadString();
                    var isBone = br.ReadBoolean();
                    var parentID = br.ReadInt32();
                    var modelBoneID = br.ReadInt32();
                    var offset = br.ReadStruct<Matrix4x4>();
                    var bindTransform = br.ReadStruct<Matrix4x4>();
                    var transform = br.ReadStruct<Matrix4x4>();

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
                }
                
                var animCount = br.ReadInt32();
                boneCount = br.ReadInt32();
                
                for (var i = 0; i < animCount; i++)
                {
                    var name = br.ReadString();
                    var duration = br.ReadSingle();
                    var tps = br.ReadSingle();

                    Keyframe[][] keyFrames = new Keyframe[boneCount][];
                    
                    for (var b = 0; b < boneCount; b++)
                    {
                        var keyFramesLen = br.ReadInt32();
                        keyFrames[b] = new Keyframe[keyFramesLen];

                        for (var k = 0; k < keyFramesLen; k++)
                        {
                            var timeStamp = br.ReadSingle();
                            var srt = br.ReadStruct<TransformStruct>();

                            keyFrames[b][k] = new Keyframe(timeStamp, srt.Scale, srt.Rotation, srt.Translation);
                        }
                    }
                    animations.Add(new Animation(name, duration, tps, keyFrames));
                }
            }

            return isAnimated? 
                new AnimatedModel 
                {
                    Parts = parts,
                    TextureNames = texList,
                    BoneCount = boneCount,
                    Animations = animations,
                    AnimatorNodes = animationNodes.ToArray(),
                    InverseGlobalTransform = igt
                }:
                new Model
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
    }
}
