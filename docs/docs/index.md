# Phoenix Framework

A lightweight 3D game framework built on **Silk.NET** and **OpenGL** for C#.

## What is Phoenix?

Phoenix provides the foundational systems needed to build a 3D game:

The main game loop, cameras, input, audio, collision detection, and an asset pipeline with binary model/texture loading.

It targets .NET 10 and runs on Windows, macOS, and Linux.

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

## See Also

- [Getting Started](getting-started.md) — project setup and first game
- [Core Systems](core/game.md) — game loop, graphics, camera, input
- [Rendering](rendering/shaders.md) — shaders, textures, models, UI, gizmos
- [Collision System](collisions/overview.md) — volumes and intersection tests
- [Audio](audio/sound.md) — sound playback and decoders
- [Asset Pipeline](asset-pipeline/loading.md) — loading assets at runtime
