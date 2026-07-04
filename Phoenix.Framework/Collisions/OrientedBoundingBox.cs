using System.Numerics;

namespace Phoenix.Framework.Collisions
{
    /// <summary>
    ///     Represents an Oriented-BoundingBox (OBB).
    /// </summary>
    public class OrientedBoundingBox
    {
        public Vector3 Position { get; set; }
        public Matrix4x4 Orientation { get; set; }
        public Vector3 Size { get; set; }

        internal Matrix4x4 _world;

        public Matrix4x4 World { get => _world; }

        /// <summary>
        ///     Builds an empty Bounding Oriented Box.
        /// </summary>
        public OrientedBoundingBox() { }

        /// <summary>
        ///     Builds a Oriented Bounding-Box with a center and extents.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="extents"></param>
        public OrientedBoundingBox(Vector3 position, Vector3 size)
        {
            Position = position;
            Size = size;
            Orientation = Matrix4x4.Identity;
            CalculateWorld();
        }

        /// <summary>
        ///     Rotate the OBB with a given Matrix.
        ///     Note that this is an absolute rotation.
        /// </summary>
        /// <param name="rotation">Rotation matrix</param>
        public void Update(Matrix4x4 rotation)
        {
            Orientation = rotation;
            CalculateWorld();
        }
        public void Update(Vector3 position)
        {
            Position = position;
            CalculateWorld();
        }
        public void Update(Vector3 position, Matrix4x4 rotation)
        {
            Position = position;
            Orientation = rotation;
            CalculateWorld();
        }

        private void CalculateWorld()
        {
            _world = Matrix4x4.CreateScale(Size) * Orientation * Matrix4x4.CreateTranslation(Position);
        }


        /// <summary>
        ///     Creates an <see cref="OrientedBoundingBox">OrientedBoundingBox</see> from a <see cref="BoundingBox">BoundingBox</see>.
        /// </summary>
        /// <param name="box">A <see cref="BoundingBox">BoundingBox</see> to create the <see cref="OrientedBoundingBox">OrientedBoundingBox</see> from</param>
        /// <returns>The generated <see cref="OrientedBoundingBox">OrientedBoundingBox</see></returns>
        public static OrientedBoundingBox FromAABB(AxisAlignedBoundingBox box)
        {
            return new OrientedBoundingBox(box.Position, box.Size);
        }

        /// <summary>
        ///     Converts a point from World-Space to OBB-Space.
        /// </summary>
        /// <param name="point">Point in World-Space</param>
        /// <returns>The point in OBB-Space</returns>
        public Vector3 ToOBBSpace(Vector3 point)
        {
            return Vector3.Transform(point - Position, Orientation);
        }


        /// <summary>
        ///     A helper method to create an array from a Vector3
        /// </summary>
        /// <param name="vector">The vector to create the array from</param>
        /// <returns>An array of length three with each position matching the Vector3 coordinates</returns>
        private float[] ToArray(Vector3 vector)
        {
            return new[] { vector.X, vector.Y, vector.Z };
        }



        /// <summary>
        ///     A helper method to create an array from a 3x3 Matrix
        /// </summary>
        /// <param name="matrix">A 3x3 Matrix to create the array from</param>
        /// <returns>An array of length nine with each position matching the matrix elements</returns>
        private float[] ToFloatArray(Matrix4x4 matrix)
        {
            return new[]
            {
                matrix.M11, matrix.M21, matrix.M31,
                matrix.M12, matrix.M22, matrix.M32,
                matrix.M13, matrix.M23, matrix.M33,
            };
        }

