using System.Numerics;

namespace Phoenix.Rendering.Geometry.Model.Animations
{
    public class AnimatorNode
    {
        public string Name { get; internal set; } = "";
        public Matrix4x4 BindTransform { get; internal set; }
        public Matrix4x4 Transform { get; internal set; } = Matrix4x4.Identity;

        public Matrix4x4 Offset { get; internal set; }
        public int ParentID { get; internal set; }
        public int ModelBoneID { get; internal set; }
        public bool IsBone { get; internal set; }

        public AnimatorNode()
        {
        }
    }
}
