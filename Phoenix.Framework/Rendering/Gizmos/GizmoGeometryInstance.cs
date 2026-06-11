using System.Numerics;

namespace Phoenix.Framework.Rendering.Gizmos
{
    internal struct GizmoGeometryInstance
    {
        public Action Draw;
        public Matrix4x4 World;
        public Vector3 Color;
        public bool Hit;
    }
}
