using System.Numerics;

namespace Phoenix.Framework.Collisions;

/// <summary>
/// Provides ray casting utility methods against bounding shapes and primitives.
/// </summary>
public static class RayCasts
{
    public static bool RayVsAABB(Ray ray, Vector3 aabbMin, Vector3 aabbMax, out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal, float maxDist = float.MaxValue)
    {
        return Ray.RayVsAABB(ray, aabbMin, aabbMax, out hitFraction, out hitPoint, out hitNormal, maxDist);
    }

    public static bool RayVsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)
    {
        return Ray.RayVsTriangle(ray, v0, v1, v2, out hitFraction, out hitPoint, out hitNormal);
    }
}
