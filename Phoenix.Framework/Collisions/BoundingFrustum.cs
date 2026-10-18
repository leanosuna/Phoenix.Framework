using Box3D;
using System.Numerics;

namespace Phoenix.Framework.Collisions;

/// <summary>
/// Defines a viewing frustum for camera visibility culling and spatial queries.
/// </summary>
public sealed class BoundingFrustum : IEquatable<BoundingFrustum>
{
    public const int PlaneCount = 6;
    public const int CornerCount = 8;

    private readonly Vector3[] _corners = new Vector3[CornerCount];
    private readonly System.Numerics.Plane[] _planes = new System.Numerics.Plane[PlaneCount];
    private Matrix4x4 _matrix;

    /// <summary>
    /// Gets or sets the combined view-projection matrix defining the frustum.
    /// </summary>
    public Matrix4x4 Matrix
    {
        get => _matrix;
        set
        {
            _matrix = value;
            CreatePlanes();
            CreateCorners();
        }
    }

    public System.Numerics.Plane Near => _planes[0];
    public System.Numerics.Plane Far => _planes[1];
    public System.Numerics.Plane Left => _planes[2];
    public System.Numerics.Plane Right => _planes[3];
    public System.Numerics.Plane Top => _planes[4];
    public System.Numerics.Plane Bottom => _planes[5];
    public System.Numerics.Plane[] Planes => _planes;

    public BoundingFrustum(Matrix4x4 value)
    {
        Matrix = value;
    }

    public void Update(Matrix4x4 viewProjection)
    {
        Matrix = viewProjection;
    }

    public Vector3[] GetCorners()
    {
        return (Vector3[])_corners.Clone();
    }

    public void GetCorners(Vector3[] corners)
    {
        if (corners == null) throw new ArgumentNullException(nameof(corners));
        if (corners.Length < CornerCount) throw new ArgumentOutOfRangeException(nameof(corners));
        _corners.CopyTo(corners, 0);
    }

    public ContainmentType Contains(Vector3 point)
    {
        for (int i = 0; i < PlaneCount; i++)
        {
            if (Vector3.Dot(_planes[i].Normal, point) + _planes[i].D > 0f)
                return ContainmentType.Disjoint;
        }
        return ContainmentType.Contains;
    }

    public ContainmentType Contains(Box3D.BoundingBox box)
    {
        bool intersects = false;
        for (int i = 0; i < PlaneCount; i++)
        {
            var plane = _planes[i];
            Vector3 min = box.Min;
            Vector3 max = box.Max;

            Vector3 p = new(
                plane.Normal.X >= 0 ? max.X : min.X,
                plane.Normal.Y >= 0 ? max.Y : min.Y,
                plane.Normal.Z >= 0 ? max.Z : min.Z
            );
            Vector3 n = new(
                plane.Normal.X >= 0 ? min.X : max.X,
                plane.Normal.Y >= 0 ? min.Y : max.Y,
                plane.Normal.Z >= 0 ? min.Z : max.Z
            );

            if (Vector3.Dot(plane.Normal, p) + plane.D < 0f)
                return ContainmentType.Disjoint;

            if (Vector3.Dot(plane.Normal, n) + plane.D > 0f)
                intersects = true;
        }
        return intersects ? ContainmentType.Intersects : ContainmentType.Contains;
    }

    public ContainmentType Contains(AxisAlignedBoundingBox box)
    {
        return Contains((Box3D.BoundingBox)box);
    }

    public ContainmentType Contains(Box3D.Sphere sphere, Vector3 position)
    {
        bool intersects = false;
        for (int i = 0; i < PlaneCount; i++)
        {
            float dist = Vector3.Dot(_planes[i].Normal, position) + _planes[i].D;
            if (dist < -sphere.Radius)
                return ContainmentType.Disjoint;
            if (dist < sphere.Radius)
                intersects = true;
        }
        return intersects ? ContainmentType.Intersects : ContainmentType.Contains;
    }

    public ContainmentType Contains(BoundingSphere sphere)
    {
        return Contains(new Box3D.Sphere(sphere.Radius), sphere.Position);
    }

