# Collision System — Overview

Phoenix provides a comprehensive set of 3D collision volumes and intersection test methods. All volumes are pure math — no physics engine is included.

## Volumes

| Volume | File | Description |
|--------|------|-------------|
| `Ray` | `Ray.cs` | Origin + direction, for ray casting |
| `BoundingSphere` | `BoundingSphere.cs` | Center + radius |
| `AxisAlignedBoundingBox` | `AxisAlignedBoundingBox.cs` | Min/Max bounds, axis-aligned |
| `OrientedBoundingBox` | `OrientedBoundingBox.cs` | Position + size + orientation matrix |
| `BoundingFrustum` | `BoundingFrustum.cs` | 6-plane view frustum |
| `BoundingCylinder` | `BoundingCylinder.cs` | Position + radius + height + rotation |
| `Capsule` | `Capsule.cs` | Segment (A–B) + radius |

## Intersection Test Types

| Type | Return | Description |
|------|--------|-------------|
| `bool Intersects(...)` | `bool` | Do the volumes intersect? |
| `ContainmentType Contains(...)` | `Contains`/`Intersects`/`Disjoint` | Full containment check |
| `float? Intersects(Ray)` | `float?` | Distance to hit, or `null` |
| `PlaneIntersectionType Intersects(Plane)` | `Front`/`Back`/`Intersecting` | Which side of a plane |

## Quick Reference

```csharp
// Ray casting
Ray ray = new Ray(position, direction);
float? hitDist = ray.Intersects(sphere);
float? hitDist = ray.Intersects(aabb);
float? hitDist = ray.Intersects(plane);
Vector3? triHit = ray.Intersects(v0, v1, v2);  // Triangle (Möller-Trumbore)

// Sphere tests
bool hits = sphere.Intersects(aabb);
bool hits = sphere.Intersects(anotherSphere);
ContainmentType type = sphere.Contains(point);

// AABB tests
bool hits = aabb.Intersects(sphere);
bool hits = aabb.Intersects(frustum);
ContainmentType type = aabb.Contains(sphere);
Vector3[] corners = aabb.GetCorners();

// Frustum tests
bool inFrustum = frustum.Contains(sphere);
bool inFrustum = frustum.Contains(aabb);
Vector3[] corners = frustum.GetCorners();

// OBB tests
bool hits = obb.Intersects(aabb);
bool hits = obb.Intersects(obb);
bool hits = obb.Intersects(ray, out float? dist);

// Cylinder tests
bool hits = cylinder.Intersects(ray);
bool hits = cylinder.Intersects(sphere);
ContainmentType type = cylinder.Contains(point);

// Capsule tests
bool hits = capsule.Intersects(ray);
```

> Note: `BoundingFrustum.Contains(Vector3 point)` is currently unimplemented (always returns `Disjoint`). Use the sphere/AABB overloads, or `ContainsPrecise` on AABBs.

## Manifold API & Ray Casting

For position/depth resolution between two volumes, see [Collision Manifolds](manifold.md). For rich ray-cast results (hit point + normal + fraction), see [Ray Casts](raycasts.md).

## Drawing Volumes with Gizmos

All collision volumes can be visualized using [`Gizmos.AddVolume<T>()`](../rendering/gizmos.md):

```csharp
Gizmos.AddVolume(sphere, new Vector3(1, 0, 0));
Gizmos.AddVolume(aabb, new Vector3(0, 1, 0));
Gizmos.AddVolume(obb, new Vector3(0, 0, 1));
Gizmos.AddVolume(frustum, new Vector3(1, 1, 0));
Gizmos.AddVolume(cylinder, new Vector3(1, 0.5f, 0.5f));
```

## Creating Volumes

### From Model Data

```csharp
// Create bounding sphere from model vertices
var sphere = BoundingSphere.CreateFromPoints(modelVertices);

// Create sphere from AABB
var sphereFromBox = BoundingSphere.CreateFromBoundingBox(aabb);

// Create sphere from frustum
var sphereFromFrustum = BoundingSphere.CreateFromFrustum(frustum);

// Create AABB from sphere
var aabbFromSphere = AxisAlignedBoundingBox.CreateFromSphere(sphere);

// Merge two spheres
var merged = BoundingSphere.CreateMerged(sphereA, sphereB);

// OBB from AABB
var obb = OrientedBoundingBox.FromAABB(aabb);
```

### From Transform

```csharp
// Transform a sphere by a matrix
var transformed = sphere.Transform(worldMatrix);

// Reposition an AABB (recompute min/max from center)
aabb.Update(newPosition);

// Transform a frustum (recompute planes)
frustum.Update(Camera.View * Camera.Projection);
```

> Note: `AxisAlignedBoundingBox` has no `Transform(matrix)` method — it is axis-aligned. Use `Update(Vector3)` to reposition it, or convert to an OBB via `OrientedBoundingBox.FromAABB` for oriented transforms.

## Coordinate Space Conversions (OBB)

```csharp
// Convert world point to OBB local space
Vector3 localPoint = obb.ToOBBSpace(worldPoint);

// Convert local point to world space
Vector3 worldPoint = obb.ToWorldSpace(localPoint);
```

## Plane Helpers

The `PlaneHelper` utility provides plane transformations and distance calculations (extension methods on `Plane`):

```csharp
// Classify a point relative to a plane
float type = plane.ClassifyPoint(point);

// Get perpendicular distance from point to plane
float distance = plane.PerpendicularDistance(point);

// Transform a plane by a matrix
Plane transformed = plane.Transform(matrix);

// Transform a plane by a quaternion
Plane transformed = plane.Transform(rotation);
```

## See Also

- [Ray Casting](ray.md) — detailed ray intersection guide
- [Bounding Spheres](spheres.md) — sphere operations
- [Frustums & Boxes](frustum-box.md) — frustum, AABB, OBB, cylinder
- [Collision Manifolds](manifold.md) — push-out vectors and depth between volumes
- [Ray Casts](raycasts.md) — ray vs volume with hit point/normal/fraction
- [Serializable Volumes](serializable.md) — editor-serializable collision volumes
- [Gizmos](../rendering/gizmos.md) — visualize collision volumes
