using System.Numerics;

namespace Phoenix.Framework.Collisions
{
    /// <summary>
    /// Represents an axis-aligned bounding box (AABB) in 3D space.
    /// </summary>
    public class AxisAlignedBoundingBox : IEquatable<AxisAlignedBoundingBox>
    {

        /// <summary>
        ///   The minimum extent of this <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        public Vector3 Min;

        /// <summary>
        ///   The maximum extent of this <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        public Vector3 Max;

        /// <summary>
        /// The extents of this bounding box
        /// </summary>
        public Vector3 Size;

        /// <summary>
        /// The positiion of this bounding box
        /// </summary>
        public Vector3 Position;


        /// <summary>
        ///   The number of corners in a <see cref="AxisAlignedBoundingBox"/>. This is equal to 8.
        /// </summary>
        public const int CornerCount = 8;



        private Vector3 _halfSize;
        internal Matrix4x4 _world;

        /// <summary>
        ///   Create a<see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        /// <param name = "center" > The position of the <see cref = "AxisAlignedBoundingBox" />.</ param >
        /// <param name = "size" > The size of the <see cref = "AxisAlignedBoundingBox" />.</ param >
        public AxisAlignedBoundingBox(Vector3 center, Vector3 size)
        {
            Size = size;
            _halfSize = size * 0.5f;
            Position = center;
            Min = Position - _halfSize;
            Max = Position + _halfSize;

            _world = Matrix4x4.CreateScale(Size) * Matrix4x4.CreateTranslation(Position);
        }



        #region Public Methods

        public void Update(Vector3 position)
        {
            Position = position;
            Min = Position - _halfSize;
            Max = Position + _halfSize;

            _world = Matrix4x4.CreateScale(Size) * Matrix4x4.CreateTranslation(Position);
        }


        /// <summary>
        ///   Check if this <see cref="AxisAlignedBoundingBox"/> contains another <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        /// <param name="box">The <see cref="AxisAlignedBoundingBox"/> to test for overlap.</param>
        /// <returns>
        ///   A value indicating if this <see cref="AxisAlignedBoundingBox"/> contains,
        ///   intersects with or is disjoint with <paramref name="box"/>.
        /// </returns>
        public ContainmentType Contains(AxisAlignedBoundingBox box)
        {
            //test if all corner is in the same side of a face by just checking min and max
            if (box.Max.X < Min.X
                || box.Min.X > Max.X
                || box.Max.Y < Min.Y
                || box.Min.Y > Max.Y
                || box.Max.Z < Min.Z
                || box.Min.Z > Max.Z)
                return ContainmentType.Disjoint;


            if (box.Min.X >= Min.X
                && box.Max.X <= Max.X
                && box.Min.Y >= Min.Y
                && box.Max.Y <= Max.Y
                && box.Min.Z >= Min.Z
                && box.Max.Z <= Max.Z)
                return ContainmentType.Contains;

            return ContainmentType.Intersects;
        }

        /// <summary>
        ///   Check if this <see cref="AxisAlignedBoundingBox"/> contains another <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        /// <param name="box">The <see cref="AxisAlignedBoundingBox"/> to test for overlap.</param>
        /// <param name="result">
        ///   A value indicating if this <see cref="AxisAlignedBoundingBox"/> contains,
        ///   intersects with or is disjoint with <paramref name="box"/>.
        /// </param>
        public void Contains(ref AxisAlignedBoundingBox box, out ContainmentType result)
        {
            result = Contains(box);
        }

