# Serializable Volumes

Namespace: `Phoenix.Framework.Collisions`

`SerializableVolume` is an abstract volume type that serializes to JSON and can draw its own gizmo — designed for editor-authored collision volumes (e.g., a level editor) that are saved to a `Volumes.json` file and loaded at runtime.

## BoundingVolumeType

```csharp
public enum BoundingVolumeType
{
    AABB, OBB, Cylinder, Sphere, Capsule
}
```

## SerializableVolume (abstract)

| Member | Description |
|--------|-------------|
| `string Name` | Volume name |
| `bool Visible` | Whether the gizmo is drawn |
| `bool Selected` | Editor selection flag |
| `abstract BoundingVolumeType Type` | Which volume type this is |
| `abstract void DrawGizmo(Gizmos gizmos)` | Draw the volume using the gizmo system |
| `abstract JsonObject Serialize()` | Convert to JSON |
| `static SerializableVolume Deserialize(JsonObject data)` | Recreate a volume from JSON (type-dispatching) |

## Concrete Types

| Class | Fields | Implicit conversion |
|-------|--------|--------------------|
| `SerializableAABB` | `Center`, `Size` (default `1,1,1`) | `→ AxisAlignedBoundingBox` |
| `SerializableOBB` | `Position`, `Size`, `Yaw`, `Pitch`, `Roll` | `→ OrientedBoundingBox` (+ `ToOBB()`) |
| `SerializableCylinder` | `Position`, `Radius` (1), `Height` (2), `Rotation` | `→ BoundingCylinder` (+ `ToCylinder()`) |
| `SerializableSphere` | `Position`, `Radius` (1) | `→ BoundingSphere` |
| `SerializableCapsule` | `PointA`, `PointB`, `Radius` (0.5) | `→ Capsule` |

Each class exposes `Type`, `DrawGizmo()`, `Serialize()`, and a `new static` `Deserialize(JsonObject)` override.

## VolumeSerializer

Saves/loads a whole volume list to/from `Volumes.json` in the working directory:

```csharp
VolumeSerializer.Save(volumes);              // writes Volumes.json
List<SerializableVolume> vols = VolumeSerializer.Load();  // reads it (empty list if missing)
```

## Usage

```csharp
// Build a volume
var aabb = new SerializableAABB("door-collider",
    new Vector3(0, 1, 0), new Vector3(2, 2, 0.1f));

// Persist
VolumeSerializer.Save(new List<SerializableVolume> { aabb });

// Reload at runtime
foreach (var vol in VolumeSerializer.Load())
{
    // Draw it (respects Visible)
    vol.DrawGizmo(Gizmos);

    // Implicitly convert to the concrete volume for tests
    if (vol.Type == BoundingVolumeType.AABB)
    {
        var box = (AxisAlignedBoundingBox)vol;   // implicit conversion
        ...
    }
}
```

## Collision Integration

Serializable volumes plug directly into the manifold and ray-cast APIs, which accept any volume via the abstract base:

- `Collision.CapsuleVsVolume(capsule, vol, out dir, out depth)` — see [Collision Manifolds](manifold.md)
- `Collision.RayVsVolume(ray, vol, out fraction, out point, out normal)`

## See Also

- [Collision Manifolds](manifold.md) — resolution against any `SerializableVolume`
- [Ray Casts](raycasts.md) — ray vs any `SerializableVolume`
- [Gizmos](../rendering/gizmos.md) — `AddVolume<T>()` and per-type `DrawGizmo`
