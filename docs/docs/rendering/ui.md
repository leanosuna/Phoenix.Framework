# UI and ImGui

Namespace: `Phoenix.Framework.Rendering.GUI`

Phoenix provides an immediate-mode ImGui Vulkan overlay, helper methods for text drawing, and an in-engine error reporting window.

---

## The UI helper

`PhoenixGame.UI` manages text rendering and standard widgets during `RenderUI()`.

### Drawing text

```csharp
// Standard text at pixel position
UI.DrawText("Hello World", new Vector2(100, 100), new Vector4(1, 1, 1, 1), size: 24);

// Centered on a point
UI.DrawCenteredText("Score: 100", new Vector2(200, 50), new Vector4(1, 1, 1, 1), 32);

// Horizontally centered
UI.DrawHCenteredText("Centered Header", new Vector2(400, 50), new Vector4(1, 1, 1, 1), 20);

// Right-aligned
UI.DrawRAlignedText("FPS: 60", new Vector2(800, 10), new Vector4(1, 1, 1, 1), 16);
```

### Drawing images

```csharp
// Draw by asset path (resolves from AssetLoader)
UI.DrawImg("Textures/hud_frame.png", new Vector2(0, 0), new Vector2(800, 600));

// Draw with UV cropping
UI.DrawImg(
    "Textures/sprite_sheet.png",
    position: new Vector2(10, 10),
    size: new Vector2(64, 64),
    uvMin: new Vector2(0, 0),
    uvMax: new Vector2(0.5f, 1f)
);

// Draw directly from a VulkanTexture
UI.DrawImg(
    myTexture,
    position: new Vector2(100, 100),
    size: new Vector2(128, 128),
    uvMin: Vector2.Zero,
    uvMax: Vector2.One
);
```

### Simple buttons

```csharp
UI.DrawSimpleButton("Click Me", new Vector2(100, 100), new Vector2(120, 40), () =>
{
    Log.Info("Button clicked!");
});
```

---

## Custom ImGui windows

Inside `RenderUI()`, you have full access to `ImGuiNET.ImGui`:

```csharp
protected override void RenderUI()
{
    ImGui.Begin("Debug Inspector");
    ImGui.Text($"FPS: {Metrics.FPS_SAMPLE:F0}");
    ImGui.Text($"Frame time: {Metrics.FT_SAMPLE:F1}ms");
    ImGui.Text($"Camera position: {Camera?.Position}");
    ImGui.Checkbox("Show Gizmos", ref _showGizmos);
    ImGui.End();
}
```

---

## Font management

Phoenix loads Cascadia Mono across sizes 10 to 100 in 1px increments by default. You can load custom TrueType fonts:

```csharp
UI.LoadFontTTF("Fonts/Roboto-Regular.ttf", new int[] { 16, 24, 32, 48 });
UI.SetFontSize(24);
```

---

## ErrorListWindow

`ErrorListWindow` displays non-fatal engine and user warnings as an in-game overlay:

```csharp
ErrorListWindow.Add("Asset failed to compile, falling back to cached version");
ErrorListWindow.Add("Low memory warning", showTimeSeconds: 5f);
ErrorListWindow.Show = true;
```

When render halt (F11) triggers, `ErrorListWindow` and ImGui remain active for inspection.
