using Phoenix;
using Phoenix.Framework.AssetImport.Processing;
using Phoenix.Framework.AssetImport.Tracking;
using Phoenix.Framework.Rendering;
using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Shaders;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Shaderc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Security.Cryptography;
using System.Text;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Central runtime asset loading system providing automated compilation, caching, and GPU registration
/// for shaders, 2D textures, static 3D models, and skeletal animated meshes.
/// </summary>
public static class AssetLoader
{
    private static Graphics? _graphics;
    private static string _contentRoot = "Content";
    private static string _cacheRoot = ".cache";

    private static readonly Dictionary<string, Model> _loadedModels = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, VulkanTexture> _loadedTextures = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether the AssetLoader has been initialized with an active Graphics context.
    /// </summary>
    public static bool IsInitialized => _graphics != null;

    /// <summary>
    /// Initializes the asset pipeline with the current Graphics subsystem and optional custom directory roots.
    /// </summary>
    public static void Initialize(Graphics graphics, string contentRoot = "Content", string cacheRoot = ".cache")
    {
        _graphics = graphics;
        _contentRoot = contentRoot;
        _cacheRoot = cacheRoot;

        Directory.CreateDirectory(Path.Combine(_cacheRoot, "models"));
        Directory.CreateDirectory(Path.Combine(_cacheRoot, "shaders"));
        Directory.CreateDirectory(Path.Combine(_cacheRoot, "textures"));

        AssetOptions.Initialize(_contentRoot);
    }

    /// <summary>
    /// Disposes and clears all cached GPU models and textures.
    /// </summary>
    public static void UnloadAll()
    {
        lock (_loadedTextures)
        {
            foreach (var texture in _loadedTextures.Values)
            {
                texture.Dispose();
            }
            _loadedTextures.Clear();
        }

        lock (_loadedModels)
        {
            foreach (var model in _loadedModels.Values)
            {
                model.Dispose();
            }
            _loadedModels.Clear();
        }
    }


    private static void EnsureInitialized()
    {
        if (_graphics == null)
            throw new InvalidOperationException("AssetLoader.Initialize(Graphics) must be called before loading assets.");
    }

    /// <summary>
    /// Resolves an asset file path checking current working directory, BaseDirectory, and Content folder.
    /// </summary>
    public static string ResolvePath(string relativeOrAbsolutePath)
    {
        if (File.Exists(relativeOrAbsolutePath))
            return Path.GetFullPath(relativeOrAbsolutePath);

        string fromBase = Path.Combine(AppContext.BaseDirectory, relativeOrAbsolutePath);
        if (File.Exists(fromBase))
            return Path.GetFullPath(fromBase);

        string fromContent = Path.Combine(AppContext.BaseDirectory, _contentRoot, relativeOrAbsolutePath);
        if (File.Exists(fromContent))
            return Path.GetFullPath(fromContent);

        string fromCwdContent = Path.Combine(Directory.GetCurrentDirectory(), _contentRoot, relativeOrAbsolutePath);
        if (File.Exists(fromCwdContent))
            return Path.GetFullPath(fromCwdContent);

        return Path.GetFullPath(relativeOrAbsolutePath);
    }

    /// <summary>
    /// Loads and compiles GLSL vertex and fragment shaders into SPIR-V bytecodes with disk caching.
    /// </summary>
    public static (byte[] VertexSpv, byte[] FragmentSpv) LoadShader(string vertPath, string fragPath)
    {
        string resolvedVert = ResolvePath(vertPath);
        string resolvedFrag = ResolvePath(fragPath);

        if (!File.Exists(resolvedVert))
            throw new FileNotFoundException($"Vertex shader not found: {resolvedVert}", resolvedVert);
        if (!File.Exists(resolvedFrag))
            throw new FileNotFoundException($"Fragment shader not found: {resolvedFrag}", resolvedFrag);

        string shaderCacheDir = Path.Combine(_cacheRoot, "shaders");
        byte[] vertSpv = ShaderCompiler.LoadOrCompile(resolvedVert, ShaderKind.VertexShader, shaderCacheDir);
        byte[] fragSpv = ShaderCompiler.LoadOrCompile(resolvedFrag, ShaderKind.FragmentShader, shaderCacheDir);

        return (vertSpv, fragSpv);
    }