        /// <summary>
        ///   Determines if this <see cref="AxisAlignedBoundingBox"/> contains or intersects with a specified <see cref="BoundingFrustum"/>.
        ///   NOTE: This method may return false positives (indicating an intersection or containment when there is none)
        ///   to improve performance. Use with caution if precision is critical.
        /// </summary>
        /// <param name="frustum">The <see cref="BoundingFrustum"/> to test for overlap.</param>
        /// <returns>
        ///   A <see cref="ContainmentType"/> value indicating whether this <see cref="AxisAlignedBoundingBox"/>
        ///   contains or intersects the <paramref name="frustum"/>.
        /// </returns>
        public ContainmentType Contains(BoundingFrustum frustum)
        {
            int i;
            Vector3[] corners = frustum.GetCorners();

            // First we check every corner of a frustum
            for (i = 0; i < corners.Length; i++)
            {
                if (Contains(corners[i]) == ContainmentType.Disjoint)
                    break;
            }

            if (i == corners.Length) // This means we checked all the corners and they were all contain or instersect
                return ContainmentType.Contains;

            if (i != 0)              // If i is not equal to zero, we can fastpath and say that this box intersects
                return ContainmentType.Intersects;


            // If we get here, it means the first (and only) point we checked was disjoint from frustum.
            // So we assume that if all other points of frustum are inside the box, then box contains the frustum.
            // Otherwise we exit immediately saying that the result is Intersects
            i++;
            for (; i < corners.Length; i++)
            {
                if (Contains(corners[i]) != ContainmentType.Contains)
                    return ContainmentType.Intersects;
            }

            // If we get here, then we know that only one point is disjoint, therefore result is Contains
            return ContainmentType.Contains;
        }

        /// <summary>
        ///   Determines if this <see cref="AxisAlignedBoundingBox"/> contains or intersects with a specified <see cref="BoundingFrustum"/>.
        ///   This method is precise and will significantly affect performance.
        /// </summary>
        /// <param name="frustum">The <see cref="BoundingFrustum"/> to test for overlap.</param>
        /// <returns>
        ///   A <see cref="ContainmentType"/> value indicating whether this <see cref="AxisAlignedBoundingBox"/>
        ///   contains or intersects the <paramref name="frustum"/>.
        /// </returns>
        public ContainmentType ContainsPrecise(BoundingFrustum frustum)
        {
            Vector3[] boxNormals = new Vector3[]
            {
                Vector3.UnitY,
                Vector3.UnitX,
                -Vector3.UnitZ,
            };

            Vector3[] frustumNormals = new Vector3[]
            {
                frustum.Left.Normal,
                frustum.Right.Normal,
                frustum.Top.Normal,
                frustum.Bottom.Normal,
                frustum.Far.Normal
            };

            // allAxes = box normals + frustum normals + cross products of box normals and frustum normals
            Vector3[] allAxes = new Vector3[23]; // 3 + 5 + 3 * 5

            allAxes[0] = boxNormals[0];
            allAxes[1] = boxNormals[1];
            allAxes[2] = boxNormals[2];

            for (int i = 0; i < frustumNormals.Length; i++)
            {
                allAxes[3 + i] = frustumNormals[i];
                for (int j = 0; j < boxNormals.Length; j++)
                {
                    allAxes[8 + i * boxNormals.Length + j] = Vector3.Cross(frustumNormals[i], boxNormals[j]);
                }
            }

            var boxCorners = GetCorners();
            var frustumCorners = frustum.GetCorners();

            bool intersects = false;

            for (int i = 0; i < allAxes.Length; i++)
            {
                // Project both shapes on the axis

                float boxMin = float.MaxValue, boxMax = float.MinValue;
                float frustumMin = float.MaxValue, frustumMax = float.MinValue;

                foreach (var point in boxCorners)
                {
                    var dot = Vector3.Dot(point, allAxes[i]);
                    if (boxMin > dot) boxMin = dot;
                    if (boxMax < dot) boxMax = dot;
                }

                foreach (var point in frustumCorners)
                {
                    var dot = Vector3.Dot(point, allAxes[i]);
                    if (frustumMin > dot) frustumMin = dot;
                    if (frustumMax < dot) frustumMax = dot;
                }

                // If we find a gap, we are sure the shapes are disjoint
                if (boxMax < frustumMin || boxMin > frustumMax)
                    return ContainmentType.Disjoint;
                // If frustum projection isn't contained inside box projection - there is an intersection
                else if (boxMax < frustumMax || boxMin > frustumMin)
                    intersects = true;
            }

            if (intersects)
                return ContainmentType.Intersects;

            return ContainmentType.Contains;
        }

