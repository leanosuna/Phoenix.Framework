---
name: phoenix-rendering
description: Shaders, textures, models & animation, primitives, UI/ImGui, and gizmos for games built on Phoenix.Framework. Use when loading GLSL shaders, binary textures/models, drawing debug gizmos, rendering ImGui text/images, or creating primitive geometry.
license: MIT
compatibility: opencode, claude, agents
metadata:
  package: Phoenix.Framework
  version: "1.0.3"
---

# Phoenix.Framework — Rendering

Rendering is driven from `PhoenixGame.Render()`. Assets are compiled to binary by the AssetTool (`pat`) and loaded via `AssetLoader` (must `Init` first).

## Key types

- Shaders: `ShaderHelper` (abstract; generated per-shader child), `ShaderUniform<T>` / `ShaderTextureUniform` (call `.Set(...)`), `GLShader` (raw: `SetUniform`, `AttachUBO`, `Use`).
- Textures: `GLTexture` (`Name`, `Handle`, `Size`, `Format`, `MipCount`, `Bind`, `Resize`, `Dispose`), `GLTextureCube`.
- Models: `Model` (`Parts`, `TextureNames`), `AnimatedModel` (`SetAnimation`, `SetAnimationBlend`, `Update(float)`, `FinalBoneMatrices`), `ModelMesh.Draw()`.
- Primitives: `Primitive` + `Cube`/`Sphere`/`Cylinder`/`Plane`/`Pyramid`/`Frustum` via `Create(...)`; requires `Primitive.SetGL(gl)` first.
- UI: `UI.DrawText/DrawCenteredText/DrawHCenteredText/DrawRAlignedText`, `DrawImg`, `DrawSimpleButton`, `LoadFontTTF`, `SetFontSize`. Font pushes are auto-balanced.
- Gizmos: `Gizmos.AddLine/AddCube/AddSphere/AddCylinder/AddPlane/AddCapsule/AddVolume/AddAxisLines`, `Enabled`.
- `CommonUBO` at binding point 0 (`View, Projection, Time, DeltaTime`) — attach via `shader.AttachUBO(Game.CommonUboHandle, "CommonData")`.

## Gotchas

- Load animated models with `AssetLoader.LoadAnimatedModel(name)`.
- `Mesh<T>` index types are `uint`/`ushort`/`byte` only.
- `Primitive` vertex layout combos must be Pos, PosUv, PosUvNorm, or PosUvNormTanBt — others throw.

## Details

See the website docs:
- Shaders: https://framework.nx.net.ar/rendering/shaders/
- Textures: https://framework.nx.net.ar/rendering/textures/
- Models & Animation: https://framework.nx.net.ar/rendering/models/
- Primitives: https://framework.nx.net.ar/rendering/primitives/
- UI & ImGui: https://framework.nx.net.ar/rendering/ui/
- Gizmos: https://framework.nx.net.ar/rendering/gizmos/
