using System.Numerics;

namespace Phoenix.Framework.Collisions
{
    public struct Capsule
    {
        public Vector3 PointA;
        public Vector3 PointB;
        public float Radius;

        public Capsule(Vector3 a, Vector3 b, float r)
        {
            PointA = a;
            PointB = b;
            Radius = r;
        }
    }
}
