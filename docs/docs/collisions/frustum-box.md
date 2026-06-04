# Frustums & Boxes

Covers `BoundingFrustum`, `AxisAlignedBoundingBox`, `OrientedBoundingBox`, and `BoundingCylinder`.

## BoundingFrustum

Represents the 6-plane view frustum derived from the view-projection matrix.

### Creating

```csharp
// From view-projection matrix
var frustum = new BoundingFrustum(Camera.View * Camera.Projection);
```

### Updating

```csharp
// Recalculate planes and corners
frustum.Update(Camera.View * Camera.Projection);
```

### Planes

| Property | Type | Description |
|----------|------|-------------|
| `Near` | `Plane` | Near clipping plane |
| `Far` | `Plane` | Far clipping plane |
| `Left` | `Plane` | Left clipping plane |
| `Right` | `Plane` | Right clipping plane |
| `Top` | `Plane` | Top clipping plane |
| `Bottom` | `Plane` | Bottom clipping plane |
| `Matrix` | `Matrix4x4` | View-projection matrix (triggers recalc) |
| `PlaneCount` | `const int` | 6 |
| `CornerCount` | `const int` | 8 |

### Containment Tests

```csharp
// Point in frustum?
ContainmentType type = frustum.Contains(point);

// Sphere in frustum?
ContainmentType type = frustum.Contains(sphere);
// Returns: Contains (fully inside), Intersects (partially inside), Disjoint (fully outside)

// AABB in frustum?
ContainmentType type = frustum.Contains(aabb);

// Frustum vs frustum?
ContainmentType type = frustum.Contains(otherFrustum);
```

### Intersection Tests

```csharp
bool hits = frustum.Intersects(sphere);
bool hits = frustum.Intersects(aabb);
bool hits = frustum.Intersects(otherFrustum);
PlaneIntersectionType type = frustum.Intersects(plane);
float? dist = frustum.Intersects(ray);
```

### Corners

```csharp
Vector3[] corners = frustum.GetCorners();

// Or into an existing array
Vector3[] corners = new Vector3[8];
frustum.GetCorners(corners);
```

## AxisAlignedBoundingBox (AABB)

An axis-aligned box defined by min and max corners.

### Creating

```csharp
var aabb = new AxisAlignedBoundingBox(
    new Vector3(-1, -1, -1),  // Min corner
    new Vector3(1, 1, 1)       // Max corner
);

// From points
var aabb = AxisAlignedBoundingBox.CreateFromPoints(points);

// From sphere
var aabb = AxisAlignedBoundingBox.CreateFromSphere(sphere);
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Min` | `Vector3` | Minimum corner |
| `Max` | `Vector3` | Maximum corner |
| `Size` | `Vector3` | Max - Min |
| `Position` | `Vector3` | Center (Min + Max) / 2 |
| `CornerCount` | `const int` | 8 |

### Corner Access

```csharp
Vector3[] corners = aabb.GetCorners();
```

### Containment

```csharp
ContainmentType type = aabb.Contains(point);
ContainmentType type = aabb.Contains(sphere);
ContainmentType type = aabb.Contains(otherAabb);
ContainmentType type = aabb.Contains(frustum);              // Imprecise
ContainmentType type = aabb.ContainsPrecise(frustum);        // SAT, 23 axes
```

### Intersections

```csharp
bool hits = aabb.Intersects(sphere);
bool hits = aabb.Intersects(otherAabb);
bool hits = aabb.Intersects(frustum);
PlaneIntersectionType type = aabb.Intersects(plane);
float? dist = aabb.Intersects(ray);
```

### Transform

```csharp
var transformed = aabb.Transform(worldMatrix);
```

### Update

```csharp
aabb.Update(newPosition);  // Move the box
```

## OrientedBoundingBox (OBB)

An axis-aligned box that can be rotated (oriented by a matrix).

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Position` | `Vector3` | Center |
| `Size` | `Vector3` | Dimensions |
| `Orientation` | `Matrix4x4` | Rotation matrix |
| `World` | `Matrix4x4` | Full world transform (computed) |

### Creating

```csharp
var obb = new OrientedBoundingBox
{
    Position = new Vector3(0, 1, 0),
    Size = new Vector3(2, 2, 2),
    Orientation = Matrix4x4.Identity
};

// From AABB
var obb = OrientedBoundingBox.FromAABB(aabb);
```

### Coordinate Transforms

```csharp
// World → OBB local space
Vector3 localPoint = obb.ToOBBSpace(worldPoint);

// OBB local space → World
Vector3 worldPoint = obb.ToWorldSpace(localPoint);
```

### Intersections

```csharp
bool hits = obb.Intersects(otherOBB);           // SAT with 15 axes
bool hits = obb.Intersects(aabb);               // AABB-OBB
bool hits = obb.Intersects(sphere);
bool hits = obb.Intersects(ray, out float? dist);  // Ray with distance
bool hits = obb.Intersects(frustum);
PlaneIntersectionType type = obb.Intersects(plane);
```

## BoundingCylinder

A cylinder with radius, height, and optional rotation.

### Creating

```csharp
var cylinder = new BoundingCylinder
{
    Position = new Vector3(0, 1, 0),
    Radius = 1.0f,
    HalfHeight = 2.0f,
    Rotation = Matrix4x4.Identity
};
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Position` | `Vector3` | Cylinder center/base |
| `Radius` | `float` | Cylinder radius |
| `HalfHeight` | `float` | Half height (total = 2 × HalfHeight) |
| `Rotation` | `Matrix4x4` | Rotation matrix |
| `Transform` | `Matrix4x4` | Full transform |
| `IsXZAligned` | `bool` | True if aligned to XZ plane (Y-up) |

### Intersections

```csharp
// Ray intersection
bool hits = cylinder.Intersects(ray);

// Sphere intersection
bool hits = cylinder.Intersects(sphere);

// Point containment
ContainmentType type = cylinder.Contains(point);

// Line segment intersection
Vector3? hit = cylinder.Intersects(pointA, pointB);

// AABB intersection
BoxCylinderIntersection type = cylinder.Intersects(aabb);
// Returns: Edge (touching), Intersecting, None
```

## Example: Terrain Collision with Cylinder

```csharp
// Player collision: test ground cylinder against terrain
var playerCylinder = new BoundingCylinder
{
    Position = playerPosition,
    Radius = 0.5f,
    HalfHeight = 1.8f,
    IsXZAligned = true
};

// Check if cylinder intersects any terrain triangle
foreach (var terrainTriangle in terrainTriangles)
{
    if (playerCylinder.Intersects(terrainTriangle.v[0], terrainTriangle.v[1]) != null)
    {
        // Player is touching terrain at this triangle
        playerPosition.Y = terrainHeight;
        break;
    }
}
```

## See Also

- [Gizmos](../rendering/gizmos.md) — `AddVolume()` for all these types
- [Camera](../core/camera.md) — `BoundingFrustum` from view-projection
- [Bounding Spheres](spheres.md) — sphere volume operations
- [Ray Casting](ray.md) — ray intersection with all volumes
