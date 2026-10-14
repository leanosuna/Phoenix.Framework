# Models and indirect rendering

Namespace: `Phoenix.Framework.Rendering.Geometry`

Phoenix Framework v2 provides two paths for rendering 3D geometry: direct drawing via `Model` and batched drawing via Multi-Draw Indirect (`IndirectMeshBuilder` and `IndirectMeshRenderer`). Skinned character meshes animate through `AnimatedModel`.

---

## 3D models

### Loading a model

Load 3D models using `AssetLoader`:

```csharp
Model model = AssetLoader.LoadModel("3D/sponza.fbx");
```

`AssetLoader` automatically checks `.contentbin/` for pre-parsed `.pmdl` binaries. When found, models load directly into GPU vertex and index buffers without running Assimp.

### Model hierarchy

```
Model
 └── ModelMesh[]
      └── ModelPart[]
           ├── IndexOffset & IndexCount
           ├── MaterialIndex & BoundingBox
           └── VulkanBuffer (Vertex & Index)
```

Vertices use `ModelVertex`:

| Attribute | Type | Format | Description |
|---|---|---|---|
| `Position` | `Vector3` | `R32G32B32_SFLOAT` | 3D vertex position in local space |
| `Normal` | `Vector3` | `R32G32B32_SFLOAT` | Surface normal vector |
| `Tangent` | `Vector3` | `R32G32B32_SFLOAT` | Tangent vector for normal mapping |
| `TexCoords` | `Vector2` | `R32G32_SFLOAT` | UV coordinates |
| `Color` | `Vector4` | `R32G32B32A32_SFLOAT` | Vertex tint color |

### Direct model drawing

Draw models directly into `RenderContext`:

```csharp
protected override void Render(RenderContext ctx, double deltaTime)
{
    using var pass = ctx.BeginPass(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));

    ctx.BindPipeline(_pipeline);
    ctx.BindDefaultDescriptorSets(_pipeline);
    ctx.PushConstants(_pipeline, in _pushData);

    _model.Draw(ctx, _pipeline);
}
```

---

## Multi-Draw Indirect (MDI)

Direct drawing issues separate draw calls per submesh. For complex scenes or large object counts, Multi-Draw Indirect combines geometry into a single GPU dispatch.

### 1. Packing meshes into an indirect batch

Use `IndirectMeshBuilder` during `Initialize()`:

```csharp
var builder = new IndirectMeshBuilder(Graphics.Context);

// Add submeshes from multiple models
builder.AddModel(buildingModel, buildingWorldMatrix, diffuseTextureId);
builder.AddModel(treeModel, treeWorldMatrix, treeTextureId);

IndirectDrawBatch batch = builder.Build();
var renderer = new IndirectMeshRenderer(Graphics.Context, batch);
```

The builder creates:
- A unified vertex buffer.
- A unified index buffer.
- An indirect command buffer containing `DrawIndexedIndirectCommand` structs:
  ```csharp
  public struct DrawIndexedIndirectCommand
  {
      public uint IndexCount;
      public uint InstanceCount;
      public uint FirstIndex;
      public int  VertexOffset;
      public uint FirstInstance;
  }
  ```
- A submesh metadata storage buffer (`VulkanStorageBuffer`) storing world matrices and material IDs for each submesh.

### 2. Dispatches in RenderContext

Execute all submesh draws with a single command:

```csharp
protected override void Render(RenderContext ctx, double deltaTime)
{
    using var pass = ctx.BeginPass(new Vector4(0.05f, 0.05f, 0.08f, 1.0f));

    ctx.BindPipeline(_mdiPipeline);
    ctx.BindDefaultDescriptorSets(_mdiPipeline); // Sets 0 and 1
    ctx.BindDescriptorSets(_mdiPipeline, new[] { _batch.MetadataDescriptorSet }, firstSet: 3);

    _renderer.Draw(ctx);
}
```

### 3. Reading submesh metadata in shaders

In your vertex shader, use `gl_DrawID` to fetch the transform and texture for each submesh:

```glsl
struct SubmeshData {
    mat4 World;
    uint TextureId;
    uint NormalMapId;
    uint SpecularMapId;
    uint Padding;
};

layout(set = 3, binding = 0) readonly buffer SubmeshBuffer {
    SubmeshData uSubmeshes[];
};

void main() {
    SubmeshData submesh = uSubmeshes[gl_DrawID];
    vTexCoords = aTexCoords;
    vTextureId = submesh.TextureId;

    gl_Position = uViewProjection * submesh.World * vec4(aPosition, 1.0);
}
```

---

## Skeletal animation

`AnimatedModel` loads rigged characters and evaluates skeleton poses:

```csharp
AnimatedModel character = AssetLoader.LoadAnimatedModel("3D/character.fbx");
character.PlayAnimation("Walk", loop: true);
```

### Bone calculation and GPU skinning
1. In `Update()`, `character.Update(deltaTime)` samples keyframe tracks and updates bone matrices.
2. In `Render()`, bone transforms upload to a Set 2 uniform buffer.
3. The vertex shader transforms vertices by bone weights:

```glsl
layout(set = 2, binding = 0) uniform BoneUBO {
    mat4 uBones[128];
};

void main() {
    mat4 boneTransform = uBones[aBoneIds.x] * aBoneWeights.x +
                         uBones[aBoneIds.y] * aBoneWeights.y +
                         uBones[aBoneIds.z] * aBoneWeights.z +
                         uBones[aBoneIds.w] * aBoneWeights.w;

    vec4 worldPos = uWorld * boneTransform * vec4(aPosition, 1.0);
    gl_Position = uViewProjection * worldPos;
}
```