        /// <summary>
        ///     Tests if this OBB intersects with another OBB.
        /// </summary>
        /// <param name="box">The other OBB to test</param>
        /// <returns>True if the two boxes intersect</returns>
        public bool Intersects(OrientedBoundingBox box)
        {
            float ra;
            float rb;
            var R = new float[3, 3];
            var AbsR = new float[3, 3];
            var ae = ToArray(Size * 0.5f);
            var be = ToArray(box.Size * 0.5f);

            // Compute rotation matrix expressing the other box in this box coordinate frame

            var result = ToFloatArray(Matrix4x4.Multiply(Orientation, box.Orientation));

            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    R[i, j] = result[i * 3 + j];


            // Compute translation vector t
            var tVec = box.Position - Position;

            // Bring translation into this boxs coordinate frame

            var t = ToArray(Vector3.Transform(tVec, Orientation));

            // Compute common subexpressions. Add in an epsilon term to
            // counteract arithmetic errors when two edges are parallel and
            // their cross product is (near) null (see text for details)

            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    AbsR[i, j] = MathF.Abs(R[i, j]) + float.Epsilon;

            // Test axes L = A0, L = A1, L = A2
            for (var i = 0; i < 3; i++)
            {
                ra = ae[i];
                rb = be[0] * AbsR[i, 0] + be[1] * AbsR[i, 1] + be[2] * AbsR[i, 2];
                if (MathF.Abs(t[i]) > ra + rb) return false;
            }

            // Test axes L = B0, L = B1, L = B2
            for (var i = 0; i < 3; i++)
            {
                ra = ae[0] * AbsR[0, i] + ae[1] * AbsR[1, i] + ae[2] * AbsR[2, i];
                rb = be[i];
                if (MathF.Abs(t[0] * R[0, i] + t[1] * R[1, i] + t[2] * R[2, i]) > ra + rb) return false;
            }

            // Test axis L = A0 x B0
            ra = ae[1] * AbsR[2, 0] + ae[2] * AbsR[1, 0];
            rb = be[1] * AbsR[0, 2] + be[2] * AbsR[0, 1];
            if (MathF.Abs(t[2] * R[1, 0] - t[1] * R[2, 0]) > ra + rb) return false;

            // Test axis L = A0 x B1
            ra = ae[1] * AbsR[2, 1] + ae[2] * AbsR[1, 1];
            rb = be[0] * AbsR[0, 2] + be[2] * AbsR[0, 0];
            if (MathF.Abs(t[2] * R[1, 1] - t[1] * R[2, 1]) > ra + rb) return false;

            // Test axis L = A0 x B2
            ra = ae[1] * AbsR[2, 2] + ae[2] * AbsR[1, 2];
            rb = be[0] * AbsR[0, 1] + be[1] * AbsR[0, 0];
            if (MathF.Abs(t[2] * R[1, 2] - t[1] * R[2, 2]) > ra + rb) return false;

            // Test axis L = A1 x B0
            ra = ae[0] * AbsR[2, 0] + ae[2] * AbsR[0, 0];
            rb = be[1] * AbsR[1, 2] + be[2] * AbsR[1, 1];
            if (MathF.Abs(t[0] * R[2, 0] - t[2] * R[0, 0]) > ra + rb) return false;

            // Test axis L = A1 x B1
            ra = ae[0] * AbsR[2, 1] + ae[2] * AbsR[0, 1];
            rb = be[0] * AbsR[1, 2] + be[2] * AbsR[1, 0];
            if (MathF.Abs(t[0] * R[2, 1] - t[2] * R[0, 1]) > ra + rb) return false;

            // Test axis L = A1 x B2
            ra = ae[0] * AbsR[2, 2] + ae[2] * AbsR[0, 2];
            rb = be[0] * AbsR[1, 1] + be[1] * AbsR[1, 0];
            if (MathF.Abs(t[0] * R[2, 2] - t[2] * R[0, 2]) > ra + rb) return false;

            // Test axis L = A2 x B0
            ra = ae[0] * AbsR[1, 0] + ae[1] * AbsR[0, 0];
            rb = be[1] * AbsR[2, 2] + be[2] * AbsR[2, 1];
            if (MathF.Abs(t[1] * R[0, 0] - t[0] * R[1, 0]) > ra + rb) return false;

            // Test axis L = A2 x B1
            ra = ae[0] * AbsR[1, 1] + ae[1] * AbsR[0, 1];
            rb = be[0] * AbsR[2, 2] + be[2] * AbsR[2, 0];
            if (MathF.Abs(t[1] * R[0, 1] - t[0] * R[1, 1]) > ra + rb) return false;

            // Test axis L = A2 x B2
            ra = ae[0] * AbsR[1, 2] + ae[1] * AbsR[0, 2];
            rb = be[0] * AbsR[2, 1] + be[1] * AbsR[2, 0];
            if (MathF.Abs(t[1] * R[0, 2] - t[0] * R[1, 2]) > ra + rb) return false;

            // Since no separating axis is found, the OBBs must be intersecting
            return true;
        }