        /// <summary>
        ///   Check if this <see cref="AxisAlignedBoundingBox"/> contains a <see cref="BoundingSphere"/>.
        /// </summary>
        /// <param name="sphere">The <see cref="BoundingSphere"/> to test for overlap.</param>
        /// <returns>
        ///   A value indicating if this <see cref="AxisAlignedBoundingBox"/> contains,
        ///   intersects with or is disjoint with <paramref name="sphere"/>.
        /// </returns>
        public ContainmentType Contains(BoundingSphere sphere)
        {
            if (sphere.Center.X - Min.X >= sphere.Radius
                && sphere.Center.Y - Min.Y >= sphere.Radius
                && sphere.Center.Z - Min.Z >= sphere.Radius
                && Max.X - sphere.Center.X >= sphere.Radius
                && Max.Y - sphere.Center.Y >= sphere.Radius
                && Max.Z - sphere.Center.Z >= sphere.Radius)
                return ContainmentType.Contains;

            double dmin = 0;

            double e = sphere.Center.X - Min.X;
            if (e < 0)
            {
                if (e < -sphere.Radius)
                {
                    return ContainmentType.Disjoint;
                }
                dmin += e * e;
            }
            else
            {
                e = sphere.Center.X - Max.X;
                if (e > 0)
                {
                    if (e > sphere.Radius)
                    {
                        return ContainmentType.Disjoint;
                    }
                    dmin += e * e;
                }
            }

            e = sphere.Center.Y - Min.Y;
            if (e < 0)
            {
                if (e < -sphere.Radius)
                {
                    return ContainmentType.Disjoint;
                }
                dmin += e * e;
            }
            else
            {
                e = sphere.Center.Y - Max.Y;
                if (e > 0)
                {
                    if (e > sphere.Radius)
                    {
                        return ContainmentType.Disjoint;
                    }
                    dmin += e * e;
                }
            }

            e = sphere.Center.Z - Min.Z;
            if (e < 0)
            {
                if (e < -sphere.Radius)
                {
                    return ContainmentType.Disjoint;
                }
                dmin += e * e;
            }
            else
            {
                e = sphere.Center.Z - Max.Z;
                if (e > 0)
                {
                    if (e > sphere.Radius)
                    {
                        return ContainmentType.Disjoint;
                    }
                    dmin += e * e;
                }
            }

            if (dmin <= sphere.Radius * sphere.Radius)
                return ContainmentType.Intersects;

            return ContainmentType.Disjoint;
        }


        /// <summary>
        ///   Check if this <see cref="AxisAlignedBoundingBox"/> contains a point.
        /// </summary>
        /// <param name="point">The <see cref="Vector3"/> to test.</param>
        /// <returns>
        ///   <see cref="ContainmentType.Contains"/> if this <see cref="AxisAlignedBoundingBox"/> contains
        ///   <paramref name="point"/> or <see cref="ContainmentType.Disjoint"/> if it does not.
        /// </returns>
        public ContainmentType Contains(Vector3 point)
        {
            var isOut = point.X < this.Min.X
                || point.X > this.Max.X
                || point.Y < this.Min.Y
                || point.Y > this.Max.Y
                || point.Z < this.Min.Z
                || point.Z > this.Max.Z;

            return isOut ? ContainmentType.Disjoint : ContainmentType.Contains;
        }

        private static readonly Vector3 MaxVector3 = new Vector3(float.MaxValue);
        private static readonly Vector3 MinVector3 = new Vector3(float.MinValue);

        /// <summary>
        ///   Create the enclosing <see cref="AxisAlignedBoundingBox"/> of a <see cref="BoundingSphere"/>.
        /// </summary>
        /// <param name="sphere">The <see cref="BoundingSphere"/> to enclose.</param>
        /// <returns>A <see cref="AxisAlignedBoundingBox"/> enclosing <paramref name="sphere"/>.</returns>
        public static AxisAlignedBoundingBox CreateFromSphere(BoundingSphere sphere)
        {
            var size = new Vector3(sphere.Radius);
            return new AxisAlignedBoundingBox(sphere.Center, size);
        }

        /// <summary>
        ///   Check if two <see cref="AxisAlignedBoundingBox"/> instances are equal.
        /// </summary>
        /// <param name="other">The <see cref="AxisAlignedBoundingBox"/> to compare with this <see cref="AxisAlignedBoundingBox"/>.</param>
        /// <returns>
        ///   <code>true</code> if <paramref name="other"/> is equal to this <see cref="AxisAlignedBoundingBox"/>,
        ///   <code>false</code> if it is not.
        /// </returns>
        public bool Equals(AxisAlignedBoundingBox? other)
        {
            return (Min == other?.Min) && (Max == other.Max);
        }

