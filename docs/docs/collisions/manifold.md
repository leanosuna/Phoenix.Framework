# Collision Manifolds

Namespace: `Phoenix.Framework.Collisions`

The static `Collision` class resolves contacts between pairs of collision volumes. Unlike the `Intersects()`/`Contains()` boolean tests, every method produces a **manifold**: a push-out direction and penetration depth you can use to resolve the collision (e.g., move the player out of a wall).

## API

All methods return `true` when the volumes intersect, and fill `pushDir`/`depth`:

```csharp
public static bool SphereVsAABB(BoundingSphere sphere, AxisAlignedBoundingBox aabb, out Vector3 pushDir, out float depth)
public static bool SphereVsOBB(BoundingSphere sphere, OrientedBoundingBox obb, out Vector3 pushDir, out float depth)
public static bool SphereVsCylinder(BoundingSphere sphere, BoundingCylinder cyl, out Vector3 pushDir, out float depth)
public static bool SphereVsSphere(BoundingSphere a, BoundingSphere b, out Vector3 pushDir, out float depth)
public static bool CapsuleVsAABB(Capsule cap, AxisAlignedBoundingBox aabb, out Vector3 pushDir, out float depth)
public static bool CapsuleVsOBB(Capsule cap, OrientedBoundingBox obb, out Vector3 pushDir, out float depth)
public static bool CapsuleVsCylinder(Capsule cap, BoundingCylinder cyl, out Vector3 pushDir, out float depth)
public static bool CapsuleVsSphere(Capsule cap, BoundingSphere sphere, out Vector3 pushDir, out float depth)
public static bool CapsuleVsCapsule(Capsule a, Capsule b, out Vector3 pushDir, out float depth)
public static bool CapsuleVsVolume(Capsule cap, SerializableVolume vol, out Vector3 pushDir, out float depth)
public static bool RayVsVolume(Ray ray, SerializableVolume vol, out float hitFraction, out Vector3 hitPoint, out Vector3 hitNormal)
```

## Resolving a Contact

```csharp
if (Collision.SphereVsAABB(playerSphere, wallBox, out var pushDir, out var depth))
{
    playerSphere.Center += pushDir * depth;  // push out of the wall
}
```

- `pushDir` — unit vector pointing from the volume toward the sphere (the direction to move the sphere to clear the collision)
- `depth` — penetration distance to push along `pushDir`

## Capsule

`Capsule` is a simple struct — a line segment (A to B) with a radius:

```csharp
public struct Capsule
{
    public Vector3 PointA;
    public Vector3 PointB;
    public float Radius;

    public Capsule(Vector3 a, Vector3 b, float r)
}
```

## Serializable Volumes

`CapsuleVsVolume` and `RayVsVolume` accept any [`SerializableVolume`](serializable.md) (AABB, OBB, cylinder, sphere, capsule), which lets you resolve against editor-authored volumes without knowing their concrete type:

```csharp
// All volumes in your scene, regardless of type
foreach (var vol in mySerializableVolumes)
{
    if (Collision.CapsuleVsVolume(playerCapsule, vol, out var dir, out var depth))
        Resolve(playerCapsule, dir, depth);
}
```

## See Also

- [Collision System Overview](overview.md) — boolean intersection tests
- [Ray Casts](raycasts.md) — `RayVs*` with hit point/normal/fraction
- [Serializable Volumes](serializable.md) — the volume hierarchy used by `*VsVolume`
- [Gizmos](../rendering/gizmos.md) — visualize volumes with `AddVolume`
