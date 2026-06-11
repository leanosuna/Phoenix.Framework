using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ModelVertex
    {
        public Vector3 Position;
        public Vector2 TexCoords;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Bitangent;
        public Vector4 BoneIds;
        public Vector4 Weights;
        
    }
}