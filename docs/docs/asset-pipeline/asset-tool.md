# AssetTool

`Phoenix.AssetTool` is a companion tool for compiling source assets (FBX models, PNG textures, GLSL shaders) into Phoenix's binary format.

## Building Assets

### CLI Commands

```bash
# Initialize an empty manifest, -force to replace
dotnet pat Content/asset-manifest.json init

# Start the AssetTool GUI
dotnet pat Content/asset-manifest.json gui

# List all of the files in the manifest
dotnet pat Content/asset-manifest.json list

# Build all of the files in the manifest
dotnet pat Content/asset-manifest.json build

# Automatically track files in the manifest and rebuild changes
dotnet pat Content/asset-manifest.json auto
```

### GUI

The GUI provides:  
- **File Selector** — Opens native file explorer window to select assets  
- **Asset Browser** — Browse and preview assets  
- **Build Panel** — Build selected assets, see real-time progress  
- **Options (right panel)** — Configure model export settings, texture compression  
- **Build Error (right panel)** — Hovering on a item that failed build shows a pop-up 
with the error log, click it to open on the right panel

### GUI Notes:
- You can select on the top left if you desire light or dark theme.  
- You can resize the font by clicking the buttons in the top left, or by Ctrl+MouseWheel Up/Down.  
- You can add/remove items from build directly from the asset browser.
- You can resize the left and right panels.
- In the options for an asset you can run a build process for that item only.

## Supported Formats

### Models

| Input | Output | Features |
|-------|--------|----------|
| FBX, OBJ, GLTF, 3DS, DAE, BLEND | `.bin` | Static meshes, skeleton animation, UVs, normals, tangents, bone weights |

### Textures

| Input | Output | Compression |
|-------|--------|-------------|
| PNG, JPG, BMP, TGA | `.bin` | BC1 (DXT1), BC3 (DXT5), BC5 (RGTC2), RGBA8 (uncompressed) |

### Shaders

| Input | Output |
|-------|--------|
| `.glsl`, `.vert`, `.frag` | `.glsl`, `.vert`, `.frag` (copied as-is) |

Shaders are not converted into bin files, they are passed through unchanged.  
They are, however compiled an verified for errors at build time.

## Build Options

### Model Load Options

| Option | Default | Description |
|--------|---------|-------------|
| `IsAnimated` | `false` | Is an animated model, extract animations |
| `Extract Textures` | `true` | Contains embedded textures, extract them |
| `Assimp Flags` | `per option default` | Postprocessing Flags for assimp |

### Texture Load Options

| Option | Default | Description |
|--------|---------|-------------|
| `Generate MipMaps` | `true` | Generate mipmaps for this texture |
| `Compression` | `BC3` | Target compression format |
| `Wrap Horizontal` | `repeat` | WrapS |
| `Wrap Vertical` | `repeat` | WrapT |
| `Min filter` | `Linear Mipmap Linear` | Interpolation filter min|
| `Mag filter` | `Linear` | Interpolation filter mag|


## See Also

- [Loading Assets](loading.md) — using compiled assets at runtime
- [Models](../rendering/models.md) — rendering compiled models
- [Textures](../rendering/textures.md) — working with compiled textures
