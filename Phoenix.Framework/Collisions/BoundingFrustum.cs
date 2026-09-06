using System.Numerics;

namespace Phoenix.Framework.Collisions
{
    public class BoundingFrustum : IEquatable<BoundingFrustum>
    {
        #region Private Fields

        private readonly Vector3[] _corners = new Vector3[CornerCount];
        private readonly Plane[] _planes = new Plane[PlaneCount];

        private Matrix4x4 _matrix;
        #endregion

        #region Public Fields

        /// <summary>
        /// The number of planes in the frustum.
        /// </summary>
        public const int PlaneCount = 6;

        /// <summary>
        /// The number of corner points in the frustum.
        /// </summary>
        public const int CornerCount = 8;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="Matrix"/> of the frustum.
        /// </summary>
        public Matrix4x4 Matrix
        {
            get { return this._matrix; }
            set
            {
                this._matrix = value;
                this.CreatePlanes();    // FIXME: The odds are the planes will be used a lot more often than the matrix
                this.CreateCorners();   // is updated, so this should help performance. I hope ;)
            }
        }

        /// <summary>
        /// Gets the near plane of the frustum.
        /// </summary>
        public Plane Near
        {
            get { return this._planes[0]; }
        }

        /// <summary>
        /// Gets the far plane of the frustum.
        /// </summary>
        public Plane Far
        {
            get { return this._planes[1]; }
        }

        /// <summary>
        /// Gets the left plane of the frustum.
        /// </summary>
        public Plane Left
        {
            get { return this._planes[2]; }
        }

        /// <summary>
        /// Gets the right plane of the frustum.
        /// </summary>
        public Plane Right
        {
            get { return this._planes[3]; }
        }

        /// <summary>
        /// Gets the top plane of the frustum.
        /// </summary>
        public Plane Top
        {
            get { return this._planes[4]; }
        }

