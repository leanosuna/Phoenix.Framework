# Graphics & Rendering

Namespace: `Phoenix.Framework.Rendering`

The `Graphics` class manages OpenGL state, render targets, and the rendering pipeline. Exposed as `PhoenixGame.Graphics`.

## Properties

| Name | Type | Description |
|---|---|---|
| `RenderHaltKey` | `Key` | Key that toggles render halt (default `F11`) |
| `RenderViewport` | `RenderViewport` | Viewport scale control for render targets |
| `DepthTest` | `(bool Enabled, GLEnum Function)` | Current depth test state |
| `AlphaBlend` | `(bool Enabled, BlendingFactor Source, BlendingFactor Destination)` | Current blend state |
| `FaceCulling` | `(bool Enabled, GLEnum Face, bool FrontIsCcw)` | Current face culling state |
| `PolygonModeState` | `(TriangleFace Face, PolygonMode Mode)` | Current polygon rasterization mode |
| `DepthWrite` | `bool` | Whether depth buffer writes are enabled |
| `ColorWrite` | `(bool R, bool G, bool B, bool A)` | Per-channel color write mask |
| `StencilWriteMask` | `uint` | Stencil buffer write mask |

## Metrics

Performance counters are grouped in the `Metrics` property.

| Name | Type | Description |
|---|---|---|
| `Time` | `double` | Total elapsed time in seconds |
| `FrameTime` | `double` | Render frame delta time |
| `UpdateDeltaTime` | `double` | Update loop delta time |
| `FT_SAMPLE` | `int` | Smoothed frame time sample (ms) |
| `FT_SAMPLE_RATE` | `double` | Frame time sample rate (default `0.3`) |
| `FPS` | `double` | Instantaneous frames per second |
| `FPS_SAMPLE` | `int` | Smoothed FPS sample |
| `FPS_SAMPLE_RATE` | `double` | FPS sample rate (default `0.3`) |
| `UPD_SAMPLE` | `int` | Smoothed updates per second sample |
| `UPD_SAMPLE_RATE` | `double` | Update sample rate (default `0.3`) |


## Methods

### Window

| Method | Description |
|---|---|
| `SetResolution(Vector2 size, bool fullscreen = true)` | Set window resolution, optionally fullscreen |
| `SetResolution(Vector2 size, Vector2 position, bool fullscreen = true)` | Set window resolution with custom position |
| `SetWindowBorder(WindowBorder type)` | Change window border style |

### GL State

All state methods are **idempotent** — they track the current value and skip redundant GL calls.

```csharp
// Depth testing
Graphics.SetDepthTest(true);
Graphics.SetDepthTest(true, GLEnum.Less);

// Alpha blending
Graphics.SetAlphaBlend(true);
Graphics.SetAlphaBlend(true, BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

// Face culling
Graphics.SetFaceCulling(true);
Graphics.SetFaceCulling(true, GLEnum.Back, frontIsCcw: true);

// Polygon mode
Graphics.SetPolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

// Depth write
Graphics.SetDepthWrite(true);

// Color write mask
Graphics.SetColorWrite(true, true, true, false);  // disable alpha write

// Stencil mask
Graphics.SetStencilWriteMask(0xFF);
```

### Clearing

```csharp
Graphics.SetClearColor(new Vector4(0.1f, 0.15f, 0.2f, 1f));
Graphics.ClearRenderTarget();
Graphics.ClearRenderTarget(true, true, false);  // color + depth only
```

### Render Target Operations

| Method | Description |
|---|---|
| `SetRenderToTarget(RenderTarget target)` | Redirect rendering to an off-screen render target |
| `SetRenderToScreen()` | Return rendering to the default scene render target |
| `BuildRenderTarget()` | `RTBuilder` fluent API to create a new render target |
| `BuildTargetTexture()` | `RTTBuilder` fluent API to create a texture attachment |
| `NewRenderTarget()` | Create a simple default render target (1 color + depth) |
| `TryFindByName(string name, out RenderTarget target)` | Look up a render target by name |
| `CopyToScreen(RenderTarget rt, int srcRTindex, Vector4 srcRect, Vector4 destRect, BlitFramebufferFilter filter = Nearest)` | Copy render target color buffer to screen |
| `CopyTo((target, RTindex, Rect) src, (target, RTindex, Rect) dest, BlitFramebufferFilter filter = Nearest)` | Copy color buffer between render targets |

### Creating Render Targets

```csharp
// Simple render target with default 1-texture + depth
var rt = Graphics.BuildRenderTarget()
    .AddTexture()
    .AddDepthBuffer()
    .Build();

var rt = Graphics.NewRenderTarget();  // shortcut for the above

// Custom render target with named texture and specific settings
var rt = Graphics.BuildRenderTarget()
    .SetName("post-process-rt")
    .AddTexture(Graphics.BuildTargetTexture()
        .SetFormat(GLEnum.Rgba8)
        .SetWrapS(GLEnum.ClampToEdge)
        .SetMinFilter(GLEnum.Linear)
        .SetStatic(new Vector2(1024, 1024)))
    .SetDepthBuffer(new DepthBuffer())
    .Build();

// Dynamic size (follows window scaling)
var rt = Graphics.BuildRenderTarget()
    .SetName("half-res-rt")
    .AddTexture(Graphics.BuildTargetTexture()
        .SetResolutionMultiplier(new Vector2(0.5f, 0.5f)))
    .AddDepthBuffer()
    .Build();
```

### Depth Buffer

```csharp
new DepthBuffer()                              // Window-sized, Depth24Stencil8
new DepthBuffer(GLEnum.Depth24Stencil8)
new DepthBuffer(new Vector2(1024, 1024))  // Fixed size
```

### Using Render Targets

```csharp
Graphics.SetRenderToTarget(rt);
// ... draw to rt ...

Graphics.SetRenderToScreen();  // back to the internal _sceneRT
```

### Finding Render Targets

```csharp
if (Graphics.TryFindByName("post-process-rt", out var target))
{
    // Use target
}
```

### Copying Between Targets

```csharp
// Copy RT color buffer to screen
Graphics.CopyToScreen(rt, 0,
    new Vector4(0, 0, rt.Width, rt.Height),
    new Vector4(0, 0, 1920, 1080));

// Copy between two render targets
Graphics.CopyTo(
    (sourceRT, 0, new Vector4(0, 0, 512, 512)),
    (destRT,   0, new Vector4(0, 0, 512, 512)));
```

## FullScreenQuad

A single quad from -1 to 1 with UV coordinates 0→1. Used for post-processing:

```csharp
Graphics.FullScreenQuad.Draw();
```

## CommonUBO

A Uniform Buffer Object at binding point 0 updated every frame with camera and timing data:

```glsl
layout(std140) uniform CommonData {
    mat4 sView;
    mat4 sProjection;
    float sTime;
    float sDeltaTime;
};
```

```csharp
shader.AttachUBO(Game.CommonUboHandle, "CommonData", binding: 0);
```

## RenderViewport

Controls the scale of render targets relative to the window:

```csharp
Graphics.RenderViewport.Scale = new Vector2(0.5f, 0.5f);  // Half resolution
```
