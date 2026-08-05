# Ray Casting

The `Ray` class represents a ray with an origin and direction, used for line-of-sight tests, projectile trajectories, and mouse picking.

## Creating a Ray

```csharp
Ray ray = new Ray(
    new Vector3(0, 5, -10),   // Origin (camera position)
    new Vector3(0, -0.1f, 1)  // Direction (normalized)
);
```

## Intersecting with Primitives

### Ray vs Triangle (Möller–Trumbore)

Returns the hit point if the ray intersects the triangle, or `null`:

```csharp
Vector3? hit = ray.Intersects(v0, v1, v2);
if (hit.HasValue)
{
    Vector3 intersectionPoint = hit.Value;
    // The ray hit the triangle at intersectionPoint
}
```

### Ray vs BoundingSphere

Returns distance to intersection (closest hit), or `null`:

```csharp
BoundingSphere sphere = new BoundingSphere(center, radius);
float? distance = ray.Intersects(sphere);
if (distance.HasValue)
{
    // Hit at distance units from ray origin
    Vector3 hitPoint = ray.Position + ray.Direction * distance.Value;
}
```

### Ray vs AxisAlignedBoundingBox

```csharp
AxisAlignedBoundingBox box = new AxisAlignedBoundingBox(center, size);
float? distance = ray.Intersects(box);
if (distance.HasValue)
{
    // Box hit at distance
}
```

### Ray vs Plane

```csharp
// Create a plane from a point and normal
Plane plane = new Plane(normal, D);  // or from 3 points
float? distance = ray.Intersects(plane);
if (distance.HasValue)
{
    Vector3 hitPoint = ray.Position + ray.Direction * distance.Value;
}
```

### Ray vs Line Segment

The `Ray` class itself has no line-segment overload. Use `BoundingCylinder.Intersects(pointA, pointB)` for a line segment test, or the richer [`RayCasts`](raycasts.md) API for point/normal results.

## Mouse Picking Example

Convert a screen coordinate to a world-space ray:

```csharp
protected override void Update(double dt)
{
    if (Input.MouseLeftDownOnce())
    {
        // Absolute cursor position (screen space)
        Vector2 mousePos = Input.GetContext().Mice[0].Position;

        // Convert to normalized device coordinates (-1 to +1)
        float ndcX = (mousePos.X / WindowWidth) * 2f - 1f;
        float ndcY = 1f - (mousePos.Y / WindowHeight) * 2f;

        // Unproject using inverse view-projection
        Matrix4x4 invVP = Matrix4x4.Invert(Camera.View * Camera.Projection);
        Vector4 nearClip = new Vector4(ndcX, ndcY, 0f, 1f);
        Vector4 farClip = new Vector4(ndcX, ndcY, 1f, 1f);

        Vector3 nearWorld = Unproject(nearClip, invVP);
        Vector3 farWorld = Unproject(farClip, invVP);

        Ray ray = new Ray(nearWorld, Vector3.Normalize(farWorld - nearWorld));

        // Test against scene objects
        float? hitDist = ray.Intersects(enemyCollisionSphere);
        if (hitDist.HasValue)
        {
            // Enemy was clicked!
            enemy.TakeDamage(25);
        }
    }
}

static Vector3 Unproject(Vector4 clip, Matrix4x4 invVP)
{
    Vector4 world = Vector4.Transform(clip, invVP);
    return new Vector3(world.X, world.Y, world.Z) / world.W;
}
```

## Ray Properties

| Property | Type | Description |
|----------|------|-------------|
| `Position` | `Vector3` | Ray origin |
| `Direction` | `Vector3` | Ray direction (should be normalized) |

## Notes

- The ray direction does not need to be normalized for intersection tests (the algorithm normalizes internally).
- Distance is measured along the ray direction from the origin.
- If the ray origin is inside a sphere or box, distance is `0`.
- Triangle intersection uses the Möller–Trumbore algorithm (barycentric coordinates).

## See Also

- [Bounding Spheres](spheres.md) — `BoundingSphere.Intersects(Ray)`
- [Frustums & Boxes](frustum-box.md) — other volume ray tests
- [Ray Casts](raycasts.md) — `RayVs*` with hit point, normal, and fraction
- [Gizmos](../rendering/gizmos.md) — visualize rays with `AddLine`
