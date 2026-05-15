# Gizmos

Gizmos provide debug visualization for geometry, collisions, and spatial data. They render as colored wireframes on top of the scene.

## Access

```csharp
var gizmos = Gizmos;  // Read-only from PhoenixGame
```

## Enabling/Disabling

```csharp
Gizmos.Enabled = true;   // Default: true
Gizmos.Enabled = false;  // Skips Update() and Render() when false
```

When disabled, gizmo drawing is completely skipped each frame.

## Drawing Primitives

### Lines

```csharp
// Draw a line from origin to destination
Gizmos.AddLine(
    Vector3.Zero,           // origin
    new Vector3(0, 5, 0),   // destination
    new Vector3(1, 1, 1)    // color (RGB, 0-1)
);

// Red line (hit indicator)
Gizmos.AddLine(Vector3.Zero, new Vector3(10, 0, 0), new Vector3(1, 0, 0), hit: true);
```

### Cubes

```csharp
// Axis-aligned cube
Gizmos.AddCube(
    Vector3.Zero,           // center position
    new Vector3(2, 2, 2),   // size (width, height, depth)
    new Vector3(0, 1, 0)    // color
);

// Oriented cube (from a transform matrix)
Gizmos.AddCube(worldMatrix, new Vector3(1, 1, 0));
```

### Spheres

```csharp
// Bounding sphere visualization
Gizmos.AddSphere(
    new Vector3(0, 1, 0),   // center
    2.5f,                   // radius
    new Vector3(0, 0, 1)    // color
);

// From a matrix (e.g., object transform)
Gizmos.AddSphere(enemy.Transform, new Vector3(1, 1, 0));
```

### Cylinders

```csharp
// Cylinder with explicit parameters
Gizmos.AddCylinder(
    Vector3.Zero,           // base position
    1.0f,                   // radius
    3.0f,                   // height
    Quaternion.Identity,    // rotation
    new Vector3(1, 0.5f, 0) // color
);

// From matrix (oriented cylinder)
Gizmos.AddCylinder(tree.Transform, new Vector3(0, 1, 1));
```

### Planes

```csharp
// Axis-aligned plane (facing up)
Gizmos.AddPlane(
    Vector3.Zero,           // position
    Vector3.UnitY,          // normal
    new Vector2(5, 5),      // size (x, z extent)
    new Vector3(1, 1, 0)    // color
);

// From matrix (oriented plane)
Gizmos.AddPlane(ground.Transform, new Vector3(1, 1, 0));
```

### Collision Volumes

Any collision volume can be drawn using the generic method:

```csharp
// Sphere
Gizmos.AddVolume(sphere, new Vector3(0, 1, 0));

// AABB
Gizmos.AddVolume(aabb, new Vector3(1, 0, 0));

// OBB
Gizmos.AddVolume(obb, new Vector3(0, 0, 1));

// Cylinder
Gizmos.AddVolume(cylinder, new Vector3(1, 1, 0));

// Frustum (camera view volume)
Gizmos.AddVolume(frustum, new Vector3(1, 0.5f, 0.5f));
```

### Axis Lines

Draw coordinate axes at the origin:

```csharp
Gizmos.AddAxisLines(5);  // Length 5 units
```

## Color System

Colors use RGB values (0–1 range):

```csharp
new Vector3(1, 0, 0)  // Red
new Vector3(0, 1, 0)  // Green
new Vector3(0, 0, 1)  // Blue
new Vector3(1, 1, 0)  // Yellow
new Vector3(1, 0, 1)  // Magenta
new Vector3(0, 1, 1)  // Cyan
new Vector3(1, 1, 1)  // White
new Vector3(0.5f, 0.5f, 0.5f)  // Gray
```

The `hit` parameter inverts colors (used for collision visualization):

```csharp
// Normal (green) → Hit (red)
Gizmos.AddVolume(sphere, new Vector3(0, 1, 0), hit: true);
```

## Gizmo Geometries

| Type | Internal Class | Vertices | Lines |
|------|---------------|----------|-------|
| Line | `GGLineSegment` | 2 | 1 |
| Cube | `GGCube` | 8 | 12 |
| Sphere | `GGSphere` | 64×3 circles | 192 edges |
| Cylinder | `GGCylinder` | 64×3 circles + 4 vertical | 196 edges |
| Plane | `GGPlane` | 4 | 5 (rectangle + diagonal) |

## Complete Example

```csharp
protected override void Render(double dt)
{
    // Draw the scene
    RenderScene();

    // Debug: draw camera frustum
    var frustum = new BoundingFrustum(Camera.View * Camera.Projection);
    Gizmos.AddVolume(frustum, new Vector3(1, 0.5f, 0.5f));

    // Debug: draw all enemy collision spheres
    foreach (var enemy in enemies)
    {
        Gizmos.AddVolume(enemy.CollisionSphere, new Vector3(1, 0, 0));
    }

    // Debug: draw ray to closest enemy
    var closest = GetClosestEnemy();
    if (closest != null)
    {
        Gizmos.AddLine(Camera.Position, closest.Position, new Vector3(0, 1, 0));
    }

    // Debug: draw grid on ground
    for (int x = -10; x <= 10; x++)
    {
        Gizmos.AddLine(new Vector3(x, 0, -10), new Vector3(x, 0, 10), new Vector3(0.3f, 0.3f, 0.3f));
        Gizmos.AddLine(new Vector3(-10, 0, x), new Vector3(10, 0, x), new Vector3(0.3f, 0.3f, 0.3f));
    }
}
```

## See Also

- [Collision System](../collisions/overview.md) — collision volumes that can be drawn
- [Camera](../core/camera.md) — frustum built from camera matrices
- [Logging](../utilities/logging.md) — ErrorListWindow for shader/runtime errors
