using Phoenix.Framework.Maths;
using Phoenix.Framework.Rendering.Geometry.Model.Animations;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Geometry.Model
{
    public class AnimatedModel : Model
    {
        public List<Animation> Animations { get; internal set; } = default!;

        public AnimatorNode[] AnimatorNodes { get; internal set; } = default!;

        public Matrix4x4 InverseGlobalTransform { get; internal set; } = default!;

        public int BoneCount { get; internal set; } = default!;
        public Matrix4x4[] FinalBoneMatrices { get; internal set; } = default!;

        private int _selectedAnimation = 0;

        public AnimatedModel()
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
            animation.Update(deltaTime, FinalBoneMatrices);
        }

    }
}
