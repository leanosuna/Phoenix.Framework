using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.Framework.Collisions
{
    public class CollisionTriangle
    {
        public Vector3[] V;
        public Vector3 N = Vector3.Zero;
        public uint ID;
        public CollisionTriangle()
        {
            V = new Vector3[3];
        }
        public CollisionTriangle(uint id, Vector3[] v)
        {
            ID = id;
            V = v;
        }
        public CollisionTriangle(uint id, Vector3[] v, Vector3 n)
        {
            ID = id;
            V = v;
            N = n;
        }

        public Vector3 GetNormal()
        {
            if(N == Vector3.Zero)
                N = CalcNormal();

            return N;
        }
        private Vector3 CalcNormal()
        {
            
            Vector3 edge1 = V[1] - V[0];
            Vector3 edge2 = V[2] - V[0];
            return Vector3.Normalize(Vector3.Cross(edge1, edge2));

        }

        public void GetPlane(out Vector3 normal, out float D)
        {
            normal = GetNormal();
            D = -Vector3.Dot(normal, V[0]);
        }
    }
}
