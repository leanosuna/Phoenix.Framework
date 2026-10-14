# Primitives

Namespace: `Phoenix.Framework.Rendering.Primitives`

Phoenix provides procedurally generated 3D geometry primitives (cube, sphere, cylinder, plane, pyramid, frustum) for prototyping, collision testing, and debug visualizations. Primitives generate GPU vertex and index buffers encapsulated in a `ModelMesh`.

---

## API

Every primitive derives from `Primitive` and provides:

```csharp
public void Draw(RenderContext rc)               // Binds vertex/index buffers and draws
public T[] GetVertexData<T>() where T : unmanaged // Access raw vertex records
public uint[] GetIndexData()                     // Access index buffer data
public ModelMesh? Mesh { get; }                  // Underlying ModelMesh handle
```

The framework initializes primitive rendering automatically during `InternalLoad()`.

---

## Creating primitives

Create primitives through their concrete static `Create` factories:

```csharp
// Default cube
var cube = Cube.Create();

// Sphere with custom subdivision count
var sphere = Sphere.Create(new InfoSphere { SubDivisions = 32 });

// Cylinder
var cylinder = Cylinder.Create(new InfoCylinder { SubDivisions = 24 });
```

---

## Configuration with PrimitiveInfo

Info classes customize attribute generation:

| Field | Type | Default | Description |
|---|---|---|---|
| `Uv` | `bool` | `false` | Generates texture coordinates |
| `Normals` | `bool` | `false` | Generates vertex normals |
| `Tangents` | `bool` | `false` | Generates tangents for normal mapping |
| `Bitangents` | `bool` | `false` | Generates bitangents |
| `SaveVertices` | `bool` | `false` | Retains CPU copy of raw vertex bytes |

Per-type fields:

| Info class | Specific fields |
|---|---|
| `InfoCube` | None (inherits `PrimitiveInfo`) |
| `InfoSphere` | `SubDivisions` (default 16) |
| `InfoCylinder` | `SubDivisions` (default 16) |
| `InfoFrustum` | `NearWidth`, `NearHeight`, `FarWidth`, `FarHeight`, `Depth` |
| `InfoPlane` | None |
| `InfoPyramid` | None |

---

## Example

```csharp
public class MyGame : PhoenixGame
{
    private Cube _cube = null!;
    private VulkanPipeline _pipeline = null!;

    protected override void Initialize()
    {
        _cube = Cube.Create(new InfoCube { Normals = true, Uv = true });
        _pipeline = CreatePipeline();
    }

    protected override void Render(RenderContext ctx, double deltaTime)
    {
        using var pass = ctx.BeginPass(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));

        ctx.BindPipeline(_pipeline);
        ctx.BindDefaultDescriptorSets(_pipeline);
        ctx.PushConstants(_pipeline, new PushConstantsData { World = Matrix4x4.Identity });

        _cube.Draw(ctx);
    }
}
```
