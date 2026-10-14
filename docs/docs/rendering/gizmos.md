# Gizmos

Namespace: `Phoenix.Framework.Rendering.Gizmos`

Gizmos provide immediate-mode debug visualization for 3D geometry, collision hulls, rays, and spatial orientations. They render as colored wireframes during the frame rendering sequence.

---

## Access and control

Access the gizmo system through `PhoenixGame.Gizmos`:

```csharp
Gizmos.Enabled = true;   // Default: true
Gizmos.Enabled = false;  // Skips update and draw commands when false
```

When enabled, gizmo line commands queue during `Update()` or `Render()`, and draw automatically before user UI passes.

---

## Drawing primitives

### Lines and rays

```csharp
// Line between two world points
Gizmos.AddLine(new Vector3(0, 0, 0), new Vector3(0, 5, 0), new Vector3(1, 1, 1));

// Ray with hit indicator
Gizmos.AddLine(origin, target, new Vector3(1, 0, 0), hit: true);
```

### Cubes and boxes

```csharp
// Axis-aligned cube
Gizmos.AddCube(center: Vector3.Zero, size: new Vector3(2, 2, 2), color: new Vector3(0, 1, 0));

// Oriented box from a world matrix
Gizmos.AddCube(worldMatrix, new Vector3(1, 1, 0));
```

### Spheres

```csharp
// Center and radius
Gizmos.AddSphere(center: new Vector3(0, 1, 0), radius: 2.5f, color: new Vector3(0, 0, 1));

// Oriented sphere from transform
Gizmos.AddSphere(entityTransform, new Vector3(0, 1, 1));
```

### Capsules and cylinders

```csharp
// Capsule between two endpoints
Gizmos.AddCapsule(pointA, pointB, radius: 0.5f, color: new Vector3(0, 1, 1));

// Cylinder with height and radius
Gizmos.AddCylinder(basePosition, radius: 1f, height: 3f, Quaternion.Identity, color: new Vector3(1, 0.5f, 0));
```

### Collision volumes

Draw any geometric collision volume directly:

```csharp
Gizmos.AddVolume(boundingSphere, new Vector3(0, 1, 0));
Gizmos.AddVolume(aabb, new Vector3(1, 0, 0));
Gizmos.AddVolume(obb, new Vector3(0, 0, 1));
Gizmos.AddVolume(boundingFrustum, new Vector3(1, 0.5f, 0.5f));
```

### Coordinate frames

Draw XYZ axis lines to visualize local orientation:

```csharp
Gizmos.AddCoordinateFrame(worldMatrix, axisLength: 2f);
Gizmos.AddAxisLines(length: 5f);
```

---

## Color system

Colors are RGB vectors with values between 0.0 and 1.0:

```csharp
new Vector3(1, 0, 0); // Red
new Vector3(0, 1, 0); // Green
new Vector3(0, 0, 1); // Blue
new Vector3(1, 1, 0); // Yellow
new Vector3(1, 1, 1); // White
```

Passing `hit: true` inverts the color, useful for collision hit confirmation.

---

## Example

```csharp
protected override void Render(RenderContext ctx, double deltaTime)
{
    using var pass = ctx.BeginPass(new Vector4(0.05f, 0.05f, 0.08f, 1f));

    // Draw main scene geometry
    _sceneModel.Draw(ctx, _pipeline);

    // Queue debug visualizations
    var frustum = new BoundingFrustum(Camera!.View * Camera.Projection);
    Gizmos.AddVolume(frustum, new Vector3(1, 0.5f, 0.5f));

    foreach (var enemy in _enemies)
    {
        Gizmos.AddVolume(enemy.BoundingSphere, new Vector3(1, 0, 0));
    }
}
```
