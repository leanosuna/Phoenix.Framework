# Phoenix Framework

A lightweight 3D game framework built on **Silk.NET** and **OpenGL** for C#.

## What is Phoenix?

Phoenix provides the foundational systems needed to build a 3D game: a game loop, rendering pipeline, camera, input, audio, collision detection, and an asset pipeline with binary model/texture loading. It targets .NET 8+ and runs on Windows, macOS, and Linux.

## Quick Start

```csharp
using Phoenix.Framework;
using Silk.NET.Input;
using Silk.NET.Maths;

public class MyGame : PhoenixGame
{
    protected override void Initialize()
    {
        Camera = new FreeCamera(
            this,
            Vector3.Zero,
            0, -MathF.PI / 4, MathF.PI / 4,
            0.1f, 1000f,
            WindowWidth / (float)WindowHeight
        );
        Camera.SetMoveKeys(Key.W, Key.S, Key.A, Key.D, Key.E, Key.Q, Key.LeftShift, 15f);
        Graphics.SetDepthTest(true);
        Graphics.SetFaceCulling(true);
    }

    protected override void Update(double dt)
    {
        Camera.Update((float)dt);
    }

    protected override void Render(double dt)
    {
        // Your rendering code
    }

    protected override void RenderUI()
    {
        // ImGui overlay
    }
}

public static class Program
{
    public static void Main()
    {
        using var game = new MyGame();
        game.Run();
    }
}
```

## Core Features

| System | Status | Description |
|--------|--------|-------------|
| Game Window | Stable | Silk.NET window with VSync and resize handling |
| Input | Stable | Keyboard + mouse via Silk.NET, edge-detect key presses |
| Camera | Stable | Abstract camera hierarchy with `FreeCamera` (WASD + mouse) |
| Shaders | Stable | GLSL vertex/fragment compilation, uniform management, UBO support |
| Textures | Stable | Binary texture loading with BC1/BC3/BC5 compression support |
| Render Targets | Stable | Fluent builder API, dynamic sizing, framebuffer blitting |
| Models | Stable | Assimp-powered binary format, skeleton animation, bone blending |
| UI (ImGui) | Stable | Overlay rendering, custom fonts, image drawing |
| Gizmos | Stable | Line-based debug drawing: lines, cubes, spheres, frustums |
| Collision | Stable | Ray casting, bounding spheres, frustums, AABB/OBB, cylinders |
| Audio | Stable | OpenAL-based, pluggable decoders (WAV built-in) |
| Asset Pipeline | Stable | JSON manifest-based loading, cached asset resolution |
| Networking | Planned | Not yet implemented |

## Architecture Overview

```
PhoenixGame
 ├── Graphics ──────► RenderTargets, FullScreenQuad, CommonUBO
 ├── InputManager ──► Keyboard, Mouse, Edge-detect
 ├── Camera ────────► View/Projection matrices
 ├── Gizmos ────────► Debug line rendering
 ├── UI ────────────► ImGui overlay + ErrorListWindow
 ├── SoundManager ──► OpenAL audio playback
 └── AssetLoader ───► Cached models, textures, shaders
```

All systems are wired into the `PhoenixGame` lifecycle:

1. **Load** — GL context ready. `InputManager`, `UI`, `RTManager`, `Graphics` created.
2. **Update** — `DelayedLoad` fires once (creates `FullScreenQuad`, `Gizmos`, `SoundManager`), then calls `Initialize()`. Each frame: `Graphics.Time` advances, `InputManager.Update()`, user `Update(dt)`.
3. **Render** — Renders to `_sceneRT`, then blits to screen. UI renders on top.

## See Also

- [Getting Started](getting-started.md) — project setup and first game
- [Core Systems](core/game.md) — game loop, graphics, camera, input
- [Rendering](rendering/shaders.md) — shaders, textures, models, UI, gizmos
- [Collision System](collisions/overview.md) — volumes and intersection tests
- [Audio](audio/sound.md) — sound playback and decoders
- [Asset Pipeline](asset-pipeline/loading.md) — loading assets at runtime
