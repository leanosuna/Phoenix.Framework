---
name: phoenix-core
description: Core game loop, graphics, camera, and input for games built on Phoenix.Framework (net10.0, NuGet package Phoenix.Framework). Use when writing or debugging PhoenixGame lifecycle, Graphics window/state calls, FreeCamera setup, or Input polling.
license: MIT
compatibility: opencode, claude, agents
metadata:
  package: Phoenix.Framework
  version: "1.0.3"
---

# Phoenix.Framework — Core Systems

Phoenix is a lightweight 3D game framework on Silk.NET + OpenGL (net10.0). A game subclasses `PhoenixGame`, overrides `Initialize()`, `Update(double dt)`, `Render(double dt)`.

## Key types

- `PhoenixGame` — abstract base. Override `Initialize()`, `Update(double)`, `Render(double)`, optional `RenderUI()`, `OnWindowResize(Vector2)`, `OnClose()`. Call `Run()`/`Stop()`.
- `PhoenixGame.Input` — keyboard/mouse. NOT `InputManager`.
- `PhoenixGame.Graphics` — window control + GL state + render targets.
- `PhoenixGame.Camera` — assign your camera in `Initialize()`.
- `PhoenixGame.UI` / `Gizmos` / `FullScreenQuad` / `CommonUboHandle`.
- `FreeCamera` (→ `MouseCamera` → `BaseCamera` → `Camera`) — ctor `(game, position, yaw, pitch, fov, nearPlane, farPlane, aspectRatio)`; `SetMoveKeys(...)`, `SetPitchYawKeys(...)`, `Update(double)`.
- `Input` — `KeyDown/KeyDownOnce`, `MouseDown/MouseDownOnce`, `MouseLeft/RightDown(Once)`, `MouseDelta`, `MouseWheelPrecise` (float, accumulates), `MouseWheelValue` (int, whole notches), `SetMouseMode/ToggleMouseMode`, `GetContext()`.

## Gotchas

- `MouseSensitivity` lives on `Input`, not the camera.
- Cameras update with `double`; `Graphics` state setters (`SetDepthTest`, `SetAlphaBlend`, …) are idempotent.
- `NetworkManager` exists but is not fully implemented.

## Details

See the website docs (pushed from this repo's `docs/docs/`):
- Game loop: https://framework.nx.net.ar/core/game/
- Graphics & Rendering: https://framework.nx.net.ar/core/graphics/
- Camera: https://framework.nx.net.ar/core/camera/
- Input: https://framework.nx.net.ar/core/input/
- Getting started: https://framework.nx.net.ar/getting-started/