    /// <summary>
    /// Loads a 2D image file, uploads it to GPU device memory, and registers it into the bindless descriptor set.
    /// Resolves options via sidecar JSON, central configuration, or code parameters, and caches decoded data on disk.
    /// </summary>
    public static VulkanTexture LoadTexture(string path, TextureLoadOptions? options = null, uint? preallocatedSlot = null)
    {
        return LoadTextureAsync(path, options, preallocatedSlot).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Loads a 2D image file asynchronously on a background thread pool worker, compresses via CPU SIMD,
    /// caches to disk in PTEX v2 format, and uploads to the GPU.
    /// </summary>
    public static async Task<VulkanTexture> LoadTextureAsync(string path, TextureLoadOptions? options = null, uint? preallocatedSlot = null, AssetLoadOperation? operation = null)
    {
        EnsureInitialized();

        string resolved = ResolvePath(path);
        if (!File.Exists(resolved))
            throw new FileNotFoundException($"Texture file not found: {resolved}");

        var op = operation ?? AssetLoadingTracker.BeginOperation(resolved, AssetLoadOperationType.Texture, "Resolving options...");

        try
        {
            var (resolvedOptions, sourceFile) = AssetOptions.ResolveTextureOptions(resolved, options);
            string cacheKey = ComputeTextureCacheKey(resolved, resolvedOptions, sourceFile);

            lock (_loadedTextures)
            {
                if (_loadedTextures.TryGetValue(cacheKey, out var cached))
                {
                    if (preallocatedSlot.HasValue)
                    {
                        _graphics!.BindlessManager.UpdateDescriptor(preallocatedSlot.Value, cached.ImageView, cached.Sampler);
                    }
                    op.Complete($"Loaded {cached.Format} (Memory cache)");
                    return cached;
                }
            }

            string sanitizedName = Path.GetFileNameWithoutExtension(resolved);
            string pathHash = ComputeTexturePathHash(resolved);
            string cacheFile = Path.Combine(_cacheRoot, "textures", $"{sanitizedName}_{pathHash}.bin");

            op.UpdateStatus("Checking cache...", 0.1f);

            CompressedTextureData texData = await Task.Run(() =>
            {
                if (File.Exists(cacheFile))
                {
                    try
                    {
                        using var fs = File.OpenRead(cacheFile);
                        using var br = new BinaryReader(fs);
                        uint magic = br.ReadUInt32();
                        if (magic == 0x58455450) // "PTEX"
                        {
                            uint ver = br.ReadUInt32();
                            if (ver == 2)
                            {
                                int width = br.ReadInt32();
                                int height = br.ReadInt32();
                                var format = (TextureCompressionFormat)br.ReadInt32();
                                bool isSRgb = br.ReadBoolean();
                                int mipCount = br.ReadInt32();

                                bool formatMatches = format == resolvedOptions.Compression && isSRgb == resolvedOptions.IsSRgb;
                                bool notStale = File.GetLastWriteTimeUtc(cacheFile) >= File.GetLastWriteTimeUtc(resolved);
                                bool mipsMatch = !resolvedOptions.GenerateMipmaps || mipCount > 1 || (width == 1 && height == 1);

                                if (!formatMatches)
                                {
                                    op.UpdateStatus($"Format mismatch (cached: {format}, desired: {resolvedOptions.Compression}). Re-encoding...", 0.2f);
                                    Log.Info($"[AssetLoader] Texture '{Path.GetFileName(resolved)}' compression format mismatch (cached: {format}, desired: {resolvedOptions.Compression}, sRGB: {isSRgb} vs {resolvedOptions.IsSRgb}). Re-encoding...");
                                }
                                else if (notStale && mipsMatch)
                                {
                                    op.UpdateStatus($"Reading cached {format}...", 0.5f);
                                    List<CompressedMipLevel> mips = new(mipCount);
                                    for (int i = 0; i < mipCount; i++)
                                    {
                                        int mW = br.ReadInt32();
                                        int mH = br.ReadInt32();
                                        int len = br.ReadInt32();
                                        byte[] data = br.ReadBytes(len);
                                        mips.Add(new CompressedMipLevel(mW, mH, data));
                                    }
                                    return new CompressedTextureData(width, height, format, isSRgb, mips);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"[AssetLoader] Failed to read texture cache file '{cacheFile}': {ex.Message}. Re-encoding...");
                    }
                }

                op.UpdateStatus($"Encoding ({resolvedOptions.Compression})...", 0.35f);
                using var image = Image.Load<Rgba32>(resolved);
                int w = image.Width;
                int h = image.Height;
                byte[] pixels = new byte[w * h * 4];
                image.CopyPixelDataTo(pixels);

                var compressed = CpuTextureCompressor.Compress(
                    pixels, w, h, resolvedOptions.Compression, resolvedOptions.GenerateMipmaps, resolvedOptions.IsSRgb);

                try
                {
                    string? dir = Path.GetDirectoryName(cacheFile);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    using var fs = File.Create(cacheFile);
                    using var bw = new BinaryWriter(fs);
                    bw.Write((uint)0x58455450); // "PTEX"
                    bw.Write((uint)2);          // version 2
                    bw.Write(compressed.Width);
                    bw.Write(compressed.Height);
                    bw.Write((int)compressed.Format);
                    bw.Write(compressed.IsSRgb);
                    bw.Write(compressed.Mips.Count);
                    for (int i = 0; i < compressed.Mips.Count; i++)
                    {
                        bw.Write(compressed.Mips[i].Width);
                        bw.Write(compressed.Mips[i].Height);
                        bw.Write(compressed.Mips[i].Data.Length);
                        bw.Write(compressed.Mips[i].Data);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"[AssetLoader] Failed to write texture cache file '{cacheFile}': {ex.Message}");
                }

                return compressed;
            });

            op.UpdateStatus("Uploading to GPU...", 0.85f);
            var texture = texData.Format != TextureCompressionFormat.None
                ? _graphics!.CreateTexture(texData, resolvedOptions, preallocatedSlot)
                : _graphics!.CreateTexture(texData.Width, texData.Height, texData.Mips[0].Data, resolvedOptions, preallocatedSlot: preallocatedSlot);

            lock (_loadedTextures)
            {
                _loadedTextures[cacheKey] = texture;
            }

            string slotInfo = preallocatedSlot.HasValue ? $" (Slot {preallocatedSlot.Value})" : "";
            op.Complete($"Uploaded {texture.Format}{slotInfo}");
            return texture;
        }
        catch (Exception ex)
        {
            op.Fail(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Loads a static 3D model with automated runtime Assimp compilation, options resolution, and binary disk caching.
    /// </summary>
    public static Model LoadModel(string path, ModelLoadOptions? options = null)
    {
        EnsureInitialized();

        string resolved = ResolvePath(path);
        if (!File.Exists(resolved))
            throw new FileNotFoundException($"Model file not found: {resolved}");

        var (resolvedOptions, sourceFile) = AssetOptions.ResolveModelOptions(resolved, options);

        string cacheKey = ComputeModelCacheKey(resolved, resolvedOptions, sourceFile);
        lock (_loadedModels)
        {
            if (_loadedModels.TryGetValue(cacheKey, out var existing))
                return existing;
        }

        string sanitizedName = Path.GetFileNameWithoutExtension(resolved);
        string cacheFile = Path.Combine(_cacheRoot, "models", $"{sanitizedName}_{cacheKey}.bin");

        bool canUseCache = File.Exists(cacheFile) &&
            File.GetLastWriteTimeUtc(cacheFile) >= File.GetLastWriteTimeUtc(resolved) &&
            (sourceFile == null || File.GetLastWriteTimeUtc(cacheFile) >= File.GetLastWriteTimeUtc(sourceFile));

        var modelTask = AssetLoadingTracker.BeginOperation(resolved, AssetLoadOperationType.Model, canUseCache ? "Loading from binary cache..." : "Importing FBX/GLTF via Assimp...");

        Model model;
        List<EmbeddedTexturePayload>? payloads = null;
        try
        {
            if (canUseCache)
            {
                model = BinaryModelReader.Read(_graphics!, cacheFile);
                modelTask.Complete("Loaded from cache");
            }
            else
            {
                (model, payloads) = AssimpImporter.ImportAndCache(_graphics!, resolved, cacheFile, resolvedOptions, modelTask);
                modelTask.Complete("Model imported and cached");
            }
        }
        catch (Exception ex)
        {
            modelTask.Fail(ex.Message);
            throw;
        }

        model.BindlessManager = _graphics!.BindlessManager;

        if (model.TextureNames.Count > 0)
        {
            StartModelTextureStreaming(model, resolved, resolvedOptions, payloads);
        }

        lock (_loadedModels)
        {
            _loadedModels[cacheKey] = model;
        }

        return model;
    }

    /// <summary>
    /// Loads a static 3D model asynchronously with background Assimp compilation and binary disk caching.
    /// </summary>
    public static async Task<Model> LoadModelAsync(string path, ModelLoadOptions? options = null)
    {
        EnsureInitialized();

        string resolved = ResolvePath(path);
        if (!File.Exists(resolved))
            throw new FileNotFoundException($"Model file not found: {resolved}");

        var (resolvedOptions, sourceFile) = AssetOptions.ResolveModelOptions(resolved, options);

        string cacheKey = ComputeModelCacheKey(resolved, resolvedOptions, sourceFile);
        lock (_loadedModels)
        {
            if (_loadedModels.TryGetValue(cacheKey, out var existing))
                return existing;
        }

        string sanitizedName = Path.GetFileNameWithoutExtension(resolved);
        string cacheFile = Path.Combine(_cacheRoot, "models", $"{sanitizedName}_{cacheKey}.bin");

        bool canUseCache = File.Exists(cacheFile) &&
            File.GetLastWriteTimeUtc(cacheFile) >= File.GetLastWriteTimeUtc(resolved) &&
            (sourceFile == null || File.GetLastWriteTimeUtc(cacheFile) >= File.GetLastWriteTimeUtc(sourceFile));

        var modelTask = AssetLoadingTracker.BeginOperation(resolved, AssetLoadOperationType.Model, canUseCache ? "Loading from binary cache..." : "Importing FBX/GLTF via Assimp...");

        try
        {
            var (model, payloads) = await Task.Run(() =>
            {
                if (canUseCache)
                {
                    var m = BinaryModelReader.Read(_graphics!, cacheFile);
                    return (m, (List<EmbeddedTexturePayload>?)null);
                }
                else
                {
                    var res = AssimpImporter.ImportAndCache(_graphics!, resolved, cacheFile, resolvedOptions, modelTask);
                    return (res.Model, (List<EmbeddedTexturePayload>?)res.EmbeddedTextures);
                }
            });

            modelTask.Complete(canUseCache ? "Loaded from cache" : "Model imported and cached");

            model.BindlessManager = _graphics!.BindlessManager;

            if (model.TextureNames.Count > 0)
            {
                StartModelTextureStreaming(model, resolved, resolvedOptions, payloads);
            }

            lock (_loadedModels)
            {
                _loadedModels[cacheKey] = model;
            }

            return model;
        }
        catch (Exception ex)
        {
            modelTask.Fail(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Loads a skeletal animated 3D model with one or more animation tracks and bone hierarchies.
    /// </summary>
    public static AnimatedModel LoadAnimatedModel(string modelPath, params string[] animationPaths)
    {
        string resolved = ResolvePath(modelPath);
        var (options, _) = AssetOptions.ResolveModelOptions(resolved);
        options.IsAnimated = true;
        options.Tangents = true;
        if (animationPaths.Length > 0)
        {
            options.AnimationFiles = animationPaths.Select(ResolvePath).ToList();
        }

        return (AnimatedModel)LoadModel(modelPath, options);
    }

    /// <summary>
    /// Loads a skeletal animated 3D model with explicit options and animation tracks.
    /// </summary>
    public static AnimatedModel LoadAnimatedModel(string modelPath, ModelLoadOptions options, params string[] animationPaths)
    {
        options.IsAnimated = true;
        options.Tangents = true;
        if (animationPaths.Length > 0)
        {
            options.AnimationFiles = animationPaths.Select(ResolvePath).ToList();
        }

        return (AnimatedModel)LoadModel(modelPath, options);
    }

    /// <summary>
    /// Loads a skeletal animated 3D model asynchronously with one or more animation tracks and bone hierarchies.
    /// </summary>
    public static async Task<AnimatedModel> LoadAnimatedModelAsync(string modelPath, params string[] animationPaths)
    {
        string resolved = ResolvePath(modelPath);
        var (options, _) = AssetOptions.ResolveModelOptions(resolved);
        options.IsAnimated = true;
        options.Tangents = true;
        if (animationPaths.Length > 0)
        {
            options.AnimationFiles = animationPaths.Select(ResolvePath).ToList();
        }

        var model = await LoadModelAsync(modelPath, options);
        return (AnimatedModel)model;
    }

    /// <summary>
    /// Loads a skeletal animated 3D model asynchronously with explicit options and animation tracks.
    /// </summary>
    public static async Task<AnimatedModel> LoadAnimatedModelAsync(string modelPath, ModelLoadOptions options, params string[] animationPaths)
    {
        options.IsAnimated = true;
        options.Tangents = true;
        if (animationPaths.Length > 0)
        {
            options.AnimationFiles = animationPaths.Select(ResolvePath).ToList();
        }

        var model = await LoadModelAsync(modelPath, options);
        return (AnimatedModel)model;
    }


    private static string ComputeTextureCacheKey(string texturePath, TextureLoadOptions options, string? optionsSourceFile)
    {
        StringBuilder sb = new();
        sb.Append(texturePath);
        if (File.Exists(texturePath))
            sb.Append(File.GetLastWriteTimeUtc(texturePath).Ticks);

        if (optionsSourceFile != null && File.Exists(optionsSourceFile))
            sb.Append(File.GetLastWriteTimeUtc(optionsSourceFile).Ticks);

        sb.Append((int)options.Compression);
        sb.Append(options.IsSRgb);
        sb.Append(options.GenerateMipmaps);
        sb.Append(options.Anisotropic);
        sb.Append((int)options.WrapU);
        sb.Append((int)options.WrapV);
        sb.Append((int)options.MinFilter);
        sb.Append((int)options.MagFilter);
        sb.Append((int)options.MipmapMode);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private static string ComputeModelCacheKey(string modelPath, ModelLoadOptions options, string? optionsSourceFile)
    {
        StringBuilder sb = new();
        sb.Append(modelPath);
        if (File.Exists(modelPath))
            sb.Append(File.GetLastWriteTimeUtc(modelPath).Ticks);

        if (optionsSourceFile != null && File.Exists(optionsSourceFile))
            sb.Append(File.GetLastWriteTimeUtc(optionsSourceFile).Ticks);

        sb.Append(options.ExtractTextures);
        sb.Append((int)options.ExtractedTextureCompression);
        sb.Append(options.IsAnimated);
        sb.Append(options.Tangents);
        sb.Append(options.PreTransform);
        sb.Append(options.Scale);
        if (options.AssimpFlags != null)
        {
            foreach (var flag in options.AssimpFlags)
            {
                sb.Append(flag);
            }
        }
        sb.Append(options.AssimpFlagsMask);

        if (options.AnimationFiles != null)
        {
            foreach (var f in options.AnimationFiles)
            {
                sb.Append(f);
                if (File.Exists(f))
                    sb.Append(File.GetLastWriteTimeUtc(f).Ticks);
            }
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private static string ComputeTexturePathHash(string texturePath)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(texturePath.ToLowerInvariant()));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private static void StartModelTextureStreaming(Model model, string resolvedModelPath, ModelLoadOptions modelOptions, List<EmbeddedTexturePayload>? payloads = null)
    {
        uint count = (uint)model.TextureNames.Count;
        uint baseSlot = _graphics!.BindlessManager.AllocateSlots(count);
        model.BaseTextureSlot = baseSlot;
        model.TextureSlotCount = count;
        model.TotalTextures = (int)count;

        Log.Info($"[AssetLoader] Reserved {count} bindless slots ({baseSlot}..{baseSlot + count - 1}) for model '{Path.GetFileName(resolvedModelPath)}'");

        string modelDir = Path.GetDirectoryName(resolvedModelPath) ?? "";

        for (int i = 0; i < model.TextureNames.Count; i++)
        {
            int textureIndex = i;
            string texName = model.TextureNames[i];
            uint targetSlot = baseSlot + (uint)textureIndex;

            if (string.IsNullOrWhiteSpace(texName))
            {
                model.NotifyTextureLoaded(textureIndex, null!);
                continue;
            }

            EmbeddedTexturePayload? payload = payloads?.FirstOrDefault(p =>
                string.Equals(p.RelativePath, texName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.FullPath, texName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.BaseName, Path.GetFileNameWithoutExtension(texName), StringComparison.OrdinalIgnoreCase));

            string? resolvedTexPath = null;
            if (payload != null)
            {
                resolvedTexPath = payload.FullPath;
            }
            else
            {
                string relCandidate = Path.Combine(modelDir, texName);
                if (File.Exists(relCandidate))
                {
                    resolvedTexPath = Path.GetFullPath(relCandidate);
                }
                else if (File.Exists(texName))
                {
                    resolvedTexPath = Path.GetFullPath(texName);
                }
                else
                {
                    string resolvedLoader = ResolvePath(texName);
                    if (File.Exists(resolvedLoader))
                    {
                        resolvedTexPath = resolvedLoader;
                    }
                }
            }

            if (resolvedTexPath == null)
            {
                Log.Warn($"[AssetLoader] Texture not found for model: {texName}");
                model.NotifyTextureLoaded(textureIndex, null!);
                continue;
            }

            var (texOptions, _) = AssetOptions.ResolveTextureOptions(resolvedTexPath);
            AssetOptions.EnsureSidecarFile(resolvedTexPath, texOptions);

            Log.Info($"[AssetLoader] Streaming texture [{textureIndex}] '{Path.GetFileName(resolvedTexPath)}' to slot {targetSlot} (Format: {texOptions.Compression})...");

            var texTask = AssetLoadingTracker.BeginOperation(resolvedTexPath, AssetLoadOperationType.Texture, $"Queued for slot {targetSlot}");

            _ = Task.Run(async () =>
            {
                try
                {
                    if (payload != null && !File.Exists(resolvedTexPath))
                    {
                        try
                        {
                            texTask.UpdateStatus("Extracting embedded payload...", 0.1f);
                            string? dir = Path.GetDirectoryName(resolvedTexPath);
                            if (!string.IsNullOrEmpty(dir))
                                Directory.CreateDirectory(dir);

                            if (payload.IsCompressed)
                            {
                                File.WriteAllBytes(resolvedTexPath, payload.Data);
                            }
                            else
                            {
                                using var image = Image.LoadPixelData<Rgba32>(payload.Data, payload.Width, payload.Height);
                                image.SaveAsPng(resolvedTexPath);
                            }
                        }
                        catch (IOException) when (File.Exists(resolvedTexPath))
                        {
                            // Another worker wrote the file concurrently
                        }
                    }

                    var texture = await LoadTextureAsync(resolvedTexPath, preallocatedSlot: targetSlot, operation: texTask);
                    Log.Info($"[AssetLoader] Texture [{textureIndex}] successfully uploaded to slot {targetSlot} ({texture.Width}x{texture.Height}, Format: {texture.Format})");
                    model.NotifyTextureLoaded(textureIndex, texture);
                }
                catch (Exception ex)
                {
                    texTask.Fail(ex.Message);
                    Log.Error($"[AssetLoader] Failed to stream model texture '{texName}': {ex.Message}");
                    model.NotifyTextureLoaded(textureIndex, null!);
                }
            });
        }
    }
}
