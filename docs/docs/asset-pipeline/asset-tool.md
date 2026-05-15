# AssetTool

`Phoenix.AssetTool` is a companion tool for compiling source assets (FBX models, PNG textures, GLSL shaders) into Phoenix's binary format.

## Projects

| Project | Type | Purpose |
|---------|------|---------|
| `Phoenix.AssetTool.Core` | Library | Asset compilation logic |
| `Phoenix.AssetTool.Gui` | GUI app | Drag-and-drop asset browser |
| `Phoenix.AssetTool.Cli` | CLI tool | Command-line builds |

## Building Assets

### CLI

```bash
# Build all assets in a directory
dotnet run --project Phoenix.AssetTool.Cli -- build --source ./Content --output ./ContentBin

# Build specific files
dotnet run --project Phoenix.AssetTool.Cli -- build --files ./Content/characters/robot.fbx

# Auto mode (watch for changes)
dotnet run --project Phoenix.AssetTool.Cli -- auto --source ./Content --output ./ContentBin
```

### GUI

Launch the GUI application:

```bash
dotnet run --project Phoenix.AssetTool.Gui
```

The GUI provides:
- **Asset Browser** — browse and preview assets
- **Build Panel** — select source/output directories, build selected assets
- **Options** — configure model export settings, texture compression

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
| `.vert`, `.frag` | `.vert`, `.frag` (copied as-is) |

No compilation or optimization is performed on shaders — they are passed through unchanged.

## Asset Pipeline

The build pipeline processes assets in this order:

```
1. Model assets → BinaryModelWriter
     ├── Extract mesh data (vertices, indices, transforms)
     ├── Extract bone animation data (keyframes, transforms)
     └── Write .bin file

2. Texture assets → TextureBinaryWriter
     ├── Load image (ImageSharp)
     ├── Compress to BC1/BC3/BC5
     └── Write .bin file (with mipmaps)

3. Shader assets → GLCompiler
     ├── Compile vertex shader
     ├── Compile fragment shader
     └── Link program (validation only, output is source text)
```

## Build Options

### Model Load Options

| Option | Default | Description |
|--------|---------|-------------|
| `ExtractAnimations` | `true` | Extract bone animation clips |
| `GenerateTangents` | `true` | Generate tangent/bitangent vectors |
| `FlipUVs` | `false` | Flip V texture coordinate |

### Texture Load Options

| Option | Default | Description |
|--------|---------|-------------|
| `Compression` | `BC3` | Target compression format |
| `MipMaps` | `true` | Generate mipmaps |
| `ResizeToPowerOfTwo` | `false` | Resize texture to next power of two |

## Internal Architecture

### BuildStatus

```csharp
public class BuildStatus
{
    public string Name { get; }
    public string RelativePath { get; }
    public AssetType Type { get; }
    public AssetBuildState State { get; set; }  // Queued, Building, Completed, Failed
    public string Error { get; set; }
}
```

### AssetBuildController

Manages the build queue:

```csharp
// Add assets to build queue
AssetBuildController.AddToQueue(buildStatuses);

// Start building
await AssetBuildController.RunBuildAsync(rebuild: false, onFinish: () => { /* done */ });

// Cancel a running build
AssetBuildController.Cancel();

// Check status
var status = AssetBuildController.Status;
```

### AssetBuildPipeline

Runs asset builds in parallel:

```csharp
// All assets are built concurrently
// Shader builds run first (they have no dependencies)
// Model and texture builds run in parallel
var results = await AssetBuildPipeline.RunBuilds(buildStatuses, cancellationToken);
```

## File Tools

### FileTools

Utility for working with asset files:

```csharp
// Check if file is an asset
bool isAsset = FileTools.IsAsset("file.bin");

// Get file type
var type = FileTools.GetFileType("model.fbx");  // Returns AssetType.Model

// List assets in directory
var assets = FileTools.GetFilesInDirectory("./Content");
```

### MultiFileWatcher

Watches multiple directories for file changes:

```csharp
var watcher = MultiFileWatcher.Create(
    new[] { "./Content/models", "./Content/textures" },
    (changedFile) => { /* rebuild triggered */ }
);
```

## GLSL Shader Compilation

Shaders are validated by compiling and linking them with Silk.NET:

```csharp
var result = GLCompiler.Compile(vertexPath, fragmentPath);
// result.Success — true if compilation succeeded
// result.Error — error message if failed
```

The compiled program is discarded after validation — the source files are used as-is at runtime.

## See Also

- [Loading Assets](loading.md) — using compiled assets at runtime
- [Models](../rendering/models.md) — rendering compiled models
- [Textures](../rendering/textures.md) — working with compiled textures
