# UI & ImGui

Phoenix integrates ImGui for overlay rendering, plus a custom text-drawing system and centralized error reporting.

## UI System

The `UI` class is automatically created by `PhoenixGame`. Access it via `this.UI`.

### Drawing Text

```csharp
// Draw text at position
UI.DrawText("Hello World", new Vector2(100, 100), new Vector4(1, 1, 1, 1), size: 24);

// Centered text
UI.DrawCenteredText("Score: 100", new Vector2(200, 50), new Vector4(1, 1, 1, 1), 32);

// Horizontally centered (right edge at x)
UI.DrawHCenteredText("Centered", new Vector2(400, 50), new Vector4(1, 1, 1, 1), 20);

// Right-aligned (right edge at x)
UI.DrawRAlignedText("FPS: 60", new Vector2(800, 10), new Vector4(1, 1, 1, 1), 16);
```

### Drawing Images

```csharp
// Draw by asset name (resolves from AssetLoader)
UI.DrawImg("textures/ui-panel", new Vector2(0, 0), new Vector2(800, 600));

// With UV cropping
UI.DrawImg("textures/sprite-sheet",
    new Vector2(0, 0),       // screen position
    new Vector2(64, 64),     // screen size
    new Vector2(0, 0),       // UV min
    new Vector2(0.5f, 1f));  // UV max (draws right half only)

// From a GLTexture object
UI.DrawImg(myTexture, new Vector2(100, 100), new Vector2(128, 128));

// From raw OpenGL handle
UI.DrawImg(myTexture.Handle, new Vector2(256, 256),
    new Vector2(50, 50), new Vector2(64, 64),
    new Vector2(200, 200), new Vector2(100, 100));
```

### Simple Buttons

```csharp
UI.DrawSimpleButton("Click Me", new Vector2(100, 100), new Vector2(120, 40), () =>
{
    // Action on click
    Debug.Log("Button clicked!");
});
```

### Custom ImGui

Full ImGui is available through the `ImGuiController` behind the scenes. Use `RenderUI()` override:

```csharp
protected override void RenderUI()
{
    ImGui.Begin("Debug Panel");
    ImGui.Text($"FPS: {Graphics.FPS_SAMPLE:F0}");
    ImGui.Text($"Frame time: {Graphics.FT_SAMPLE * 1000:F1}ms");
    ImGui.Text($"Models loaded: {_models.Count}");
    ImGui.End();
}
```

### Font Management

Phoenix loads Cascadia Mono at sizes 10-100 in 5px steps by default. Load additional fonts:

```csharp
// Load a custom TTF font
UI.LoadFontTTF("fonts/myfont.ttf", new int[] { 16, 24, 32, 48 });

// Push a specific size
UI.SetFontSize(24);
UI.DrawText("Large text", new Vector2(100, 100), Vector4.One, 24);
```

## ErrorListWindow

Centralized error reporting from all framework subsystems. Errors are displayed as an overlay and logged.

### Showing Errors

Errors are automatically added by the framework (shader errors, audio errors, etc.). You can add your own:

```csharp
// Add with caller info (auto-filled by compiler attributes)
ErrorListWindow.Add("Something went wrong");

// Add with auto-expire (hidden after N seconds)
ErrorListWindow.Add("Temporary warning", showTimeSeconds: 5f);

// Toggle visibility
ErrorListWindow.Show = true;
```

### Error Display

Each error shows:
- **Message** — the error text
- **Count** — how many times it was added (deduplication)
- **Hover tooltip** — shows `[CallerFilePath]`, `[CallerLineNumber]`, `[CallerMemberName]`

### Example Error Output

```
⚠ Shader error: Could not find uniform "uEmissiveMap" in shader "shaders/pbr"
   at MyGame.Render() in C:\projects\MyGame\MyGame.cs:42
```

## UI Rendering Order

UI rendering happens after the main scene:

```
1. UI.Update(dt)           — ImGui controller update
2. SetRenderToScreen()     — Blit scene to window
3. ClearRenderTarget()     — Clear for overlay
4. UI.Render()             — ImGui + ErrorListWindow
5. RenderUI(dt)            — User's ImGui code
```

## See Also

- [Shaders](shaders.md) — for uniform and texture management
- [Logging](../utilities/logging.md) — ErrorListWindow and Log system
- [Core Game Loop](../core/game.md) — when RenderUI() is called
