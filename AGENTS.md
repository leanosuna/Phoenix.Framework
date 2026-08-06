# Phoenix Framework — Agent Guide

Local development guide for the Phoenix.Framework repo. For per-system API details, see the **local docs** (`docs/docs/`) — the same content is published to https://framework.nx.net.ar/.

## Solution Overview

Lightweight 3D game framework on **Silk.NET + OpenGL** for C# / .NET 10.0. Provides rendering, audio, input, collision, and asset loading.

| Project | Path |
|---|---|
| `Phoenix.Framework` | `Phoenix.Framework/Phoenix.Framework.csproj` — core library (NuGet `Phoenix.Framework`) |

### Key Stack

- **Language:** C# / .NET 10.0
- **Silk.NET** 2.23.0 (OpenGL, Windowing, Input, Maths, OpenAL)
- **Networking:** RiptideNetworking.Riptide 2.2.1
- **Textures:** SixLabors.ImageSharp 3.1.11
- **Docs:** MkDocs (material) — `docs/`

### Dev Commands

```bash
dotnet build                        # Build (Debug)
dotnet build -c Release             # Build Release
dotnet pack -c Release              # Pack to bin/Release
../pack-framework.sh                # Clean Release build + pack to local-nuget + clear cache
cd docs && mkdocs serve             # Serve docs locally (global mkdocs install)
```

### Solution Layout

```
Phoenix.Framework/
├── Phoenix.Framework/              # Core library source
│   ├── AssetImport/                # Runtime asset loading (AssetLoader, manifest)
│   ├── Cameras/                    # Camera hierarchy (BaseCamera, MouseCamera, FreeCamera)
│   ├── Collisions/                 # Volumes, RayCasts, Collision manifolds, Serializable volumes
│   ├── FileHelpers/                # Log, EmbeddedHelper
│   ├── Input/                      # Input class (keyboard/mouse)
│   ├── Maths/                      # MathHelper, MathEx
│   ├── Network/                    # NetworkManager (INCOMPLETE — not production-ready)
│   ├── Rendering/                  # GL engine: Geometry, Gizmos, GUI, Primitives, RT, Shaders, Textures
│   ├── Sound/                      # SoundManager, SoundClip, SoundInstance, decoders
│   ├── DisplayInfo.cs              # Display/refresh-rate metadata
│   └── PhoenixGame.cs              # Abstract base game class
├── docs/                           # MkDocs site (source of truth)
├── skills/                         # Shareable per-system skills (see below)
├── AGENTS.md                       # This file
└── SKILLS.md                       # REMOVED — replaced by skills/
```

## Docs

The **hand-written** docs live in `docs/docs/` and are pushed to https://framework.nx.net.ar/ (Cloudflare Pages). Use the local files for reference:

| System | Local doc |
|---|---|
| Game loop / PhoenixGame | `docs/docs/core/game.md` |
| Graphics & Rendering | `docs/docs/core/graphics.md` |
| Camera | `docs/docs/core/camera.md` |
| Input | `docs/docs/core/input.md` |
| Shaders | `docs/docs/rendering/shaders.md` |
| Textures | `docs/docs/rendering/textures.md` |
| Models & Animation | `docs/docs/rendering/models.md` |
| Primitives | `docs/docs/rendering/primitives.md` |
| UI & ImGui | `docs/docs/rendering/ui.md` |
| Gizmos | `docs/docs/rendering/gizmos.md` |
| Collision overview | `docs/docs/collisions/overview.md` |
| Ray casting / Ray casts | `docs/docs/collisions/ray.md`, `raycasts.md` |
| Spheres / Frustum-Box | `docs/docs/collisions/spheres.md`, `frustum-box.md` |
| Manifolds / Serializable | `docs/docs/collisions/manifold.md`, `serializable.md` |
| Audio | `docs/docs/audio/sound.md` |
| Asset loading | `docs/docs/asset-pipeline/loading.md` |
| Math helpers | `docs/docs/math/math.md` |
| Logging | `docs/docs/utilities/logging.md` |

When docs change, keep `docs/mkdocs.yml` nav in sync and verify with `mkdocs build --strict`.

## Skills

Per-system skills (authoritative copies in `skills/`, also installed to `~/.claude/skills/` and `~/.agents/skills/` for use in game repos):

- `phoenix-core` — game loop, graphics, camera, input
- `phoenix-rendering` — shaders, textures, models, primitives, UI, gizmos
- `phoenix-collision` — volumes, raycasts, manifolds, serializable volumes
- `phoenix-audio` — sound playback and decoders
- `phoenix-utilities` — math, logging

Each skill is a compact pointer (key types + website links) — load the relevant one rather than pulling API details into context.

## Notes

- `Input` is the class name — never write `InputManager`.
- `NetworkManager` is not fully implemented; don't build on it.
- Version bumps: update `<Version>` in `Phoenix.Framework.csproj`, the pack scripts' cache paths, and `SKILLS`/`AGENTS` version notes together.
