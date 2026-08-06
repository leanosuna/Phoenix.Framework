---
name: phoenix-collision
description: Ray casting, collision volumes (sphere, AABB, OBB, frustum, cylinder, capsule), manifold resolution, and serializable volumes for games built on Phoenix.Framework. Use for intersection tests, raycasts, push-out resolution, or volume gizmos.
license: MIT
compatibility: opencode, claude, agents
metadata:
  package: Phoenix.Framework
  version: "1.0.3"
---

# Phoenix.Framework — Collision System

Pure-math 3D collision — no physics engine. All types in `Phoenix.Framework.Collisions`.

## Key types

- `Ray` — `Intersects(sphere/aabb/plane/triangle)` → distance or `null`.
- `RayCasts` (static) — `RayVsAABB`, `RayVsOBB`, `RayVsSphere`, `RayVsCapsule`, `RayVsTriangle`, `RayVsCylinder` → `(hitFraction, hitPoint, hitNormal)`.
- `Collision` (static, manifold API) — `SphereVsAABB/OBB/Cylinder/Sphere`, `CapsuleVs*`, `CapsuleVsVolume`, `RayVsVolume` → `(pushDir, depth)`.
- Volumes: `BoundingSphere`, `AxisAlignedBoundingBox` (ctor is `(center, size)`), `OrientedBoundingBox` (`Position/Size/Orientation`), `BoundingFrustum`, `BoundingCylinder` (`Position/Radius/HalfHeight/Rotation`), `Capsule` (`PointA/PointB/Radius`).
- Serializable (editor-authored, JSON): `SerializableVolume` + `SerializableAABB/OBB/Cylinder/Sphere/Capsule`, `VolumeSerializer.Save/Load` (`Volumes.json`).
- `Plane`/`PlaneHelper`, `ContainmentType`, `PlaneIntersectionType`, `BoxCylinderIntersection`.

## Gotchas

- `AABB` ctor is `(center, size)` — a "min/max" pair will misplace the box.
- `BoundingFrustum.Contains(Vector3)` is implemented; `Intersects(Ray)` does a plane-slab sweep.
- Draw any volume with `Gizmos.AddVolume(volume, color)`.

## Details

See the website docs:
- Overview: https://framework.nx.net.ar/collisions/overview/
- Ray Casting: https://framework.nx.net.ar/collisions/ray/
- Ray Casts: https://framework.nx.net.ar/collisions/raycasts/
- Bounding Spheres: https://framework.nx.net.ar/collisions/spheres/
- Frustums & Boxes: https://framework.nx.net.ar/collisions/frustum-box/
- Collision Manifolds: https://framework.nx.net.ar/collisions/manifold/
- Serializable Volumes: https://framework.nx.net.ar/collisions/serializable/
