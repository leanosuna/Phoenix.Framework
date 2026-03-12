using System.Numerics;

namespace Phoenix.Rendering.Animation
{
    public class AnimatorNode
    {
        public string Name { get; internal set; } = "";
        public Matrix4x4 BindTransform { get; internal set; } = Matrix4x4.Identity;
        public Matrix4x4 Transform { get; internal set; } = Matrix4x4.Identity;

        public Matrix4x4 Offset { get; internal set; } = Matrix4x4.Identity;
        public int ParentID { get; internal set; }
        public int ModelBoneID { get; internal set; }
        public bool IsBone { get; internal set; }

        public AnimatorNode(Matrix4x4 transform, int parentID, int modelBoneID, Matrix4x4 offset)
        {
            BindTransform = transform;
            ParentID = parentID;
            ModelBoneID = modelBoneID;
            IsBone = true;
            Offset = offset;
        }

        public AnimatorNode(Matrix4x4 transform, int parentID)
        {
            BindTransform = transform;
            ParentID = parentID;
            ModelBoneID = -1;
            IsBone = false;
            Offset = Matrix4x4.Identity;
        }

        public AnimatorNode()
        {

        }
    }
}