        /// <summary>
        ///   Check if two <see cref="AxisAlignedBoundingBox"/> instances are equal.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with this <see cref="AxisAlignedBoundingBox"/>.</param>
        /// <returns>
        ///   <code>true</code> if <paramref name="obj"/> is equal to this <see cref="AxisAlignedBoundingBox"/>,
        ///   <code>false</code> if it is not.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return (obj is AxisAlignedBoundingBox) && Equals((AxisAlignedBoundingBox)obj);
        }

        /// <summary>
        ///   Get an array of <see cref="Vector3"/> containing the corners of this <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        /// <returns>An array of <see cref="Vector3"/> containing the corners of this <see cref="AxisAlignedBoundingBox"/>.</returns>
        public Vector3[] GetCorners()
        {
            return [
                new Vector3(Min.X, Max.Y, Max.Z),
                new Vector3(Max.X, Max.Y, Max.Z),
                new Vector3(Max.X, Min.Y, Max.Z),
                new Vector3(Min.X, Min.Y, Max.Z),
                new Vector3(Min.X, Max.Y, Min.Z),
                new Vector3(Max.X, Max.Y, Min.Z),
                new Vector3(Max.X, Min.Y, Min.Z),
                new Vector3(Min.X, Min.Y, Min.Z)
            ];
        }

        /// <summary>
        ///   Get the hash code for this <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        /// <returns>A hash code for this <see cref="AxisAlignedBoundingBox"/>.</returns>
        public override int GetHashCode()
        {
            return this.Min.GetHashCode() + this.Max.GetHashCode();
        }

        /// <summary>
        ///   Check if this <see cref="AxisAlignedBoundingBox"/> intersects another <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        /// <param name="box">The <see cref="AxisAlignedBoundingBox"/> to test for intersection.</param>
        /// <returns>
        ///   <code>true</code> if this <see cref="AxisAlignedBoundingBox"/> intersects <paramref name="box"/>,
        ///   <code>false</code> if it does not.
        /// </returns>
        public bool Intersects(AxisAlignedBoundingBox box)
        {
            if ((this.Max.X >= box.Min.X) && (this.Min.X <= box.Max.X))
            {
                if ((this.Max.Y < box.Min.Y) || (this.Min.Y > box.Max.Y))
                {
                    return false;
                }

                return (this.Max.Z >= box.Min.Z) && (this.Min.Z <= box.Max.Z);
            }
            return false;
        }

        /// <summary>
        ///   Check if this <see cref="AxisAlignedBoundingBox"/> intersects a <see cref="BoundingFrustum"/>.
        /// </summary>
        /// <param name="frustum">The <see cref="BoundingFrustum"/> to test for intersection.</param>
        /// <returns>
        ///   <code>true</code> if this <see cref="AxisAlignedBoundingBox"/> intersects <paramref name="frustum"/>,
        ///   <code>false</code> if it does not.
        /// </returns>
        public bool Intersects(BoundingFrustum frustum)
        {
            return frustum.Intersects(this);
        }

        /// <summary>
        ///   Check if this <see cref="AxisAlignedBoundingBox"/> intersects a <see cref="BoundingFrustum"/>.
        /// </summary>
        /// <param name="sphere">The <see cref="BoundingFrustum"/> to test for intersection.</param>
        /// <returns>
        ///   <code>true</code> if this <see cref="AxisAlignedBoundingBox"/> intersects <paramref name="sphere"/>,
        ///   <code>false</code> if it does not.
        /// </returns>
        public bool Intersects(BoundingSphere sphere)
        {
            var squareDistance = 0.0f;
            var point = sphere.Center;
            if (point.X < Min.X) squareDistance += (Min.X - point.X) * (Min.X - point.X);
            if (point.X > Max.X) squareDistance += (point.X - Max.X) * (point.X - Max.X);
            if (point.Y < Min.Y) squareDistance += (Min.Y - point.Y) * (Min.Y - point.Y);
            if (point.Y > Max.Y) squareDistance += (point.Y - Max.Y) * (point.Y - Max.Y);
            if (point.Z < Min.Z) squareDistance += (Min.Z - point.Z) * (Min.Z - point.Z);
            if (point.Z > Max.Z) squareDistance += (point.Z - Max.Z) * (point.Z - Max.Z);
            return squareDistance <= sphere.Radius * sphere.Radius;
        }

