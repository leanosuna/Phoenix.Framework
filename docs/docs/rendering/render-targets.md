# Render targets

Namespace: `Phoenix.Framework.Rendering.Vulkan`

`VulkanRenderTarget` creates offscreen color and depth buffers for multi-pass rendering, deferred shading G-buffers, shadow maps, and post-processing effects.

---

## Creating a render target

Instantiate `VulkanRenderTarget` during `Initialize()`:

```csharp
// Standard 1080p color + depth target
var renderTarget = new VulkanRenderTarget(
    Graphics.Context,
    width: 1920,
    height: 1080,
    colorFormat: Format.R8G8B8A8Unorm,
    depthFormat: Format.D32Sfloat
);
```

For HDR rendering or bloom buffers, use floating-point formats:

```csharp
var hdrTarget = new VulkanRenderTarget(
    Graphics.Context,
    width: 1920,
    height: 1080,
    colorFormat: Format.R16G16B16A16Sfloat,
    depthFormat: Format.D32Sfloat
);
```

---

## Offscreen dynamic passes

Direct rendering to a target using `ctx.BeginPass(target, clearColor)`:

```csharp
protected override void Render(RenderContext ctx, double deltaTime)
{
    // Pass 1: Render scene geometry into offscreen HDR target
    using (var pass = ctx.BeginPass(_hdrTarget, new Vector4(0.05f, 0.05f, 0.08f, 1.0f)))
    {
        ctx.BindPipeline(_scenePipeline);
        ctx.BindDefaultDescriptorSets(_scenePipeline);
        _sceneModel.Draw(ctx, _scenePipeline);
    }

    // Pass 2: Render post-processing quad directly to swapchain
    using (var pass = ctx.BeginSwapchainPass(Vector4.Zero))
    {
        ctx.BindPipeline(_bloomPipeline);
        // Bind _hdrTarget.ColorTexture as input texture
        ctx.PushConstants(_bloomPipeline, new PushConstantsData { TextureId = _hdrTarget.ColorTexture.TextureId });
        ctx.Draw(3); // Fullscreen triangle
    }
}
```

When `EndPass()` executes on an offscreen target, `RenderContext` automatically transitions the color attachment layout from `ColorAttachmentOptimal` to `ShaderReadOnlyOptimal` using `VK_KHR_synchronization2` pipeline barriers.

---

## Blitting to the swapchain

To copy and scale an offscreen target directly to the swapchain backbuffer without a post-processing shader pass, use `ctx.BlitToSwapchain()`:

```csharp
ctx.BlitToSwapchain(_sceneTarget, Filter.Linear);
```

This operation performs hardware-accelerated image blitting and handles layout transitions to `PresentSrc`.

---

## Resizing render targets

When the window resizes, update offscreen targets to match the new dimensions:

```csharp
protected override void OnWindowResize(Vector2 size)
{
    Graphics.WaitIdle();
    _sceneTarget.Resize((uint)size.X, (uint)size.Y);
}
```
Always call `Graphics.WaitIdle()` before resizing or recreating render targets to ensure GPU commands are not actively writing to target memory.