    public ContainmentType Contains(BoundingFrustum frustum)
    {
        if (this == frustum)
            return ContainmentType.Contains;

        bool intersects = false;
        for (int i = 0; i < PlaneCount; i++)
        {
            var planeIntersection = frustum.Intersects(_planes[i]);
            switch (planeIntersection)
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

    public bool Intersects(AxisAlignedBoundingBox box) => Contains(box) != ContainmentType.Disjoint;
    public bool Intersects(Box3D.BoundingBox box) => Contains(box) != ContainmentType.Disjoint;
    public bool Intersects(BoundingSphere sphere) => Contains(sphere) != ContainmentType.Disjoint;
    public bool Intersects(Box3D.Sphere sphere, Vector3 position) => Contains(sphere, position) != ContainmentType.Disjoint;
    public bool Intersects(BoundingFrustum frustum) => Contains(frustum) != ContainmentType.Disjoint;

    public void Intersects(ref BoundingSphere sphere, out bool result)
    {
        result = Intersects(sphere);
    }

    public PlaneIntersectionType Intersects(System.Numerics.Plane plane)
    {
        float d0 = Vector3.Dot(plane.Normal, _corners[0]) + plane.D;
        PlaneIntersectionType result = d0 > 0 ? PlaneIntersectionType.Front : d0 < 0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;

        for (int i = 1; i < _corners.Length; i++)
        {
            float di = Vector3.Dot(plane.Normal, _corners[i]) + plane.D;
            PlaneIntersectionType current = di > 0 ? PlaneIntersectionType.Front : di < 0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
            if (current != result)
                return PlaneIntersectionType.Intersecting;
        }

        return result;
    }

    public float? Intersects(Ray ray)
    {
        float tmin = 0f;
        float tmax = float.MaxValue;

        for (int i = 0; i < PlaneCount; i++)
        {
            var plane = _planes[i];
            float dotNormal = Vector3.Dot(plane.Normal, ray.Direction);
            float distance = Vector3.Dot(plane.Normal, ray.Position) + plane.D;

            if (MathF.Abs(dotNormal) < 1e-6f)
            {
                if (distance > 0f)
                    return null;
                continue;
            }

            float t = -distance / dotNormal;
            if (dotNormal > 0f)
                tmax = MathF.Min(tmax, t);
            else
                tmin = MathF.Max(tmin, t);

            if (tmin > tmax)
                return null;
        }

        if (tmax < 0f)
            return null;

        return tmin;
    }

    private void CreatePlanes()
    {
        _planes[0] = System.Numerics.Plane.Normalize(new System.Numerics.Plane(-_matrix.M13, -_matrix.M23, -_matrix.M33, -_matrix.M43));
        _planes[1] = System.Numerics.Plane.Normalize(new System.Numerics.Plane(_matrix.M13 - _matrix.M14, _matrix.M23 - _matrix.M24, _matrix.M33 - _matrix.M34, _matrix.M43 - _matrix.M44));
        _planes[2] = System.Numerics.Plane.Normalize(new System.Numerics.Plane(-_matrix.M14 - _matrix.M11, -_matrix.M24 - _matrix.M21, -_matrix.M34 - _matrix.M31, -_matrix.M44 - _matrix.M41));
        _planes[3] = System.Numerics.Plane.Normalize(new System.Numerics.Plane(_matrix.M11 - _matrix.M14, _matrix.M21 - _matrix.M24, _matrix.M31 - _matrix.M34, _matrix.M41 - _matrix.M44));
        _planes[4] = System.Numerics.Plane.Normalize(new System.Numerics.Plane(_matrix.M12 - _matrix.M14, _matrix.M22 - _matrix.M24, _matrix.M32 - _matrix.M34, _matrix.M42 - _matrix.M44));
        _planes[5] = System.Numerics.Plane.Normalize(new System.Numerics.Plane(-_matrix.M14 - _matrix.M12, -_matrix.M24 - _matrix.M22, -_matrix.M34 - _matrix.M32, -_matrix.M44 - _matrix.M42));
    }

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

    private static Vector3 IntersectionPoint(System.Numerics.Plane a, System.Numerics.Plane b, System.Numerics.Plane c)
    {
        Vector3 cross = Vector3.Cross(b.Normal, c.Normal);
        float f = -Vector3.Dot(a.Normal, cross);

        Vector3 v1 = Vector3.Multiply(Vector3.Cross(b.Normal, c.Normal), a.D);
        Vector3 v2 = Vector3.Multiply(Vector3.Cross(c.Normal, a.Normal), b.D);
        Vector3 v3 = Vector3.Multiply(Vector3.Cross(a.Normal, b.Normal), c.D);

        return new Vector3(
            (v1.X + v2.X + v3.X) / f,
            (v1.Y + v2.Y + v3.Y) / f,
            (v1.Z + v2.Z + v3.Z) / f
        );
    }

    public bool Equals(BoundingFrustum? other)
    {
        return other is not null && _matrix == other._matrix;
    }

    public override bool Equals(object? obj) => obj is BoundingFrustum other && Equals(other);
    public override int GetHashCode() => _matrix.GetHashCode();
    public static bool operator ==(BoundingFrustum? a, BoundingFrustum? b) => Equals(a, b);
    public static bool operator !=(BoundingFrustum? a, BoundingFrustum? b) => !Equals(a, b);
}
