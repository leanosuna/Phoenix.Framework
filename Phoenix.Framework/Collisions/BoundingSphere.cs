using Box3D;
using System.Numerics;

namespace Phoenix.Framework.Collisions;

/// <summary>
/// Represents a bounding sphere in 3D space with Box3D interop.
/// </summary>
public sealed class BoundingSphere : IEquatable<BoundingSphere>
{
    public Vector3 Position;
    public float Radius;

    public Vector3 Center
    {
        get => Position;
        set => Position = value;
    }

    public BoundingSphere(Vector3 position, float radius)
    {
        Position = position;
        Radius = radius;
    }

    public static implicit operator Box3D.Sphere(BoundingSphere s) => new(s.Radius);

    public bool Intersects(BoundingSphere other)
    {
        float r = Radius + other.Radius;
        return Vector3.DistanceSquared(Position, other.Position) <= r * r;
    }

    public bool Intersects(AxisAlignedBoundingBox box)
    {
        return box.Intersects(this);
    }

    public bool Intersects(Box3D.BoundingBox box)
    {
        Vector3 closest = Vector3.Clamp(Position, box.Min, box.Max);
        return Vector3.DistanceSquared(Position, closest) <= Radius * Radius;
    }

    public PlaneIntersectionType Intersects(in System.Numerics.Plane plane)
    {
        float dist = Vector3.Dot(plane.Normal, Position) + plane.D;
        if (dist > Radius)
            return PlaneIntersectionType.Front;
        if (dist < -Radius)
            return PlaneIntersectionType.Back;
        return PlaneIntersectionType.Intersecting;
    }

    public bool Contains(Vector3 point)
    {
        return Vector3.DistanceSquared(Position, point) <= Radius * Radius;
    }

    public bool Equals(BoundingSphere? other)
    {
        return other is not null && Position == other.Position && Radius == other.Radius;
    }

    public override bool Equals(object? obj) => obj is BoundingSphere other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Position, Radius);
    public static bool operator ==(BoundingSphere? a, BoundingSphere? b) => Equals(a, b);
    public static bool operator !=(BoundingSphere? a, BoundingSphere? b) => !Equals(a, b);
}
