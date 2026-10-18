using Box3D;
using System.Numerics;

namespace Phoenix.Framework.Collisions;

/// <summary>
/// Represents a 3D ray with origin and direction, providing intersection tests against Box3D and bounding volumes.
/// </summary>
public sealed class Ray : IEquatable<Ray>
{
    public Vector3 Position;
    public Vector3 Direction;

    public Ray(Vector3 position, Vector3 direction)
    {
        Position = position;
        Direction = direction != Vector3.Zero ? Vector3.Normalize(direction) : Vector3.Zero;
    }

    public void Update(Vector3 position, Vector3? direction = null)
    {
        Position = position;
        if (direction.HasValue)
            Direction = direction.Value != Vector3.Zero ? Vector3.Normalize(direction.Value) : Vector3.Zero;
    }

    public float? Intersects(Vector3 min, Vector3 max)
    {
        float tMin = 0.0f;
        float tMax = float.MaxValue;

        // X
        if (MathF.Abs(Direction.X) < 1e-8f)
        {
            if (Position.X < min.X || Position.X > max.X) return null;
        }
        else
        {
            float invD = 1.0f / Direction.X;
            float t1 = (min.X - Position.X) * invD;
            float t2 = (max.X - Position.X) * invD;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax) return null;
        }

        // Y
        if (MathF.Abs(Direction.Y) < 1e-8f)
        {
            if (Position.Y < min.Y || Position.Y > max.Y) return null;
        }
        else
        {
            float invD = 1.0f / Direction.Y;
            float t1 = (min.Y - Position.Y) * invD;
            float t2 = (max.Y - Position.Y) * invD;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax) return null;
        }

        // Z
        if (MathF.Abs(Direction.Z) < 1e-8f)
        {
            if (Position.Z < min.Z || Position.Z > max.Z) return null;
        }
        else
        {
            float invD = 1.0f / Direction.Z;
            float t1 = (min.Z - Position.Z) * invD;
            float t2 = (max.Z - Position.Z) * invD;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax) return null;
        }

        return tMin;
    }

    public float? Intersects(Box3D.BoundingBox box)
    {
        return Intersects(box.Min, box.Max);
    }

    public static bool RayVsAABB(Ray ray, Vector3 aabbMin, Vector3 aabbMax, out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal, float maxDist = float.MaxValue)
    {
        hitFraction = 0f;
        hitPoint = Vector3.Zero;
        hitNormal = Vector3.Zero;

        float tMin = 0.0f;
        float tMax = maxDist;
        Vector3 normal = Vector3.Zero;

        for (int i = 0; i < 3; i++)
        {
            float origin = i switch { 0 => ray.Position.X, 1 => ray.Position.Y, _ => ray.Position.Z };
            float dir = i switch { 0 => ray.Direction.X, 1 => ray.Direction.Y, _ => ray.Direction.Z };
            float minVal = i switch { 0 => aabbMin.X, 1 => aabbMin.Y, _ => aabbMin.Z };
            float maxVal = i switch { 0 => aabbMax.X, 1 => aabbMax.Y, _ => aabbMax.Z };

            if (MathF.Abs(dir) < 1e-8f)
            {
                if (origin < minVal || origin > maxVal)
                    return false;
            }
            else
            {
                float invD = 1.0f / dir;
                float t1 = (minVal - origin) * invD;
                float t2 = (maxVal - origin) * invD;
                Vector3 n = i switch { 0 => new Vector3(-MathF.Sign(dir), 0, 0), 1 => new Vector3(0, -MathF.Sign(dir), 0), _ => new Vector3(0, 0, -MathF.Sign(dir)) };
                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                    n = -n;
                }
                if (t1 > tMin)
                {
                    tMin = t1;
                    normal = n;
                }
                tMax = MathF.Min(tMax, t2);
                if (tMin > tMax)
                    return false;
            }
        }

        if (tMin < 0f || tMin > maxDist)
            return false;

        hitFraction = tMin;
        hitPoint = ray.Position + ray.Direction * tMin;
        hitNormal = normal;
        return true;
    }

    public static bool RayVsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)
    {
        hitFraction = 0f;
        hitPoint = Vector3.Zero;
        hitNormal = Vector3.Zero;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(ray.Direction, edge2);
        float a = Vector3.Dot(edge1, h);

        if (MathF.Abs(a) < 1e-6f)
            return false;

        float f = 1.0f / a;
        Vector3 s = ray.Position - v0;
        float u = f * Vector3.Dot(s, h);

        if (u < 0.0f || u > 1.0f)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(ray.Direction, q);

        if (v < 0.0f || u + v > 1.0f)
            return false;

        float t = f * Vector3.Dot(edge2, q);
        if (t < 0.0f)
            return false;

        hitFraction = t;
        hitPoint = ray.Position + ray.Direction * t;
        hitNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
        return true;
    }

    public float? Intersects(AxisAlignedBoundingBox box)
    {
        return Intersects((Box3D.BoundingBox)box);
    }

    public float? Intersects(BoundingSphere sphere)
    {
        Vector3 m = Position - sphere.Position;
        float b = Vector3.Dot(m, Direction);
        float c = Vector3.Dot(m, m) - sphere.Radius * sphere.Radius;

        if (c > 0f && b > 0f)
            return null;

        float discr = b * b - c;
        if (discr < 0f)
            return null;

        float t = -b - MathF.Sqrt(discr);
        return t < 0f ? 0f : t;
    }

    public float? Intersects(in System.Numerics.Plane plane)
    {
        float dot = Vector3.Dot(plane.Normal, Direction);
        if (MathF.Abs(dot) < 1e-6f)
            return null;

        float t = -(Vector3.Dot(plane.Normal, Position) + plane.D) / dot;
        return t >= 0f ? t : null;
    }

    public float? Intersects(BoundingFrustum frustum)
    {
        return frustum.Intersects(this);
    }

    public bool Equals(Ray? other)
    {
        return other is not null && Position == other.Position && Direction == other.Direction;
    }

    public override bool Equals(object? obj) => obj is Ray other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Position, Direction);
    public static bool operator ==(Ray? a, Ray? b) => Equals(a, b);
    public static bool operator !=(Ray? a, Ray? b) => !Equals(a, b);
}
