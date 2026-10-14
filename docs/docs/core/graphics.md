# Graphics and rendering

Namespace: `Phoenix.Framework.Rendering`

The `Graphics` class manages Vulkan devices, swapchain presentation, uniform buffer pools, bindless descriptors, and command recording. Exposed as `PhoenixGame.Graphics`.

---

## Core properties

| Property | Type | Description |
|---|---|---|
| `Context` | `VulkanContext` | Vulkan instance, physical device, logical device, and graphics/present queue families |
| `Swapchain` | `VulkanSwapchain` | Double-buffered swapchain, depth-stencil buffer, and image presentation synchronization |
| `RenderContext` | `RenderContext` | Active command recording context for dynamic passes, pipeline binding, and draw dispatches |
| `Bindless` / `BindlessManager` | `BindlessManager` | Global texture descriptor array (Set 1) indexing up to 4096 or 16384 sampled images |
| `CommonUbo` | `CommonUBOManager` | Manages per-frame camera matrices and timing uniforms (Set 0) |
| `Viewport` | `RenderViewport` | Viewport scaling controller for decoupling render resolution from window size |
| `SceneRenderTarget` | `VulkanRenderTarget` | Offscreen scene color and depth target used when viewport scaling is active |
| `ClearColor` | `Vector4` | Default clear color for rendering passes |
| `RenderHaltKey` | `Key` | Keyboard key used to toggle render halt (default `F11`) |
| `RenderHalt` | `bool` | Current render halt state |

---

## Vulkan context and swapchain

### VulkanContext
`Graphics.Context` selects discrete GPUs by default and activates required Vulkan 1.3 features:
- Dynamic rendering (`VK_KHR_dynamic_rendering`).
- Synchronization2 (`VK_KHR_synchronization2`).
- Descriptor indexing with partially bound descriptor arrays.
- Multi-Draw Indirect (`multiDrawIndirect` and `drawParameters`).

### VulkanSwapchain
`Graphics.Swapchain` manages two frames in flight:
- **Presentation modes**: Supports `Fifo` (VSync locked), `Mailbox` (triple-buffering without tearing), and `Immediate` (uncapped with tearing).
- **Double-buffered synchronization**: Each frame has dedicated command buffers, image-available semaphores, render-finished semaphores, and CPU execution fences.
- **Dynamic recreation**: When the window is resized or minimized, `Swapchain.Recreate()` rebuilds color image views and depth-stencil attachments without device destruction.

---

## RenderContext and dynamic passes

In Phoenix v2, all rendering commands are recorded through `RenderContext`.

```csharp
protected override void Render(RenderContext ctx, double deltaTime)
{
    // Begin a scoped dynamic pass
    using (var pass = ctx.BeginPass(new Vector4(0.1f, 0.12f, 0.16f, 1.0f)))
    {
        ctx.BindPipeline(_pipeline);
        ctx.BindDefaultDescriptorSets(_pipeline);
        ctx.PushConstants(_pipeline, in _pushConstants);

        ctx.BindVertexBuffer(_vertexBuffer);
        ctx.BindIndexBuffer(_indexBuffer);
        ctx.DrawIndexed(_indexCount);
    }
}
```

### Dynamic pass methods

- `BeginPass(Vector4 clearColor, float clearDepth = 1.0f)`: Starts dynamic rendering on the scaled scene target if viewport scaling is enabled, or directly on the swapchain backbuffer. Returns a `ScopedPass` struct that invokes `EndPass()` when disposed.
- `BeginPass(VulkanRenderTarget target, Vector4 clearColor, float clearDepth = 1.0f)`: Directs dynamic rendering to an offscreen render target.
- `BeginSwapchainPass(Vector4 clearColor, float clearDepth = 1.0f)`: Explicitly begins rendering directly to the swapchain image.
- `EndPass()`: Closes `vkCmdEndRendering` and transitions attachment layouts.

### Pipeline and resource binding

- `BindPipeline(VulkanPipeline pipeline)`: Binds graphics pipeline and sets dynamic viewport and scissor rects.
- `BindDefaultDescriptorSets(VulkanPipeline pipeline)`: Binds Set 0 (`CommonUBO`) and Set 1 (`Bindless`).
- `BindDescriptorSets(VulkanPipeline pipeline, DescriptorSet[] sets, uint firstSet = 0)`: Binds custom descriptor sets.
- `PushConstants(VulkanPipeline pipeline, in PushConstantsData data)`: Writes 80-byte push constants directly to GPU command registers.
- `BindVertexBuffer(VulkanBuffer buffer, ulong offset = 0)`: Binds vertex attributes.
- `BindIndexBuffer(VulkanBuffer buffer, IndexType indexType = IndexType.Uint32, ulong offset = 0)`: Binds index buffer.

### Drawing commands

- `Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)`: Non-indexed draw.
- `DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)`: Indexed draw.
- `DrawIndexedIndirect(VulkanBuffer buffer, ulong offset, uint drawCount, uint stride)`: Dispatches multi-mesh indirect draw batches.
- `Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)`: Dispatches compute pipelines.
- `BlitToSwapchain(VulkanRenderTarget target, Filter filter = Filter.Linear)`: Scales and copies an offscreen target to the swapchain backbuffer.

---

## RenderViewport scaling

`Graphics.Viewport` allows running the 3D scene at a lower internal resolution for performance while maintaining native resolution for ImGui overlays:

```csharp
// Render scene at 720p and upscale with linear filtering to a 1080p window
Graphics.Viewport.Enabled = true;
Graphics.Viewport.SetVirtualResolution(1280, 720);
Graphics.Viewport.Filter = Silk.NET.Vulkan.Filter.Linear;
```

When enabled:
1. `ctx.BeginPass()` renders to `Graphics.SceneRenderTarget`.
2. At the end of 3D rendering, the framework blits `SceneRenderTarget` to the swapchain backbuffer.
3. `RenderUI()` draws ImGui overlays directly on the backbuffer at native pixel resolution.

---

## Synchronization and GPU idle

Call `Graphics.WaitIdle()` to flush the entire GPU queue before destroying or recreating heavy resources:

```csharp
Graphics.WaitIdle();
_meshBuffer.Dispose();
```
