using Box3D;
using System.Numerics;

namespace Phoenix.Framework.Collisions;

/// <summary>
/// Represents an axis-aligned bounding box (AABB) in 3D space with Box3D interop.
/// </summary>
public sealed class AxisAlignedBoundingBox : IEquatable<AxisAlignedBoundingBox>
{
    public const int CornerCount = 8;

    public Vector3 Min;
    public Vector3 Max;
    public Vector3 Size;
    public Vector3 Position;

    public Vector3 Center
    {
        get => Position;
        set
        {
            Position = value;
            Vector3 half = Size * 0.5f;
            Min = Position - half;
            Max = Position + half;
        }
    }

    public AxisAlignedBoundingBox(Vector3 center, Vector3 size)
    {
        Position = center;
        Size = size;
        Vector3 half = size * 0.5f;
        Min = center - half;
        Max = center + half;
    }

    public AxisAlignedBoundingBox(Vector3 min, Vector3 max, bool isMinMax = true)
    {
        Min = min;
        Max = max;
        Size = max - min;
        Position = (min + max) * 0.5f;
    }

    public static implicit operator Box3D.BoundingBox(AxisAlignedBoundingBox box) => new(box.Min, box.Max);
    public static implicit operator AxisAlignedBoundingBox(Box3D.BoundingBox box) => new(box.Center, box.Size);

    public bool Intersects(AxisAlignedBoundingBox other)
    {
        return Min.X <= other.Max.X && Max.X >= other.Min.X &&
               Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
               Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
    }

    public bool Intersects(Box3D.BoundingBox other)
    {
        return ((Box3D.BoundingBox)this).Overlaps(other);
    }

    public bool Intersects(BoundingSphere sphere)
    {
        Vector3 closest = Vector3.Clamp(sphere.Position, Min, Max);
        return Vector3.DistanceSquared(sphere.Position, closest) <= sphere.Radius * sphere.Radius;
    }

    public PlaneIntersectionType Intersects(in System.Numerics.Plane plane)
    {
        Vector3 p = new(
            plane.Normal.X >= 0 ? Max.X : Min.X,
            plane.Normal.Y >= 0 ? Max.Y : Min.Y,
            plane.Normal.Z >= 0 ? Max.Z : Min.Z
        );
        Vector3 n = new(
            plane.Normal.X >= 0 ? Min.X : Max.X,
            plane.Normal.Y >= 0 ? Min.Y : Max.Y,
            plane.Normal.Z >= 0 ? Min.Z : Max.Z
        );

        if (Vector3.Dot(plane.Normal, p) + plane.D < 0f)
            return PlaneIntersectionType.Back;

        if (Vector3.Dot(plane.Normal, n) + plane.D > 0f)
            return PlaneIntersectionType.Front;

        return PlaneIntersectionType.Intersecting;
    }

    public bool Contains(Vector3 point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y &&
               point.Z >= Min.Z && point.Z <= Max.Z;
    }

    public bool Contains(AxisAlignedBoundingBox other)
    {
        return other.Min.X >= Min.X && other.Max.X <= Max.X &&
               other.Min.Y >= Min.Y && other.Max.Y <= Max.Y &&
               other.Min.Z >= Min.Z && other.Max.Z <= Max.Z;
    }

    public Vector3[] GetCorners()
    {
        return
        [
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

    public void GetCorners(Vector3[] corners)
    {
        if (corners == null) throw new ArgumentNullException(nameof(corners));
        if (corners.Length < CornerCount) throw new ArgumentOutOfRangeException(nameof(corners));

        corners[0] = new Vector3(Min.X, Max.Y, Max.Z);
        corners[1] = new Vector3(Max.X, Max.Y, Max.Z);
        corners[2] = new Vector3(Max.X, Min.Y, Max.Z);
        corners[3] = new Vector3(Min.X, Min.Y, Max.Z);
        corners[4] = new Vector3(Min.X, Max.Y, Min.Z);
        corners[5] = new Vector3(Max.X, Max.Y, Min.Z);
        corners[6] = new Vector3(Max.X, Min.Y, Min.Z);
        corners[7] = new Vector3(Min.X, Min.Y, Min.Z);
    }

    public bool Equals(AxisAlignedBoundingBox? other)
    {
        return other is not null && Min == other.Min && Max == other.Max;
    }

    public override bool Equals(object? obj) => obj is AxisAlignedBoundingBox other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Min, Max);
    public static bool operator ==(AxisAlignedBoundingBox? a, AxisAlignedBoundingBox? b) => Equals(a, b);
    public static bool operator !=(AxisAlignedBoundingBox? a, AxisAlignedBoundingBox? b) => !Equals(a, b);
}