        /// <summary>
        ///   Check if this <see cref="AxisAlignedBoundingBox"/> intersects a <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">The <see cref="Plane"/> to test for intersection.</param>
        /// <returns>
        ///   <code>true</code> if this <see cref="AxisAlignedBoundingBox"/> intersects <paramref name="plane"/>,
        ///   <code>false</code> if it does not.
        /// </returns>
        public PlaneIntersectionType Intersects(Plane plane)
        {
            // See https://cgvr.informatik.uni-bremen.de/teaching/cg_literatur/lighthouse3d_view_frustum_culling/index.html

            Vector3 positiveVertex;
            Vector3 negativeVertex;

            if (plane.Normal.X >= 0)
            {
                positiveVertex.X = Max.X;
                negativeVertex.X = Min.X;
            }
            else
            {
                positiveVertex.X = Min.X;
                negativeVertex.X = Max.X;
            }

            if (plane.Normal.Y >= 0)
            {
                positiveVertex.Y = Max.Y;
                negativeVertex.Y = Min.Y;
            }
            else
            {
                positiveVertex.Y = Min.Y;
                negativeVertex.Y = Max.Y;
            }

            if (plane.Normal.Z >= 0)
            {
                positiveVertex.Z = Max.Z;
                negativeVertex.Z = Min.Z;
            }
            else
            {
                positiveVertex.Z = Min.Z;
                negativeVertex.Z = Max.Z;
            }

            // Inline Vector3.Dot(plane.Normal, negativeVertex) + plane.D;
            var distance = plane.Normal.X * negativeVertex.X + plane.Normal.Y * negativeVertex.Y + plane.Normal.Z * negativeVertex.Z + plane.D;
            if (distance > 0)
            {
                return PlaneIntersectionType.Front;
            }

            // Inline Vector3.Dot(plane.Normal, positiveVertex) + plane.D;
            distance = plane.Normal.X * positiveVertex.X + plane.Normal.Y * positiveVertex.Y + plane.Normal.Z * positiveVertex.Z + plane.D;
            if (distance < 0)
            {
                return PlaneIntersectionType.Back;
            }

            return PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        ///   Check if this <see cref="AxisAlignedBoundingBox"/> intersects a <see cref="Ray"/>.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> to test for intersection.</param>
        /// <returns>
        ///   The distance along the <see cref="Ray"/> to the intersection point or
        ///   <code>null</code> if the <see cref="Ray"/> does not intesect this <see cref="AxisAlignedBoundingBox"/>.
        /// </returns>
        public Nullable<float> Intersects(Ray ray)
        {
            return ray.Intersects(this);
        }

        /// <summary>
        ///   Check if two <see cref="AxisAlignedBoundingBox"/> instances are equal.
        /// </summary>
        /// <param name="a">A <see cref="AxisAlignedBoundingBox"/> to compare the other.</param>
        /// <param name="b">A <see cref="AxisAlignedBoundingBox"/> to compare the other.</param>
        /// <returns>
        ///   <code>true</code> if <paramref name="a"/> is equal to this <paramref name="b"/>,
        ///   <code>false</code> if it is not.
        /// </returns>
        public static bool operator ==(AxisAlignedBoundingBox a, AxisAlignedBoundingBox b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///   Check if two <see cref="AxisAlignedBoundingBox"/> instances are not equal.
        /// </summary>
        /// <param name="a">A <see cref="AxisAlignedBoundingBox"/> to compare the other.</param>
        /// <param name="b">A <see cref="AxisAlignedBoundingBox"/> to compare the other.</param>
        /// <returns>
        ///   <code>true</code> if <paramref name="a"/> is not equal to this <paramref name="b"/>,
        ///   <code>false</code> if it is.
        /// </returns>
        public static bool operator !=(AxisAlignedBoundingBox a, AxisAlignedBoundingBox b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Get a <see cref="String"/> representation of this <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> representation of this <see cref="AxisAlignedBoundingBox"/>.</returns>
        public override string ToString()
        {
            return "{{Min:" + this.Min.ToString() + " Max:" + this.Max.ToString() + "}}";
        }

        /// <summary>
        /// Deconstruction method for <see cref="AxisAlignedBoundingBox"/>.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void Deconstruct(out Vector3 min, out Vector3 max)
        {
            min = Min;
            max = Max;
        }

        #endregion Public Methods
    }
}
