# Phoenix Framework

A lightweight 3D game framework built on **Silk.NET** and **Vulkan 1.3** for C# on .NET 10.

## What is Phoenix?

Phoenix provides the foundational systems needed to build a 3D game:

The main game loop, a modern Vulkan 1.3 dynamic rendering pipeline, cameras, input, OpenAL audio, Box3D rigid-body physics, geometric collision queries, and an asset pipeline with binary distribution bundles and live hot reloading.

It targets .NET 10 and runs on Windows, macOS (via MoltenVK), and Linux (Wayland and X11).

## Core features

| System | Status | Description |
|---|---|---|
| Game window | Stable | Silk.NET window with Wayland and X11 platform selection, VSync, and dynamic resize handling |
| Vulkan 1.3 core | Stable | Dynamic rendering, Synchronization2, double-buffered swapchain, per-frame inflight fences |
| Rendering context | Stable | Scoped dynamic passes, attachment layout transitions, viewport scaling, swapchain blitting |
| Bindless textures | Stable | Global descriptor array indexing up to 4096 or 16384 textures directly through 80-byte push constants |
| Shaders & pipelines | Stable | SPIR-V graphics and compute pipelines with standardized CommonUBO, bone, and metadata descriptor sets |
| Geometry & MDI | Stable | Standard models and Multi-Draw Indirect packing via `IndirectMeshBuilder` and `IndirectMeshRenderer` |
| Skeletal animation | Stable | Bone hierarchy parsing, keyframe sampling, FBX dummy node folding, and Set 2 bone uniform buffers |
| Render targets | Stable | Offscreen color and depth targets, G-buffers, MSAA, and post-processing blit operations |
| Input | Stable | Keyboard and mouse via Silk.NET, edge-detected key queries, cursor lock modes |
| Camera | Stable | Abstract camera hierarchy with `FreeCamera` (WASD movement and mouse aim) |
| UI (ImGui) | Stable | Immediate-mode ImGui Vulkan overlay, custom fonts, text rendering, debug windows |
| Gizmos | Stable | Line-based debug drawing: lines, rays, cubes, spheres, capsules, frustums, coordinate frames |
| Physics & collision | Stable | Box3D.NET rigid-body dynamics, gravity, sub-stepping, raycasts, and standalone geometric query volumes |
| Audio | Stable | OpenAL audio playback with 3D spatial positioning, volume control, and streaming |
| Asset pipeline | Stable | Standalone `pat` CLI tool, `.contentbin` binary bundles, JSON sidecars, and live WebSocket hot reloading |

## Architecture overview

```
PhoenixGame
 ├── Arguments ─────► CLI options (window dimensions, VSync, platform backend)
 ├── Graphics ──────► VulkanContext, VulkanSwapchain, CommonUBO, BindlessManager
 │    └── RenderContext ──► Scoped dynamic passes, pipeline binding, MDI, blitting
 ├── Input ─────────► Keyboard, mouse, edge-detected press queries
 ├── Camera ────────► View and projection matrices bound into CommonUBO
 ├── Gizmos ────────► Debug line rendering
 ├── UI ────────────► ImGui Vulkan overlay
 ├── SoundManager ──► OpenAL audio playback and 3D voices
 ├── PhysicsWorld ──► Box3D rigid-body simulation and collision queries
 ├── AssetLoader ───► .contentbin binary bundles, cached resolution
 └── AssetClient ───► WebSocket IPC client for live asset hot reloading
```

## Documentation sections

- [Getting started](getting-started.md): Project setup, asset tool workflow, and first game
- [Core systems](core/game.md): Game loop lifecycle, Vulkan graphics, camera, input
- [Rendering](rendering/shaders.md): Shaders, bindless textures, models, MDI, render targets, UI, gizmos
- [Collision and physics](collisions/overview.md): Box3D physics simulation and geometric collision volumes
- [Audio](audio/sound.md): Sound playback, sound instances, spatialization
- [Asset pipeline](asset-pipeline/loading.md): Loading assets, the `pat` tool, binary distribution, hot reloading
- [Utilities](math/math.md): Math helpers, structured logging, frame metrics
