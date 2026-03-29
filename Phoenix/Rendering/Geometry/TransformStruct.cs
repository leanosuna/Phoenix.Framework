using System.Numerics;

namespace Phoenix.Rendering.Geometry
{
    public struct TransformStruct
    {
        public Vector3 Scale { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Vector3 Translation { get; private set; }

        public TransformStruct(Vector3 scale, Quaternion rotation, Vector3 translation)
        {
            Scale = scale;
            Rotation = rotation;
            Translation = translation;
        }
    }
}
