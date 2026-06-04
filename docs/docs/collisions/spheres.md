# Bounding Spheres

A `BoundingSphere` is defined by a center point and a radius. It provides fast containment and intersection tests.

## Creating a Sphere

```csharp
// From center and radius
var sphere = new BoundingSphere(new Vector3(0, 1, 0), 2.5f);
```

### From Other Volumes

```csharp
// From a set of points
var sphere = BoundingSphere.CreateFromPoints(vertexPositions);

// From an AABB
var sphere = BoundingSphere.CreateFromBoundingBox(aabb);

// From a frustum
var sphere = BoundingSphere.CreateFromFrustum(frustum);

// Merge two spheres (tightest bound containing both)
var merged = BoundingSphere.CreateMerged(sphereA, sphereB);
```

## Transforming

```csharp
// Transform by matrix (e.g., world transform)
var transformed = sphere.Transform(worldMatrix);
```

## Updating

```csharp
// Move the sphere center
sphere.Update(newPosition);
```

## Intersection Tests

### Sphere vs Sphere

```csharp
bool hits = sphere.Intersects(otherSphere);
ContainmentType type = sphere.Contains(otherSphere);  // Contains / Intersects / Disjoint
```

### Sphere vs AABB

```csharp
bool hits = sphere.Intersects(aabb);
ContainmentType type = sphere.Contains(aabb);
```

### Sphere vs Frustum

```csharp
ContainmentType type = sphere.Contains(frustum);
```

### Sphere vs Plane

```csharp
PlaneIntersectionType type = sphere.Intersects(plane);
// Returns: Front (center is in front), Back (center is behind), Intersecting (crosses)
```

### Sphere vs Ray

```csharp
Ray ray = new Ray(origin, direction);
float? distance = sphere.Intersects(ray);
if (distance.HasValue)
{
    // Ray hits the sphere at distance units
}
```

### Sphere vs Point

```csharp
ContainmentType type = sphere.Contains(point);
// Returns: Contains (point inside), Disjoint (point outside)
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Center` | `Vector3` | Sphere center |
| `Radius` | `float` | Sphere radius |

## Use Cases

- **Enemy hitboxes** — fast overlap checks
- **View culling** — `frustum.Contains(sphere)` to skip off-screen objects
- **Spatial partitioning** — sphere bounds for quadtree/octree nodes
- **Proximity checks** — `sphere.Intersects(otherSphere)` for trigger zones

## Example: View Culling

```csharp
foreach (var enemy in enemies)
{
    // Skip enemies outside the camera frustum
    if (frustum.Contains(enemy.CollisionSphere) == ContainmentType.Disjoint)
        continue;

    // Enemy is visible — render it
    RenderEnemy(enemy);
}
```

## See Also

- [Ray Casting](ray.md) — `BoundingSphere.Intersects(Ray)`
- [Frustums & Boxes](frustum-box.md) — frustum contains sphere test
- [Gizmos](../rendering/gizmos.md) — `AddVolume(sphere, color)`
