using Box3D;
using System.Numerics;

namespace Phoenix.Framework.Collisions;

/// <summary>
/// Represents a 3D capsule bounding volume with Box3D interop.
/// </summary>
public sealed class Capsule : IEquatable<Capsule>
{
    public Vector3 PointA;
    public Vector3 PointB;
    public float Radius;

    public Capsule(Vector3 pointA, Vector3 pointB, float radius)
    {
        PointA = pointA;
        PointB = pointB;
        Radius = radius;
    }

    public static implicit operator Box3D.Capsule(Capsule c) => new(c.PointA, c.PointB, c.Radius);
    public static implicit operator Capsule(Box3D.Capsule c) => new(c.Start, c.End, c.Radius);

    public bool Equals(Capsule? other)
    {
        return other is not null && PointA == other.PointA && PointB == other.PointB && Radius == other.Radius;
    }

    public override bool Equals(object? obj) => obj is Capsule other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(PointA, PointB, Radius);
    public static bool operator ==(Capsule? a, Capsule? b) => Equals(a, b);
    public static bool operator !=(Capsule? a, Capsule? b) => !Equals(a, b);
}
