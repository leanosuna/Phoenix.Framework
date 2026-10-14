# Game loop

Namespace: `Phoenix.Framework`

`PhoenixGame` is the abstract base class for all Phoenix applications. It initializes the Silk.NET window, coordinates the Vulkan 1.3 rendering pipeline, processes inputs and asset reloads, and invokes user callbacks.

---

## Internal execution pipeline

Silk.NET drives the window loop and dispatches internal lifecycle events:

```
Run()
 └── Window.Create()
      └── Window.Load       → InternalLoad()
           ├── Log initialized
           ├── Graphics created (VulkanContext, VulkanSwapchain, CommonUBO, BindlessManager)
           ├── Input created
           ├── AssetLoader initialized
           ├── SoundManager initialized
           ├── Primitive factory initialized
           ├── Gizmos created
           ├── UI created (ImGui Vulkan overlay)
           ├── AssetIpcClient connected (127.0.0.1:28473)
           └── Initialize() called          ← user setup code
      └── Window.Update     → InternalUpdate()
           ├── Metrics.ProcessUpdate(dt)
           ├── Input.Update()
           ├── AssetIpcClient.Update()     ← drains reload events to main thread
           ├── Gizmos.Update() if enabled
           ├── UI.Update(dt)
           ├── Render halt check (F11)
           └── Update(dt)                  ← user game logic
      └── Window.Render     → InternalRender()
           ├── Metrics.ProcessRender(dt)
           ├── Swapchain.AcquireNextImage()
           ├── CommonUBO updated (camera matrices, time, resolution)
           ├── Swapchain.BeginCommandBuffer()
           ├── RenderContext prepared with active command buffer
           ├── If not halted:
           │    ├── Render(ctx, dt)        ← user rendering code
           │    └── Gizmos.Render(ctx) if enabled
           ├── Viewport blit or fallback clear pass executed
           ├── RenderUI()                  ← user overlay code
           ├── UI.Render(ctx)              ← ImGui draw commands
           └── Swapchain.SubmitAndPresent()
      └── Window.Closing    → InternalOnClose()
           ├── Graphics.WaitIdle()
           ├── Gizmos disposed
           ├── UI disposed
           ├── AssetIpcClient disconnected
           ├── AssetLoader.UnloadAll()
           ├── SoundManager shut down
           ├── OnClose() called            ← user cleanup code
           └── Graphics disposed
```

---

## Overridable methods

### Core lifecycle

| Method | Invocation | Description |
|---|---|---|
| `Initialize()` | Once at startup | Graphics and subsystems are ready. Load assets, build pipelines, and set up scene objects. |
| `Update(double deltaTime)` | Every frame | Process game logic, character physics, and camera controls. |
| `Render(RenderContext ctx, double deltaTime)` | Every frame | Record rendering commands into the provided `RenderContext`. |
| `RenderUI()` | Every frame | Draw ImGui controls or custom UI text overlays on top of the 3D scene. |
| `ConfigureWindow(ref WindowOptions options)` | Pre-startup | Modify window flags (dimensions, borders, title) before the OS window is created. |
| `OnWindowResize(Vector2 size)` | On window resize | Update projection aspect ratios or custom offscreen render targets. |
| `OnClose()` | Shutdown | Release application-specific native handles or resources before exit. |

### Asset reload callbacks

`PhoenixGame` listens to the asset server IPC socket and dispatches reload events onto the main thread:

| Method | Parameter | Description |
|---|---|---|
| `OnModelReloaded` | `ModelReloadEventArgs` | Fires when a 3D model asset has been recompiled by the asset tool. |
| `OnTextureReloaded` | `TextureReloadEventArgs` | Fires when a texture asset has been re-compressed or modified on disk. |
| `OnShaderReloaded` | `ShaderReloadEventArgs` | Fires when a SPIR-V shader has been recompiled. |

---

## Launch configuration (GameArguments)

`PhoenixGame` parses command-line arguments on startup:

```csharp
public class GameApp : PhoenixGame
{
    public GameApp(string[]? args) : base(args)
    {
    }
}
```

Pre-defined engine arguments:

- `--width <int>`: Sets window width in pixels.
- `--height <int>`: Sets window height in pixels.
- `--fullscreen`: Starts in fullscreen mode.
- `--vsync <bool>`: Enables or disables vertical synchronization.
- `--fps <int>`: Limits maximum updates and frames per second.
- `--backend <x11|wayland>`: Forces the windowing backend on Linux.
- `--validation`: Activates Vulkan validation layers (`VK_LAYER_KHRONOS_validation`).
- `--gpu <int>`: Selects physical GPU device index.
- `--asset-server <host:port>`: Specifies asset tool WebSocket address.

Read custom options using `Arguments.Get("option-name")` or `Arguments.Options`.

---

## Render halt (F11)

Pressing F11 toggles render halt. When halted:
- `Update()` and `Render()` stop executing.
- The UI layer stays alive and responsive, restoring mouse cursor mode so inspectors and menus remain interactive.
- Pressing F11 again resumes regular game loop execution.

---

## Properties

| Property | Type | Description |
|---|---|---|
| `Window` | `IWindow` | Underlying Silk.NET window instance |
| `WindowSize` | `Vector2` | Window client size in screen coordinates |
| `FramebufferSize` | `Vector2` | Current framebuffer size in pixels |
| `WindowWidth` / `WindowHeight` | `int` | Shortcuts for window dimensions |
| `FramebufferWidth` / `FramebufferHeight` | `int` | Shortcuts for framebuffer pixel dimensions |
| `Graphics` | `Graphics` | Graphics engine managing Vulkan devices, swapchain, and pipelines |
| `Input` | `Input` | Keyboard and mouse edge-detection helper |
| `Camera` | `Camera?` | Active camera whose matrices are bound to `CommonUBO` |
| `Metrics` | `Metrics` | Frame time, FPS, and UPS sampling counters |
| `UI` | `UI` | ImGui overlay and text drawing helper |
| `Gizmos` | `Gizmos` | Debug line, box, sphere, and frustum drawer |
| `AssetClient` | `AssetIpcClient?` | WebSocket client connected to the asset tool server |
| `Arguments` | `GameArguments` | Parsed command-line launch arguments |
