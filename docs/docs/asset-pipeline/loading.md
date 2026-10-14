# Loading assets

Namespace: `Phoenix.Framework.AssetImport`

`AssetLoader` is the runtime asset manager in Phoenix. It handles cached loading of 3D models, bindless textures, and SPIR-V pipelines, prioritizing pre-compiled binary files in `.contentbin/`.

---

## Dual resolution pipeline

When `AssetLoader.LoadModel()` or `AssetLoader.LoadTexture()` is invoked:

1. **Binary distribution lookup**: Checks the `.contentbin/` directory first for pre-compiled binaries:
   - 3D models: `.contentbin/<path>.pmdl` (parsed directly by `BinaryModelReader`).
   - Textures: `.contentbin/<path>.dds` (direct block-compressed GPU staging upload).
   - Shaders: `.contentbin/<path>.spv` (pre-compiled SPIR-V bytecode).
2. **Raw development fallback**: If the binary is missing, falls back to raw source files (`.fbx`, `.obj`, `.gltf`, `.png`, `.jpg`) using runtime importers.

In production releases, you only ship the `.contentbin/` folder, eliminating Assimp and runtime shader compiler overhead.

---

## Loading models

```csharp
// Standard static model
Model model = AssetLoader.LoadModel("3D/sponza.fbx");

// Load with custom options
var options = new ModelLoadOptions
{
    Scale = 0.01f,
    CalculateTangents = true,
    FlipUV = false
};
Model customModel = AssetLoader.LoadModel("3D/house.obj", options);

// Skinned animated character
AnimatedModel character = AssetLoader.LoadAnimatedModel("3D/character.fbx");
```

---

## Loading textures

```csharp
// Load diffuse texture
VulkanTexture diffuse = AssetLoader.LoadTexture("Textures/brick_diffuse.png");

// TextureId can be passed directly to push constants for Set 1 bindless indexing
uint textureId = diffuse.TextureId;
```

---

## Asset caching

Loaded assets are cached in memory. Subsequent calls with the same path return the existing GPU resource:

```csharp
var modelA = AssetLoader.LoadModel("3D/tree.fbx");
var modelB = AssetLoader.LoadModel("3D/tree.fbx");
// ReferenceEquals(modelA, modelB) == true
```

Call `AssetLoader.UnloadAll()` during shutdown to dispose GPU buffers and textures.

---

## Live hot reloading

When running alongside `pat watch` or `pat web`, the game automatically hooks into WebSocket IPC messages:

```csharp
protected override void OnModelReloaded(ModelReloadEventArgs e)
{
    Log.Info($"Model modified on disk: {e.RelativePath}");
    // Rebuild scene structures or collision meshes if necessary
}

protected override void OnTextureReloaded(TextureReloadEventArgs e)
{
    Log.Info($"Texture reloaded: {e.RelativePath}");
}
```
Reload events trigger on the main game thread during `InternalUpdate()`, ensuring thread-safe resource updates.
