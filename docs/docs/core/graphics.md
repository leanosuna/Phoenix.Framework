# Graphics & Rendering

The `Graphics` class manages OpenGL state, render targets, and the rendering pipeline.

## GL State Management

`Graphics` tracks previous state to avoid redundant GL calls. All methods are idempotent.

### Depth Testing

```csharp
Graphics.SetDepthTest(true);
Graphics.SetDepthTest(true, GLEnum.Less);  // with custom compare function
```

### Alpha Blending

```csharp
Graphics.SetAlphaBlend(true);
Graphics.SetAlphaBlend(true,
    BlendingFactor.SrcAlpha,
    BlendingFactor.OneMinusSrcAlpha);
```

### Face Culling

```csharp
Graphics.SetFaceCulling(true);
Graphics.SetFaceCulling(true, GLEnum.Back, frontIsCcw: true);
```

### Clearing and Viewport

```csharp
Graphics.SetClearColor(new Vector4(0.1f, 0.15f, 0.2f, 1f));
Graphics.ClearRenderTarget();
Graphics.ClearRenderTarget(true, true, false);  // color + depth
```

### Accessors

| Property | Type | Description |
|----------|------|-------------|
| `Time` | `double` | Total elapsed time (set internally) |
| `FPS` | `double` | Instantaneous FPS |
| `FPS_SAMPLE` | `double` | Averaged FPS over `FPS_SAMPLE_RATE` (default 0.3s) |
| `FT_SAMPLE` | `double` | Frame time sample |
| `RenderHaltKey` | `Key` | Key that toggles render halt (default `Key.F11`) |

## Render Targets

Render targets redirect rendering to an off-screen framebuffer. The framework provides fluent builders.

### Creating a Render Target

```csharp
// Simple render target with default 1-texture + depth
var rt = Graphics.BuildRenderTarget()
    .AddTexture()
    .AddDepthBuffer()
    .Build();

// Custom render target with named texture and specific settings
var rt = Graphics.BuildRenderTarget()
    .SetName("post-process-rt")
    .AddTexture(RTManager.BuildRTT()
        .SetFormat(Silk.NET.OpenGL.GLEnum.Rgba8)
        .SetWrapS(Silk.NET.OpenGL.GLEnum.ClampToEdge)
        .SetMinFilter(Silk.NET.OpenGL.GLEnum.Linear)
        .SetStatic(new Silk.NET.Maths.Vector2(1024, 1024)))
    .SetDepthBuffer(new DepthBuffer())
    .Build();

// Dynamic size (follows window scaling)
var rt = Graphics.BuildRenderTarget()
    .SetName("half-res-rt")
    .AddTexture(RTManager.BuildRTT()
        .SetResolutionMultiplier(new Silk.NET.Maths.Vector2(0.5f, 0.5f)))
    .AddDepthBuffer()
    .Build();
```

### Depth Buffer

```csharp
new DepthBuffer()                              // Window-sized, Depth24Stencil8
new DepthBuffer(Silk.NET.OpenGL.GLEnum.Depth24Stencil8)
new DepthBuffer(new Silk.NET.Maths.Vector2(1024, 1024))  // Fixed size
```

### Using Render Targets

```csharp
// Redirect rendering to a target
Graphics.SetRenderToTarget(rt);
// ... draw to rt ...

// Return to screen
Graphics.SetRenderToScreen();
```

### Rendering to Screen

The scene is always rendered to an internal `_sceneRT` each frame. `TrueRenderToScreen()` blits it to the window framebuffer:

```csharp
// Blit a specific render target to screen
Graphics.CopyToScreen(rt, srcRTIndex: 0,
    new Vector4(0, 0, 1, 1),   // source rect
    new Vector4(0, 0, 1, 1),   // destination rect
    Silk.NET.OpenGL.GLEnum.Nearest);
```

### Finding Render Targets

```csharp
if (Graphics.TryFindByName("post-process-rt", out var target))
{
    // Use target
}

var target = Graphics.FindByName("post-process-rt");  // throws if not found
```

## FullScreenQuad

A single quad from -1 to 1 with UV coordinates 0→1. Used for post-processing:

```csharp
// Set your post-processing shader as current
shader.SetAsCurrentGLProgram();
Graphics.FullScreenQuad.Draw();  // Draws the quad
```

## CommonUBO

A single Uniform Buffer Object (binding point 0) updated every frame with camera and timing data:

```csharp
// GLSL side
layout(std140) uniform CommonData {
    mat4 sView;           // layout(location = 0)
    mat4 sProjection;     // layout(location = 1)
    float sTime;          // layout(location = 2)
    float sDeltaTime;     // layout(location = 3)
};
```

```csharp
// C# side — the UBO is created and updated automatically by PhoenixGame.
// Bind it in your shader:
shader.AttachUBO(Game.CommonUboHandle, "CommonData", binding: 0);
```

## RenderViewport

Controls the scale of render targets relative to the window:

```csharp
Graphics.RenderViewport.Scale = new Vector2(0.5f, 0.5f);  // Half resolution
```
