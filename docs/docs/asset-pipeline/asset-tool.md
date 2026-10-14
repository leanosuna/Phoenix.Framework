# Asset tool (pat)

`Phoenix.AssetTool` (invoked as `pat` or `dotnet pat`) is the companion tool for compiling source assets (FBX/OBJ models, PNG/JPG textures, GLSL shaders) into Phoenix's binary distribution format (`.contentbin/`).

---

## CLI commands

Run `pat` commands from your project root:

```bash
# Batch build all assets into .contentbin
dotnet pat build

# Force rebuild of all assets regardless of cache timestamps
dotnet pat build -force

# Watch Content/ folder, rebuild changes, and notify running game clients
dotnet pat watch

# Launch the Web Asset Studio UI and live IPC server on port 28473
dotnet pat web

# Run headless WebSocket IPC server and UDP discovery beacon
dotnet pat serve

# Clean the .contentbin folder
dotnet pat clean
```

---

## Output binary formats (.contentbin)

When `pat build` executes, it processes source assets into binary bundles:

| Input | Output format | Features |
|---|---|---|
| **Models** (FBX, OBJ, GLTF) | `.pmdl` | Geometry buffers, vertex attributes, bone hierarchy, animation tracks |
| **Textures** (PNG, JPG, TGA) | `.dds` | Precomputed mipmaps, BC1/BC3/BC5/BC7 block compression |
| **Shaders** (vert, frag, comp) | `.spv` | Compiled SPIR-V bytecode validated against Vulkan 1.3 target |

---

## Sidecar configuration files

Assets support optional JSON sidecars (`<assetname>.<ext>.json`) stored alongside source files in `Content/`:

### Model sidecar example (`sponza.fbx.json`)

```json
{
  "Scale": 0.01,
  "CalculateTangents": true,
  "FlipUV": false,
  "SwapYZ": false
}
```

### Texture sidecar example (`normal.png.json`)

```json
{
  "CompressionFormat": "BC5",
  "GenerateMipmaps": true,
  "Filter": "Linear",
  "Wrap": "Repeat"
}
```

The tool bakes sidecar settings directly into the binary output files during compilation.

---

## Live hot reloading workflow

1. Start your asset server or watch process:
   ```bash
   dotnet pat watch
   ```
2. In a second terminal, launch your game:
   ```bash
   dotnet run
   ```
3. When you modify an asset or tweak a sidecar JSON file:
   - `pat watch` detects the change and recompiles the binary.
   - The tool server broadcasts an `AssetReloadMessage` over WebSocket (port 28473).
   - The game's `AssetIpcClient` intercepts the message and fires `OnModelReloaded`, `OnTextureReloaded`, or `OnShaderReloaded`.
   - Your scene updates live without restarting the game.
