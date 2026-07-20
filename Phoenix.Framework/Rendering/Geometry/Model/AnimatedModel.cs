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

        public bool EnableBoneWorldTransforms { get; set; } = false;
        private int _selectedAnimation = 0;
        private BoneWorldTransform[] _boneWorldTransforms = [];

        private Matrix4x4[] _blendFromMatrices = [];
        private Animation? _blendFromAnim;
        private float _blendFactor;
        private float _blendDuration = 0.25f;
        private Matrix4x4[] _blendTemp = [];

        public struct BoneWorldTransform
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        public AnimatedModel()
        {
            FinalBoneMatrices = new Matrix4x4[BoneCount];
        }

        public void SetAnimation(int index)
        {
            if (index < 0 || index > Animations.Count - 1)
                throw new Exception($"animation index outside of bounds {Animations.Count}");

            _selectedAnimation = index;
            _blendFromAnim = null;
            _blendFactor = 0;
        }

        public void SetAnimationBlend(int index, float duration = 0.25f)
        {
            if (index < 0 || index > Animations.Count - 1)
                throw new Exception($"animation index outside of bounds {Animations.Count}");

            if (_selectedAnimation == index)
                return;

            _blendFromAnim = Animations[_selectedAnimation];
            if (_blendFromMatrices.Length != BoneCount)
                _blendFromMatrices = new Matrix4x4[BoneCount];
            Array.Copy(FinalBoneMatrices, _blendFromMatrices, BoneCount);

            _selectedAnimation = index;
            _blendFactor = 0;
            _blendDuration = duration;
        }

        public void Update(float deltaTime)
        {
            if(FinalBoneMatrices.Length == 0)
                FinalBoneMatrices = new Matrix4x4[BoneCount];

            if (_blendFromAnim != null)
            {
                _blendFactor += deltaTime / _blendDuration;
                if (_blendFactor >= 1.0f)
                {
                    _blendFromAnim = null;
                    _blendFactor = 0;
                }
            }

            if (_blendFromAnim != null)
            {
                if (_blendTemp.Length != BoneCount)
                    _blendTemp = new Matrix4x4[BoneCount];

                _blendFromAnim.Update(deltaTime, _blendTemp);

                var animation = Animations[_selectedAnimation];
                animation.Update(deltaTime, FinalBoneMatrices);

                for (int b = 0; b < BoneCount; b++)
                    FinalBoneMatrices[b] = Matrix4x4.Lerp(_blendTemp[b], FinalBoneMatrices[b], _blendFactor);
            }
            else
            {
                var animation = Animations[_selectedAnimation];
                animation.Update(deltaTime, FinalBoneMatrices);
            }

            if(EnableBoneWorldTransforms)
                ComputeBoneWorldTransforms();
        }

        public ReadOnlySpan<BoneWorldTransform> GetBoneWorldTransforms()
        {
            return _boneWorldTransforms;
        }

        private void ComputeBoneWorldTransforms()
        {
            if (_boneWorldTransforms.Length != BoneCount)
                _boneWorldTransforms = new BoneWorldTransform[BoneCount];

            Matrix4x4.Invert(InverseGlobalTransform, out var globalTransform);

            for (int b = 0; b < BoneCount; b++)
            {
                var node = FindBoneNode(b);
                if (node == null)
                {
                    _boneWorldTransforms[b] = default;
                    continue;
                }

                var F = FinalBoneMatrices[b];

                var F_col = Matrix4x4.Transpose(F);

                Matrix4x4.Invert(node.Offset, out var offsetInv);

                var W_col = globalTransform * F_col * offsetInv;

                var position = new Vector3(W_col.M14, W_col.M24, W_col.M34);

                var W_row = Matrix4x4.Transpose(W_col);
                Matrix4x4.Decompose(W_row, out _, out var rotation, out _);

                _boneWorldTransforms[b] = new BoneWorldTransform
                {
                    Position = position,
                    Rotation = rotation
                };
            }
        }

        private AnimatorNode? FindBoneNode(int boneIndex)
        {
            for (int i = 0; i < AnimatorNodes.Length; i++)
            {
                if (AnimatorNodes[i].IsBone && AnimatorNodes[i].ModelBoneID == boneIndex)
                    return AnimatorNodes[i];
            }
            return null;
        }

    }
}