        /// <summary>
        ///     Tests if this OBB intersects with another AABB.
        /// </summary>
        /// <param name="box">The other AABB to test</param>
        /// <returns>True if the two boxes intersect</returns>
        public bool Intersects(AxisAlignedBoundingBox box)
        {
            return Intersects(FromAABB(box));
        }

        /// <summary>
        ///     Tests if this OBB intersects with a Ray.
        /// </summary>
        /// <param name="ray">The ray to test</param>
        /// <param name="result">The length in the ray direction from the ray origin</param>
        /// <returns>True if the OBB intersects the Ray</returns>
        public bool Intersects(Ray ray, out float? result)
        {
            //Transform Ray to OBB-Space
            var rayOrigin = ray.Position;
            var rayDestination = rayOrigin + ray.Direction;


            var rayOriginInOBBSpace = ToOBBSpace(rayOrigin);
            var rayDestinationInOBBSpace = ToOBBSpace(rayDestination);

            var rayInOBBSpace = new Ray(rayOriginInOBBSpace, Vector3.Normalize(rayDestinationInOBBSpace - rayOriginInOBBSpace));

            var enclosingBox = new AxisAlignedBoundingBox(-Size * 0.5f, Size * 0.5f);

            // Perform Ray-AABB intersection
            var testResult = enclosingBox.Intersects(rayInOBBSpace);
            result = testResult;

            return testResult != null;
        }


        /// <summary>
        ///     Tests if this OBB intersects with a Sphere.
        /// </summary>
        /// <param name="sphere">The sphere to test</param>
        /// <returns>True if the OBB intersects the Sphere</returns>
        public bool Intersects(BoundingSphere sphere)
        {
            // Transform sphere to OBB-Space
            var obbSpaceSphere = new BoundingSphere(ToOBBSpace(sphere.Center), sphere.Radius);

            var aabb = new AxisAlignedBoundingBox(-Size * 0.5f, Size * 0.5f);

            return aabb.Intersects(obbSpaceSphere);
        }



        /// <summary>
        ///     Tests the intersection between the OBB and a Plane.
        /// </summary>
        /// <param name="plane">The plane to test</param>
        /// <returns>Front if the OBB is in front of the plane, back if it is behind, and intersecting if it intersects with the plane</returns>
        public PlaneIntersectionType Intersects(Plane plane)
        {
            // Maximum extent in direction of plane normal 
            var normal = Vector3.Transform(plane.Normal, Orientation);

            var half = Size * 0.5f;
            var r = MathF.Abs(half.X * normal.X)
                + MathF.Abs(half.Y * normal.Y)
                + MathF.Abs(half.Z * normal.Z);

            // signed distance between box center and plane
            var d = Vector3.Dot(plane.Normal, Position) + plane.D;


            // Return signed distance
            if (MathF.Abs(d) < r)
                return PlaneIntersectionType.Intersecting;
            else if (d < 0.0f)
                return PlaneIntersectionType.Front;
            else
                return PlaneIntersectionType.Back;
        }

        /// <summary>
        ///     Tests the intersection between the OBB and a Frustum.
        /// </summary>
        /// <param name="frustum">The frustum to test</param>
        /// <returns>True if the OBB intersects with the Frustum, false otherwise</returns>
        public bool Intersects(BoundingFrustum frustum)
        {
            var planes = new[]
            {
                frustum.Left,
                frustum.Right,
                frustum.Far,
                frustum.Near,
                frustum.Bottom,
                frustum.Top
            };

            for (var faceIndex = 0; faceIndex < 6; ++faceIndex)
            {
                var side = Intersects(planes[faceIndex]);
                if (side == PlaneIntersectionType.Back)
                    return false;
        }
            return true;
        }

        /// <summary>
        ///     Converts a point from OBB-Space to World-Space.
        /// </summary>
        /// <param name="point">Point in OBB-Space</param>
        /// <returns>The point in World-Space</returns>
        public Vector3 ToWorldSpace(Vector3 point)
        {
            return Position + Vector3.Transform(point, Orientation);
        }

    }
}
