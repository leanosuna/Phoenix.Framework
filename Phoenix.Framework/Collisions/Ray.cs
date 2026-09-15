using System.Numerics;

namespace Phoenix.Framework.Collisions
{
    /// <summary>
    /// Represents a ray with an origin and a direction in 3D space.
    /// </summary>
    public class Ray : IEquatable<Ray>
    {
        #region Public Fields

        /// <summary>
        /// The direction of this <see cref="Ray"/>.
        /// </summary>
        public Vector3 Direction;

        /// <summary>
        /// The origin of this <see cref="Ray"/>.
        /// </summary>
        public Vector3 Position;

        #endregion


        #region Public Constructors

        /// <summary>
        /// Create a <see cref="Ray"/>.
        /// </summary>
        /// <param name="position">The origin of the <see cref="Ray"/>.</param>
        /// <param name="direction">The direction of the <see cref="Ray"/>.</param>
        public Ray(Vector3 position, Vector3 direction)
        {
            this.Position = position;
            this.Direction = direction;
        }

        #endregion


        #region Public Methods

        public void Update(Vector3 position, Vector3? direction)
        {
            Position = position;
            if (direction.HasValue)
                Direction = direction.Value;
        }

        /// <summary>
        /// Check if the specified <see cref="Object"/> is equal to this <see cref="Ray"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to test for equality with this <see cref="Ray"/>.</param>
        /// <returns>
        /// <code>true</code> if the specified <see cref="Object"/> is equal to this <see cref="Ray"/>,
        /// <code>false</code> if it is not.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return (obj is Ray) && Equals((Ray)obj);
        }

        /// <summary>
        /// Check if the specified <see cref="Ray"/> is equal to this <see cref="Ray"/>.
        /// </summary>
        /// <param name="other">The <see cref="Ray"/> to test for equality with this <see cref="Ray"/>.</param>
        /// <returns>
        /// <code>true</code> if the specified <see cref="Ray"/> is equal to this <see cref="Ray"/>,
        /// <code>false</code> if it is not.
        /// </returns>
        public bool Equals(Ray? other)
        {
            return other is not null && Position.Equals(other.Position) && Direction.Equals(other.Direction);
        }