        /// <summary>
        /// Gets the bottom plane of the frustum.
        /// </summary>
        public Plane Bottom
        {
            get { return this._planes[5]; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the frustum by extracting the view planes from a matrix.
        /// </summary>
        /// <param name="value">Combined matrix which usually is (View * Projection).</param>
        public BoundingFrustum(Matrix4x4 value)
        {
            this._matrix = value;
            this.CreatePlanes();
            this.CreateCorners();
        }

        #endregion

        #region Operators

        /// <summary>
        /// Compares whether two <see cref="BoundingFrustum"/> instances are equal.
        /// </summary>
        /// <param name="a"><see cref="BoundingFrustum"/> instance on the left of the equal sign.</param>
        /// <param name="b"><see cref="BoundingFrustum"/> instance on the right of the equal sign.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(BoundingFrustum a, BoundingFrustum b)
        {
            if (Equals(a, null))
                return (Equals(b, null));

            if (Equals(b, null))
                return (Equals(a, null));

            return a._matrix == (b._matrix);
        }

        /// <summary>
        /// Compares whether two <see cref="BoundingFrustum"/> instances are not equal.
        /// </summary>
        /// <param name="a"><see cref="BoundingFrustum"/> instance on the left of the not equal sign.</param>
        /// <param name="b"><see cref="BoundingFrustum"/> instance on the right of the not equal sign.</param>
        /// <returns><c>true</c> if the instances are not equal; <c>false</c> otherwise.</returns>
        public static bool operator !=(BoundingFrustum a, BoundingFrustum b)
        {
            return !(a == b);
        }

        #endregion

        #region Public Methods

        public void Update(Matrix4x4 viewProjection)
        {
            Matrix = viewProjection;
        }



        #region Contains

        /// <summary>
        /// Containment test between this <see cref="BoundingFrustum"/> and specified <see cref="BoundingFrustum"/>.
        /// </summary>
        /// <param name="frustum">A <see cref="BoundingFrustum"/> for testing.</param>
        /// <returns>Result of testing for containment between this <see cref="BoundingFrustum"/> and specified <see cref="BoundingFrustum"/>.</returns>
        public ContainmentType Contains(BoundingFrustum frustum)
        {
            if (this == frustum)                // We check to see if the two frustums are equal
                return ContainmentType.Contains;// If they are, there's no need to go any further.

            var intersects = false;
            for (var i = 0; i < PlaneCount; ++i)
            {
                var planeIntersectionType = frustum.Intersects(_planes[i]);
                switch (planeIntersectionType)
                {
                    case PlaneIntersectionType.Front:
                        return ContainmentType.Disjoint;
                    case PlaneIntersectionType.Intersecting:
                        intersects = true;
                        break;
                }
            }
            return intersects ? ContainmentType.Intersects : ContainmentType.Contains;
        }


        /// <summary>
        /// Containment test between this <see cref="BoundingFrustum"/> and specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="point">A <see cref="Vector3"/> for testing.</param>
        /// <returns>Result of testing for containment between this <see cref="BoundingFrustum"/> and specified <see cref="Vector3"/>.</returns>
        public ContainmentType Contains(Vector3 point)
        {
            for (var i = 0; i < PlaneCount; ++i)
            {
                if (_planes[i].DotCoordinate(point) > 0)
                {
                    return ContainmentType.Disjoint;
                }
            }
            return ContainmentType.Contains;
        }

        #endregion

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="BoundingFrustum"/>.
        /// </summary>
        /// <param name="other">The <see cref="BoundingFrustum"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public bool Equals(BoundingFrustum? other)
        {
            return other is not null && this == other;
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="BoundingFrustum"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public override bool Equals(object? obj)
        {
            return (obj is BoundingFrustum) && this == ((BoundingFrustum)obj);
        }

        /// <summary>
        /// Returns a copy of internal corners array.
        /// </summary>
        /// <returns>The array of corners.</returns>
        public Vector3[] GetCorners()
        {
            return (Vector3[])this._corners.Clone();
        }

        /// <summary>
        /// Returns a copy of internal corners array.
        /// </summary>
        /// <param name="corners">The array which values will be replaced to corner values of this instance. It must have size of <see cref="BoundingFrustum.CornerCount"/>.</param>
		public void GetCorners(Vector3[] corners)
        {
            if (corners == null) throw new ArgumentNullException("corners");
            if (corners.Length < CornerCount) throw new ArgumentOutOfRangeException("corners");

            this._corners.CopyTo(corners, 0);
        }

        /// <summary>
        /// Gets the hash code of this <see cref="BoundingFrustum"/>.
        /// </summary>
        /// <returns>Hash code of this <see cref="BoundingFrustum"/>.</returns>
        public override int GetHashCode()
        {
            return this._matrix.GetHashCode();
        }

        /// <summary>
        /// Gets whether or not a specified <see cref="BoundingFrustum"/> intersects with this <see cref="BoundingFrustum"/>.
        /// </summary>
        /// <param name="frustum">An other <see cref="BoundingFrustum"/> for intersection test.</param>
        /// <returns><c>true</c> if other <see cref="BoundingFrustum"/> intersects with this <see cref="BoundingFrustum"/>; <c>false</c> otherwise.</returns>
        public bool Intersects(BoundingFrustum frustum)
        {
            return Contains(frustum) != ContainmentType.Disjoint;
        }

        /// <summary>
        /// Gets type of intersection between specified <see cref="Plane"/> and this <see cref="BoundingFrustum"/>.
        /// </summary>
        /// <param name="plane">A <see cref="Plane"/> for intersection test.</param>
        /// <returns>A plane intersection type.</returns>
        public PlaneIntersectionType Intersects(Plane plane)
        {
            PlaneIntersectionType result = plane.Intersects(ref _corners[0]);
            for (int i = 1; i < _corners.Length; i++)
                if (plane.Intersects(ref _corners[i]) != result)
                    result = PlaneIntersectionType.Intersecting;

            return result;
        }

        /// <summary>
        /// Returns a <see cref="String"/> representation of this <see cref="BoundingFrustum"/> in the format:
        /// {Near:[nearPlane] Far:[farPlane] Left:[leftPlane] Right:[rightPlane] Top:[topPlane] Bottom:[bottomPlane]}
        /// </summary>
        /// <returns><see cref="String"/> representation of this <see cref="BoundingFrustum"/>.</returns>
        public override string ToString()
        {
            return "{Near: " + this._planes[0] +
                   " Far:" + this._planes[1] +
                   " Left:" + this._planes[2] +
                   " Right:" + this._planes[3] +
                   " Top:" + this._planes[4] +
                   " Bottom:" + this._planes[5] +
                   "}";
        }

        #endregion

        #region Private Methods

        private void CreateCorners()
        {
            _corners[0] = IntersectionPoint(_planes[0], _planes[2], _planes[4]);
            _corners[1] = IntersectionPoint(_planes[0], _planes[3], _planes[4]);
            _corners[2] = IntersectionPoint(_planes[0], _planes[3], _planes[5]);
            _corners[3] = IntersectionPoint(_planes[0], _planes[2], _planes[5]);
            _corners[4] = IntersectionPoint(_planes[1], _planes[2], _planes[4]);
            _corners[5] = IntersectionPoint(_planes[1], _planes[3], _planes[4]);
            _corners[6] = IntersectionPoint(_planes[1], _planes[3], _planes[5]);
            _corners[7] = IntersectionPoint(_planes[1], _planes[2], _planes[5]);
        }

        private void CreatePlanes()
        {
            _planes[0] = new Plane(-this._matrix.M13, -this._matrix.M23, -this._matrix.M33, -this._matrix.M43);
            _planes[1] = new Plane(this._matrix.M13 - this._matrix.M14, this._matrix.M23 - this._matrix.M24, this._matrix.M33 - this._matrix.M34, this._matrix.M43 - this._matrix.M44);
            _planes[2] = new Plane(-this._matrix.M14 - this._matrix.M11, -this._matrix.M24 - this._matrix.M21, -this._matrix.M34 - this._matrix.M31, -this._matrix.M44 - this._matrix.M41);
            _planes[3] = new Plane(this._matrix.M11 - this._matrix.M14, this._matrix.M21 - this._matrix.M24, this._matrix.M31 - this._matrix.M34, this._matrix.M41 - this._matrix.M44);
            _planes[4] = new Plane(this._matrix.M12 - this._matrix.M14, this._matrix.M22 - this._matrix.M24, this._matrix.M32 - this._matrix.M34, this._matrix.M42 - this._matrix.M44);
            _planes[5] = new Plane(-this._matrix.M14 - this._matrix.M12, -this._matrix.M24 - this._matrix.M22, -this._matrix.M34 - this._matrix.M32, -this._matrix.M44 - this._matrix.M42);

            _planes[0].Normalize();
            _planes[1].Normalize();
            _planes[2].Normalize();
            _planes[3].Normalize();
            _planes[4].Normalize();
            _planes[5].Normalize();
        }

        private static Vector3 IntersectionPoint(Plane a, Plane b, Plane c)
        {
            // Formula used
            //                d1 ( N2 * N3 ) + d2 ( N3 * N1 ) + d3 ( N1 * N2 )
            //P =   -------------------------------------------------------------------------
            //                             N1 . ( N2 * N3 )
            //
            // Note: N refers to the normal, d refers to the displacement. '.' means dot product. '*' means cross product

            Vector3 v1, v2, v3;
            Vector3 cross = Vector3.Cross(b.Normal, c.Normal);

            float f = Vector3.Dot(a.Normal, cross);
            f *= -1.0f;

            cross = Vector3.Cross(b.Normal, c.Normal);
            v1 = Vector3.Multiply(cross, a.D);
            //v1 = (a.D * (Vector3.Cross(b.Normal, c.Normal)));


            cross = Vector3.Cross(c.Normal, a.Normal);
            v2 = Vector3.Multiply(cross, b.D);
            //v2 = (b.D * (Vector3.Cross(c.Normal, a.Normal)));


            cross = Vector3.Cross(a.Normal, b.Normal);
            v3 = Vector3.Multiply(cross, c.D);
            //v3 = (c.D * (Vector3.Cross(a.Normal, b.Normal)));

            return new Vector3(
                (v1.X + v2.X + v3.X) / f,
                (v1.Y + v2.Y + v3.Y) / f,
                (v1.Z + v2.Z + v3.Z) / f
            );
        }

        #endregion
    }
}
