# Ray Casts

Namespace: `Phoenix.Framework.Collisions`

The static `RayCasts` class performs ray vs volume tests that return a full hit result: hit **fraction**, **point**, and **normal**. Unlike `Ray.Intersects()` (which returns a distance), these are designed for gameplay use — bullet impacts, ground snapping, and AI line-of-sight.

## API

```csharp
public static bool RayVsAABB(Ray ray, Vector3 aabbMin, Vector3 aabbMax,
    out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal,
    float maxDist = float.MaxValue)

public static bool RayVsOBB(Ray ray, Vector3 obbPos, Vector3 obbSize, Matrix4x4 orientation,
    out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal,
    float maxDist = float.MaxValue)

public static bool RayVsSphere(Ray ray, Vector3 center, float radius,
    out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)

public static bool RayVsCapsule(Ray ray, Vector3 pointA, Vector3 pointB, float radius,
    out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal,
    float maxDist = float.MaxValue)

public static bool RayVsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2,
    out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)

public static bool RayVsCylinder(Ray ray, Vector3 center, float radius, float halfHeight,
    out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)
```

## Outputs

- `hitFraction` — normalized distance along the ray (0 = origin, 1 = ray direction length); multiply by `ray.Direction` to get the world-space offset
- `hitPoint` — exact world-space intersection point
- `hitNormal` — surface normal at the hit point (for bounce/reflection or decals)

`maxDist` (where available) limits the search distance; hits beyond it are ignored.

## Example

```csharp
var ray = new Ray(origin, direction);

if (RayCasts.RayVsAABB(ray, boxMin, boxMax,
        out var fraction, out var point, out var normal))
{
    // Impact position
    var impact = ray.Position + ray.Direction * fraction;

    // Reflect the ray off the surface
    var reflected = Vector3.Reflect(ray.Direction, normal);
}
```

## See Also

- [Ray Casting](ray.md) — the `Ray` class and its built-in `Intersects()` tests
- [Collision Manifolds](manifold.md) — volume-vs-volume contact resolution
- [Serializable Volumes](serializable.md) — `Collision.RayVsVolume` for ray vs any volume type