        /// <summary>
        /// Get a hash code for this <see cref="Ray"/>.
        /// </summary>
        /// <returns>A hash code for this <see cref="Ray"/>.</returns>
        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Direction.GetHashCode();
        }

        //TODO: add hit position, add triangle intersect with hit position

        // adapted from http://www.scratchapixel.com/lessons/3d-basic-lessons/lesson-7-intersecting-simple-shapes/ray-box-intersection/
        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        /// <param name="box">The <see cref="AxisAlignedBoundingBox"/> to test for intersection.</param>
        /// <returns>
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="AxisAlignedBoundingBox"/>.
        /// </returns>
        public float? Intersects(AxisAlignedBoundingBox box)
        {
            const float Epsilon = 1e-6f;

            float? tMin = null, tMax = null;

            if (Math.Abs(Direction.X) < Epsilon)
            {
                if (Position.X < box.Min.X || Position.X > box.Max.X)
                    return null;
            }
            else
            {
                tMin = (box.Min.X - Position.X) / Direction.X;
                tMax = (box.Max.X - Position.X) / Direction.X;

                if (tMin > tMax)
                {
                    var temp = tMin;
                    tMin = tMax;
                    tMax = temp;
                }
            }

            if (Math.Abs(Direction.Y) < Epsilon)
            {
                if (Position.Y < box.Min.Y || Position.Y > box.Max.Y)
                    return null;
            }
            else
            {
                var tMinY = (box.Min.Y - Position.Y) / Direction.Y;
                var tMaxY = (box.Max.Y - Position.Y) / Direction.Y;

                if (tMinY > tMaxY)
                {
                    var temp = tMinY;
                    tMinY = tMaxY;
                    tMaxY = temp;
                }

                if ((tMin.HasValue && tMin > tMaxY) || (tMax.HasValue && tMinY > tMax))
                    return null;

                if (!tMin.HasValue || tMinY > tMin) tMin = tMinY;
                if (!tMax.HasValue || tMaxY < tMax) tMax = tMaxY;
            }

            if (Math.Abs(Direction.Z) < Epsilon)
            {
                if (Position.Z < box.Min.Z || Position.Z > box.Max.Z)
                    return null;
            }
            else
            {
                var tMinZ = (box.Min.Z - Position.Z) / Direction.Z;
                var tMaxZ = (box.Max.Z - Position.Z) / Direction.Z;

                if (tMinZ > tMaxZ)
                {
                    var temp = tMinZ;
                    tMinZ = tMaxZ;
                    tMaxZ = temp;
                }

                if ((tMin.HasValue && tMin > tMaxZ) || (tMax.HasValue && tMinZ > tMax))
                    return null;

                if (!tMin.HasValue || tMinZ > tMin) tMin = tMinZ;
                if (!tMax.HasValue || tMaxZ < tMax) tMax = tMaxZ;
            }

            // having a positive tMax and a negative tMin means the ray is inside the box
            // we expect the intesection distance to be 0 in that case
            if ((tMin.HasValue && tMin < 0) && tMax > 0) return 0;

            // a negative tMin means that the intersection point is behind the ray's origin
            // we discard these as not hitting the AABB
            if (tMin < 0) return null;

            return tMin;
        }
        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="BoundingSphere"/>.
        /// </summary>
        /// <param name="sphere">The <see cref="BoundingSphere"/> to test for intersection.</param>
        /// <returns>
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="BoundingSphere"/>.
        /// </returns>
        public float? Intersects(BoundingSphere sphere)
        {
            float? result;
            // Find the vector between where the ray starts the the sphere's centre
            Vector3 difference = sphere.Center - this.Position;

            float differenceLengthSquared = difference.LengthSquared();
            float sphereRadiusSquared = sphere.Radius * sphere.Radius;


            // If the distance between the ray start and the sphere's centre is less than
            // the radius of the sphere, it means we've intersected. N.B. checking the LengthSquared is faster.
            if (differenceLengthSquared < sphereRadiusSquared)
            {
                return 0.0f;
            }

            float distanceAlongRay = Vector3.Dot(Direction, difference);
            // If the ray is pointing away from the sphere then we don't ever intersect
            if (distanceAlongRay < 0)
            {
                return null;

            }

            // Next we kinda use Pythagoras to check if we are within the bounds of the sphere
            // if x = radius of sphere
            // if y = distance between ray position and sphere centre
            // if z = the distance we've travelled along the ray
            // if x^2 + z^2 - y^2 < 0, we do not intersect
            float dist = sphereRadiusSquared + distanceAlongRay * distanceAlongRay - differenceLengthSquared;

            result = (dist < 0) ? null : distanceAlongRay - (float?)MathF.Sqrt(dist);
            return result;
        }

        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">The <see cref="Plane"/> to test for intersection.</param>
        /// <returns>
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="Plane"/>.
        /// </returns>
        public float? Intersects(Plane plane)
        {
            float? result;
            Intersects(ref plane, out result);
            return result;
        }

        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">The <see cref="Plane"/> to test for intersection.</param>
        /// <param name="result">
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="Plane"/>.
        /// </param>
        public void Intersects(ref Plane plane, out float? result)
        {
            var den = Vector3.Dot(Direction, plane.Normal);
            if (Math.Abs(den) < 0.00001f)
            {
                result = null;
                return;
            }

            result = (-plane.D - Vector3.Dot(plane.Normal, Position)) / den;

            if (result < 0.0f)
            {
                if (result < -0.00001f)
                {
                    result = null;
                    return;
                }

                result = 0.0f;
            }
        }

        public Vector3? Intersects(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            // Tolerance for floating-point comparisons
            float epsilon = 1e-8f;

            // Find vectors for two edges sharing v0
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;

            // Calculate the determinant
            Vector3 h = Vector3.Cross(Direction, edge2);
            float a = Vector3.Dot(edge1, h);

            // If the determinant is near zero, the ray is parallel to the triangle
            if (a > -epsilon && a < epsilon)
            {
                return null;  // No intersection
            }

            // Calculate f, u, and v
            float f = 1.0f / a;
            Vector3 s = Position - v0;
            float u = f * Vector3.Dot(s, h);

            // If the intersection is outside the triangle
            if (u < 0.0f || u > 1.0f)
            {
                return null;
            }

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(Direction, q);

            // The intersection is outside the triangle
            if (v < 0.0f || u + v > 1.0f)
            {
                return null;
            }

            // Calculate where the intersection point is on the ray
            float t = f * Vector3.Dot(edge2, q);

            // If t is greater than epsilon, the intersection is on the ray
            if (t > epsilon)
            {
                // Calculate the exact intersection point
                return Position + Direction * t;
            }

            // No intersection; the ray does not hit the triangle
            return null;
        }


        /// <summary>
        /// Check if two rays are not equal.
        /// </summary>
        /// <param name="a">A ray to check for inequality.</param>
        /// <param name="b">A ray to check for inequality.</param>
        /// <returns><code>true</code> if the two rays are not equal, <code>false</code> if they are.</returns>
        public static bool operator !=(Ray a, Ray b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Check if two rays are equal.
        /// </summary>
        /// <param name="a">A ray to check for equality.</param>
        /// <param name="b">A ray to check for equality.</param>
        /// <returns><code>true</code> if the two rays are equals, <code>false</code> if they are not.</returns>
        public static bool operator ==(Ray a, Ray b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Get a <see cref="String"/> representation of this <see cref="Ray"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> representation of this <see cref="Ray"/>.</returns>
        public override string ToString()
        {
            return "{{Position:" + Position.ToString() + " Direction:" + Direction.ToString() + "}}";
        }

        /// <summary>
        /// Deconstruction method for <see cref="Ray"/>.
        /// </summary>
        /// <param name="position">Receives the start position of the ray.</param>
        /// <param name="direction">Receives the direction of the ray.</param>
        public void Deconstruct(out Vector3 position, out Vector3 direction)
        {
            position = Position;
            direction = Direction;
        }

        #endregion
    }
}
