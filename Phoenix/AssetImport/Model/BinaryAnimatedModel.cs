using Phoenix.AssetImport.Model.Animation;
using Phoenix.Rendering;
using Phoenix.Rendering.Animation;
using Phoenix.Rendering.Geometry;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.AssetImport.Model
{
    public class BinaryAnimatedModel : BinaryModel
    {
        public List<BinaryAnimation> Animations { get; internal set; } = default!;

        public AnimatorNode[] AnimatorNodes { get; internal set; } = default!;

        public Matrix4x4 InverseGlobalTransform { get; internal set; } = default!;

        public int BoneCount { get; internal set; } = default!;
        public Matrix4x4[] FinalBoneMatrices { get; internal set; } = default!;

        private int _selectedAnimation = 0;

        public BinaryAnimatedModel()
        {
            FinalBoneMatrices = new Matrix4x4[BoneCount];
        }

        public void SetAnimation(int index)
        {
            if (index < 0 || index > Animations.Count - 1)
                throw new Exception($"animation index outside of bounds {Animations.Count}");

            _selectedAnimation = index;
        }
        public void Update(float deltaTime)
        {
            if(FinalBoneMatrices.Length == 0)
                FinalBoneMatrices = new Matrix4x4[BoneCount];

            var animation = Animations[_selectedAnimation];
            animation.Update(deltaTime);

            for (var i = 0; i < AnimatorNodes.Length; i++)
            {
                var node = AnimatorNodes[i];

                var pid = node.ParentID;
                var parentTransform = pid != -1 ? AnimatorNodes[pid].Transform : Matrix4x4.Identity;

                Matrix4x4 localTransform;
                if (node.IsBone)
                {
                    var animTransform = animation.Transforms[node.ModelBoneID];

                    animTransform.Transpose();

                    localTransform = animTransform;
                }
                else
                    localTransform = node.BindTransform;

                node.Transform = parentTransform * localTransform;

                if (node.IsBone)
                {
                    var final = InverseGlobalTransform * node.Transform * node.Offset;
                    final.Transpose();
                    FinalBoneMatrices[node.ModelBoneID] = final;
                }
            }
        }

    }
}
