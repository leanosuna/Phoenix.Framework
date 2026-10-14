# Collision and physics overview

Phoenix Framework provides two complementary collision systems:

1. **Standalone geometric query volumes**: Pure mathematical volumes in `Phoenix.Framework.Collisions` for ray casting, visibility frustum culling, trigger detection, and distance queries without physics overhead.
2. **Rigid-body dynamics**: `PhysicsWorld` in `Phoenix.Framework.Physics`, wrapping Box3D.NET to simulate dynamic bodies, gravity, impulses, stacking, and physics raycasts.

---

## Standalone geometric volumes

| Volume | Class | Description |
|---|---|---|
| Ray | `Ray` | Ray origin and direction for line-of-sight and pick testing |
| Bounding sphere | `BoundingSphere` | Center and radius |
| Axis-aligned box | `AxisAlignedBoundingBox` | Min and max bounds aligned to coordinate axes |
| Oriented box | `OrientedBoundingBox` | Position, extents, and rotation matrix |
| Bounding frustum | `BoundingFrustum` | Six clipping planes constructed from camera matrices |
| Cylinder | `BoundingCylinder` | Position, radius, height, and orientation |
| Capsule | `Capsule` | Line segment between points A and B plus radius |

---

## Intersection query types

- `bool Intersects(...)`: Returns true if two volumes overlap.
- `ContainmentType Contains(...)`: Returns `Disjoint`, `Intersects`, or `Contains`.
- `float? Intersects(Ray)`: Returns hit distance along the ray, or null if missed.
- `CollisionManifold`: Calculates contact normal, penetration depth, and contact points between two overlapping shapes.

---

## Rigid-body physics (PhysicsWorld)

For games requiring full Newtonian simulation (falling objects, collisions, player locomotion against obstacles), instantiate `PhysicsWorld`:

```csharp
var physics = new PhysicsWorld(new Vector3(0, -9.81f, 0));

// Create static floor
var floor = physics.CreateStaticBox(new Vector3(0, -0.5f, 0), new Vector3(100, 1, 100));

// Create falling crate
var box = physics.CreateDynamicBox(new Vector3(0, 10, 0), new Vector3(1, 1, 1));

// Advance simulation in Update()
physics.Step((float)deltaTime, subSteps: 4);
```

See [Physics World](physics.md) for full details on bodies, shapes, gravity, and physics raycasts.
