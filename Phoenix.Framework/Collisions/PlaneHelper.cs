using System.Numerics;

namespace Phoenix.Framework.Collisions
{
    public static class PlaneHelper
    {
        /// <summary>
        /// Determines the relative position of a point with respect to a plane.
        /// </summary>
        /// <param name="plane">The plane against which the point is classified.</param>
        /// <param name="point">The point to classify, represented as a <see cref="Vector3"/>.</param>
        /// <returns>A value indicating the relative position of the point to the plane:         a positive value if the point is
        /// in the direction of the plane's normal,         a negative value if the point is in the opposite direction,
        /// and zero if the point lies on the plane.</returns>
        public static float ClassifyPoint(this Plane plane, Vector3 point)
        {
            return point.X * plane.Normal.X + point.Y * plane.Normal.Y + point.Z * plane.Normal.Z + plane.D;
        }
        /// <summary>
        /// Calculates the shortest distance from a specified point to the given plane.
        /// </summary>
        /// <remarks>The distance is calculated using the formula: <c>distance = |(ax + by + cz + d) /
        /// sqrt(a² + b² + c²)|</c>, where <c>(a, b, c)</c> is the normal vector of the plane, and <c>d</c> is the
        /// plane's distance from the origin.</remarks>
        /// <param name="plane">The plane to which the distance is calculated.</param>
        /// <param name="point">The point from which the distance to the plane is measured.</param>
        /// <returns>The perpendicular distance between the specified point and the plane. The value is always non-negative.</returns>
        public static float PerpendicularDistance(this Plane plane, Vector3 point)
        {
            // dist = (ax + by + cz + d) / sqrt(a*a + b*b + c*c)
            return (float)Math.Abs((plane.Normal.X * point.X + plane.Normal.Y * point.Y + plane.Normal.Z * point.Z + plane.D)
                                    / Math.Sqrt(plane.Normal.X * plane.Normal.X + plane.Normal.Y * plane.Normal.Y + plane.Normal.Z * plane.Normal.Z));
        }

        /// <summary>
        /// Transforms a normalized plane by a matrix.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed plane.</returns>
        public static Plane Transform(this Plane plane, Matrix4x4 matrix)
        {
            Matrix4x4.Invert(matrix, out var inverted);
            var transposeInverted = Matrix4x4.Transpose(inverted);

            var vec4 = plane.AsVector4();

            vec4 = Vector4.Transform(vec4, transposeInverted);

            return new Plane(vec4);
        }

        /// <summary>
        /// Transforms a normalized plane by a quaternion rotation.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="rotation">The quaternion rotation.</param>
        /// <returns>The transformed plane.</returns>
        public static Plane Transform(this Plane plane, Quaternion rotation)
        {
            var vec4 = plane.AsVector4();

            vec4 = Vector4.Transform(vec4, rotation);

            return new Plane(vec4);
        }
    }
}
