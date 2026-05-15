# PhoenixGame

`PhoenixGame` is the abstract base class for all Phoenix applications. It manages the window, input, rendering pipeline, and coordinates all subsystems.

## Creating a Game

Inherit from `PhoenixGame` and implement the three required methods:

```csharp
public class MyGame : PhoenixGame
{
    protected override void Initialize()
    {
        // Called once after GL context is ready, before the first frame
    }

    protected override void Update(double deltaTime)
    {
        // Called every frame — game logic, input, camera movement, etc.
    }

    protected override void Render(double deltaTime)
    {
        // Called every frame — draw your scene
    }
}
```

## Entry Point

```csharp
public static class Program
{
    public static void Main()
    {
        using var game = new MyGame();
        game.Run();  // Blocking call — runs until Window.Close()
    }
}
```

## Lifecycle

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

## Optional Overrides

### `RenderUI()`

Called after the main scene is rendered and the screen is cleared, but before ImGui. Use this for custom ImGui rendering.

```csharp
protected override void RenderUI()
{
    ImGui.Begin("Debug Info");
    ImGui.Text($"FPS: {Graphics.FPS_SAMPLE:F0}");
    ImGui.Text($"Frame time: {Graphics.FT_SAMPLE:F3}ms");
    ImGui.End();
}
```

### `OnWindowResize()`

Called when the window is resized.

```csharp
protected override void OnWindowResize(Vector2D<int> windowSize)
{
    base.OnWindowResize(windowSize);
    Camera.AspectRatio = WindowWidth / (float)WindowHeight;
}
```

### `OnClose()`

Called when the user closes the window.

```csharp
protected override void OnClose()
{
    // Cleanup resources
    base.OnClose();
}
```

### `InitialLoadScreen()`

Called during the very first frame, before `Initialize()` completes. Use this to show a loading screen.

## Important Notes

- `InputManager`, `FullScreenQuad`, `Gizmos`, and `UI` are **read-only**. Do not assign them — they are created automatically.
- `Camera` must be assigned in `Initialize()` or later. Before that, it is `default!`.
- The `CommonUBO` is updated every frame with `[View, Projection, Time, DeltaTime]` and bound at binding point 0.
- Press **F11** to toggle render halt (useful for debugging game logic without rendering).
- `Initialize()` is called once on the first frame after `DelayedLoad()` completes.
