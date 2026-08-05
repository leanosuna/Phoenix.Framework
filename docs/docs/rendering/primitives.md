# Primitives

Namespace: `Phoenix.Framework.Rendering.Primitives`

Phoenix provides procedurally generated geometry primitives (cube, sphere, cylinder, plane, pyramid, frustum) for debug rendering, prototyping, and quick meshes. Each primitive is generated on the GPU at creation time and can be drawn with its own VAO, or queried for vertex/index data.

## API

```csharp
public static void SetGL(GL gl)  // Required once before creating any primitive
```

Every primitive (`Cube`, `Sphere`, `Cylinder`, `Plane`, `Pyramid`, `Frustum`) derives from `Primitive` and offers:

```csharp
public void Draw()                              // Bind VAO + draw elements
public T[] GetVertexData<T>() where T : unmanaged  // Download vertex data
public uint[] GetIndexData()                    // Download index data
```

Each concrete primitive is created through static factories and exposes its info as a public field (`CubeInfo`, `SphereInfo`, `CylinderInfo`, `PlaneInfo`, `PyramidInfo`, `FrustumInfo`):

```csharp
var cube = Cube.Create();
var sphere = Sphere.Create(new InfoSphere { SubDivisions = 32 });
```

## Info Classes

All info classes derive from `PrimitiveInfo`, which exposes toggles for generated attributes:

| Field | Type | Default |
|-------|------|---------|
| `MeshPrimitiveType` | `PrimitiveType` | `Triangles` |
| `Uv` | `bool` | `false` |
| `Normals` | `bool` | `false` |
| `Tangents` | `bool` | `false` |
| `Bitangents` | `bool` | `false` |
| `SaveVertices` | `bool` | `false` |

Per-type fields:

| Info Class | Fields (defaults) |
|------------|-------------------|
| `InfoCube` | — (inherits `PrimitiveInfo` only) |
| `InfoSphere` | `SubDivisions` (16) |
| `InfoCylinder` | `SubDivisions` (16) |
| `InfoFrustum` | `NearWidth` (1.0), `NearHeight` (1.0), `FarWidth` (2.0), `FarHeight` (2.0), `Depth` (1.0) |
| `InfoPlane` | — |
| `InfoPyramid` | — |

## Example

```csharp
Primitive.SetGL(GL);   // once at startup

// Simple cube
var cube = Cube.Create();
cube.Draw();

// Sphere with higher tessellation and normals
var sphere = Sphere.Create(new InfoSphere
{
    SubDivisions = 48,
    Normals = true,
    SaveVertices = true
});

// Read back vertex data
var verts = sphere.GetVertexData<VertexPosUvNorm>();  // your own vertex struct
```

## See Also

- [Gizmos](gizmos.md) — debug wireframe drawing (no vertex management needed)
- [Graphics & Rendering](../core/graphics.md) — shaders to draw primitives with
- [Models & Animation](models.md) — imported model meshes
