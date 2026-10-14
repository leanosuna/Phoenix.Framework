# Shaders and pipelines

Namespace: `Phoenix.Framework.Rendering.Vulkan`

In Phoenix Framework v2, shaders run through explicit Vulkan pipelines (`VulkanPipeline` for graphics and `VulkanComputePipeline` for compute). Pipelines combine SPIR-V shader bytecode, vertex input layouts, rasterization settings, blend states, and descriptor layouts.

---

## Vulkan graphics pipelines

Pipelines are immutable Vulkan objects. Build them during `Initialize()` using `VulkanPipelineDescription`:

```csharp
VulkanPipelineDescription desc = new()
{
    VertexShaderBytes = vertSpvBytes,
    FragmentShaderBytes = fragSpvBytes,
    VertexInputState = ModelVertex.GetVertexInputState(),
    ColorAttachmentFormat = Graphics.Swapchain.ColorFormat,
    DepthAttachmentFormat = Graphics.Swapchain.DepthFormat,
    DepthTest = true,
    DepthWrite = true,
    CullMode = CullModeFlags.BackBit,
    FrontFace = FrontFace.CounterClockwise,
    BlendMode = BlendMode.Alpha
};

_pipeline = new VulkanPipeline(Graphics.Context, desc);
```

### Dynamic state
All graphics pipelines activate dynamic viewport and scissor states. Resizing the window or changing viewports does not require pipeline recreation.

---

## Standard descriptor sets

Phoenix organizes descriptor sets into standardized frequencies:

| Set | Name | Lifetime | Contents |
|---|---|---|---|
| **Set 0** | `CommonUBO` | Per frame | Camera matrices, camera position, screen resolution, time, delta time |
| **Set 1** | `BindlessManager` | Global | Global array of 4096 or 16384 sampled images (`uTextures[]`) |
| **Set 2** | `BoneLayout` | Per model | Up to 128 bone transformation matrices for skeletal animation |
| **Set 3** | Custom / SSBO | As needed | Submesh metadata for Multi-Draw Indirect or custom storage buffers |

In GLSL shaders:

```glsl
// Set 0: Common frame data
layout(set = 0, binding = 0) uniform CommonUBO {
    mat4 uView;
    mat4 uProjection;
    mat4 uViewProjection;
    mat4 uInverseView;
    mat4 uInverseProjection;
    vec4 uCameraPosition;
    vec2 uResolution;
    float uTime;
    float uDeltaTime;
};

// Set 1: Global bindless texture array
layout(set = 1, binding = 0) uniform sampler2D uTextures[];

// Set 2: Skeletal animation bones (if applicable)
layout(set = 2, binding = 0) uniform BoneUBO {
    mat4 uBones[128];
};

// Set 3: Submesh metadata (for Multi-Draw Indirect)
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
```

---

## 80-byte push constants

To avoid reallocating or rebinding descriptor sets for individual draw calls, Phoenix uses an 80-byte push constant layout:

```csharp
public struct PushConstantsData
{
    public Matrix4x4 World;        // 64 bytes
    public uint TextureId;         // 4 bytes
    public uint NormalMapId;       // 4 bytes
    public uint SpecularMapId;     // 4 bytes
    public uint Flags;             // 4 bytes
}
```

In your GLSL vertex and fragment shaders:

```glsl
layout(push_constant) uniform PushConstants {
    mat4 uWorld;
    uint uTextureId;
    uint uNormalMapId;
    uint uSpecularMapId;
    uint uFlags;
};

void main() {
    // Sample texture by index from Set 1
    vec4 albedo = texture(uTextures[nonuniformEXT(uTextureId)], vTexCoords);
}
```

Push constants transmit directly to hardware command registers via `ctx.PushConstants(_pipeline, in pushData)`.

---

## Binding pipelines in RenderContext

Bind pipelines and descriptors during command recording:

```csharp
protected override void Render(RenderContext ctx, double deltaTime)
{
    using var pass = ctx.BeginPass(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));

    ctx.BindPipeline(_pipeline);
    ctx.BindDefaultDescriptorSets(_pipeline); // Binds Set 0 and Set 1
    ctx.PushConstants(_pipeline, in _pushConstants);

    ctx.BindVertexBuffer(_vertexBuffer);
    ctx.BindIndexBuffer(_indexBuffer);
    ctx.DrawIndexed(_indexCount);
}
```

---

## Compute pipelines

For GPU compute workloads (such as particle simulations or image processing), use `VulkanComputePipeline`:

```csharp
var computeDesc = new VulkanComputePipelineDescription
{
    ComputeShaderBytes = computeSpvBytes,
    DescriptorSetLayouts = new[] { _storageLayout }
};

var computePipeline = new VulkanComputePipeline(Graphics.Context, computeDesc);

// Dispatch workgroups
ctx.BindPipeline(computePipeline);
ctx.BindDescriptorSets(computePipeline, new[] { _storageSet });
ctx.Dispatch(groupCountX: 64, groupCountY: 64, groupCountZ: 1);
```
