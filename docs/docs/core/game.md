# PhoenixGame

`PhoenixGame` is the abstract base class for all Phoenix applications. It manages the window, input, rendering pipeline, and coordinates all subsystems.

## Understanding the internal pipeline

The windowing manager from Silk.NET fires internally InternalLoad(), InternalUpdate(), and InternalRender().  
These will handle setting up and updating all internal systems every frame in the background as shown below.

## Internal systems
```
Run()
 └── Window.Create()
      └── Window.Load       → InternalLoad()
           ├── InputManager created
           ├── UI created (ImGui initialized)
           ├── RTManager created
           ├── Scene render target created
           ├── Graphics created
           └── CommonUBO created
      └── Window.Update     → InternalUpdate()
           ├── Frame 0: DelayedLoad()
           │    ├── FullScreenQuad created
           │    ├── Gizmos created
           │    ├── SoundManager initialized
           │    ├── AssetLoader initialized
           │    └── Initialize() called
           │
           ├── Graphics.Time += dt
           ├── InputManager.Update()
           ├── Render halt check (F11)
           ├── Update(dt)          ← user code
           ├── CommonUBO updated
           └── Gizmos.Update() if enabled
      └── Window.Render     → InternalRender()
           ├── UI.Update(dt)
           ├── If not halted:
           │    ├── SetRenderToTarget(_sceneRT)
           │    ├── Render(dt)           ← user code
           │    └── Gizmos.Render() if enabled
           └── TrueRenderToScreen()
               ├── ClearRenderTarget()
               ├── UI.Render()           ← ImGui overlay
               └── RenderUI(dt)          ← user overlay code
```

## The internal scene target and RenderViewport
The pipeline is designed to have the game render to an internal scene render target.
This allows the UI layer of the game to render using the game window resolution, keeping the UI clean and pixel perfect, independent of the rendering framebuffer size which can be set to a different (lower) resolution at Graphics.RenderViewport.

## Rendering halt
This internal control allows the game to be halted at any time using the configured key (F11 by default) which stops excecuting the update and render methods but keeps the UI alive and responsive. Useful for debugging purposes.


## Important Notes
- The `CommonUBO` is updated every frame with `[View, Projection, Time, DeltaTime]` and bound at binding point 0.


## Properties

### Game Access

| Property | Type | Notes |
|----------|------|-------|
| `GL` | `GL` | Silk.NET OpenGL context (available after `Load`) |
| `Window` | `IWindow` | Silk.NET window handle |
| `WindowSize` | `Vector2` | Current window dimensions |
| `WindowWidth` | `int` | Width shortcut |
| `WindowHeight` | `int` | Height shortcut |

### System Access

| Property | Type | Notes |
|----------|------|-------|
| `InputManager` | `InputManager` | Read-only. Do not assign. |
| `FullScreenQuad` | `FullScreenQuad` | Read-only. Single quad for post-processing. |
| `Gizmos` | `Gizmos` | Read-only. Debug drawing. |
| `UI` | `UI` | Read-only. ImGui and text overlay. |
| `Camera` | `Camera` | Settable. Assign your camera instance in `Initialize()`. |
| `Graphics` | `Graphics` | Read-only after load. |
| `CommonUboHandle` | `uint` | UBO handle bound at binding point 0. |


