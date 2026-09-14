using System.Text.Json;
using Phoenix;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Manages asset load option discovery, sidecar JSON parsing, and centralized options resolution.
/// </summary>
public static class AssetOptions
{
    private static readonly object _lock = new();
    private static AssetLoadOptions? _centralOptions;
    private static string? _centralOptionsPath;

    /// <summary>
    /// Loads or reloads the central asset options file from the content root.
    /// </summary>
    public static void Initialize(string contentRoot = "Content")
    {
        lock (_lock)
        {
            string[] candidates =
            [
                Path.Combine(contentRoot, "asset-options.json"),
                Path.Combine(contentRoot, "asset-load-options.json"),
                Path.Combine(AppContext.BaseDirectory, contentRoot, "asset-options.json"),
                Path.Combine(AppContext.BaseDirectory, contentRoot, "asset-load-options.json")
            ];

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    _centralOptionsPath = Path.GetFullPath(candidate);
                    if (JsonIOTools.Load<AssetLoadOptions>(_centralOptionsPath, out var loaded))
                    {
                        _centralOptions = loaded;
                        return;
                    }
                }
            }

            _centralOptionsPath = Path.GetFullPath(Path.Combine(contentRoot, "asset-options.json"));
            _centralOptions = new AssetLoadOptions();
        }
    }

    /// <summary>
    /// Resolves model loading options using explicit overrides, sidecar JSON files, central manifest, or defaults.
    /// </summary>
    public static (ModelLoadOptions Options, string? SourceFile) ResolveModelOptions(string assetPath, ModelLoadOptions? explicitOptions = null)
    {
        if (explicitOptions != null)
            return (explicitOptions, null);

        string? sidecar = FindSidecarFile(assetPath);
        if (sidecar != null && JsonIOTools.Load<ModelLoadOptions>(sidecar, out var sidecarOptions))
        {
            return (sidecarOptions, sidecar);
        }

        lock (_lock)
        {
            if (_centralOptions == null)
                Initialize();

            string normalized = NormalizeAssetKey(assetPath);
            if (_centralOptions!.Models.TryGetValue(normalized, out var centralOptions))
            {
                return (centralOptions, _centralOptionsPath);
            }
        }

        return (new ModelLoadOptions(), null);
    }

    /// <summary>
    /// Resolves texture loading options using explicit overrides, sidecar JSON files, central manifest, or defaults.
    /// </summary>
    public static (TextureLoadOptions Options, string? SourceFile) ResolveTextureOptions(string assetPath, TextureLoadOptions? explicitOptions = null)
    {
        if (explicitOptions != null)
            return (explicitOptions, null);

        string? sidecar = FindSidecarFile(assetPath);
        if (sidecar != null && JsonIOTools.Load<TextureLoadOptions>(sidecar, out var sidecarOptions))
        {
            return (sidecarOptions, sidecar);
        }

        lock (_lock)
        {
            if (_centralOptions == null)
                Initialize();

            string normalized = NormalizeAssetKey(assetPath);
            if (_centralOptions!.Textures.TryGetValue(normalized, out var centralOptions))
            {
                return (centralOptions, _centralOptionsPath);
            }
        }

        return (InferDefaultOptions(assetPath), null);
    }

    /// <summary>
    /// Registers or updates model options in the central configuration.
    /// </summary>
    public static void SetModelOptions(string assetPath, ModelLoadOptions options)
    {
        lock (_lock)
        {
            if (_centralOptions == null)
                Initialize();

            string key = NormalizeAssetKey(assetPath);
            _centralOptions!.Models[key] = options;
        }
    }

    /// <summary>
    /// Registers or updates texture options in the central configuration.
    /// </summary>
    public static void SetTextureOptions(string assetPath, TextureLoadOptions options)
    {
        lock (_lock)
        {
            if (_centralOptions == null)
                Initialize();

            string key = NormalizeAssetKey(assetPath);
            _centralOptions!.Textures[key] = options;
        }
    }

    /// <summary>
    /// Saves the current central options configuration to disk.
    /// </summary>
    public static void SaveCentral(string? targetPath = null)
    {
        lock (_lock)
        {
            string savePath = targetPath ?? _centralOptionsPath ?? "Content/asset-options.json";
            string? dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            JsonIOTools.Save(savePath, _centralOptions ?? new AssetLoadOptions());
            _centralOptionsPath = Path.GetFullPath(savePath);
        }
    }

    /// <summary>
    /// Ensures that a sidecar options JSON file exists for the given texture.
    /// If it does not exist, writes the provided options as a sidecar JSON file ({texturePath}.json).
    /// Existing sidecar files are never overwritten.
    /// </summary>
    public static void EnsureSidecarFile(string texturePath, TextureLoadOptions options)
    {
        try
        {
            string sidecarPath = texturePath + ".json";
            if (!File.Exists(sidecarPath))
            {
                string? dir = Path.GetDirectoryName(sidecarPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                JsonIOTools.Save(sidecarPath, options);
                Log.Info($"[AssetOptions] Created sidecar options file: {sidecarPath}");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[AssetOptions] Failed to create sidecar options file for '{texturePath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Infers default texture loading options based on filename conventions.
    /// Normal maps default to BC5 (Linear).
    /// Packed PBR maps default to BC7 (Linear).
    /// Grayscale masks (roughness, glossiness, metalness, ambient occlusion) default to BC4 (Linear).
    /// Color maps (albedo, diffuse, specular, emissive) and unrecognized textures default to BC7 (sRGB).
    /// </summary>
    public static TextureLoadOptions InferDefaultOptions(string assetPath)
    {
        string name = Path.GetFileNameWithoutExtension(assetPath);

        // 1. Normal maps: BC5, Linear
        if (HasAnyKeyword(name, "normal", "norm", "nrm"))
        {
            return new TextureLoadOptions
            {
                Compression = TextureCompressionFormat.BC5,
                IsSRgb = false
            };
        }

        // 2. Packed PBR masks: BC7, Linear
        if (HasAnyKeyword(name, "orm", "rma", "mrao", "packed"))
        {
            return new TextureLoadOptions
            {
                Compression = TextureCompressionFormat.BC7,
                IsSRgb = false
            };
        }

        // 3. Grayscale scalar masks: BC4, Linear
        if (HasAnyKeyword(name, "rough", "roughness", "gloss", "glossiness", "metal", "metallic", "ao", "ambient", "occlusion", "height", "disp", "displacement"))
        {
            return new TextureLoadOptions
            {
                Compression = TextureCompressionFormat.BC4,
                IsSRgb = false
            };
        }

        // 4. Color maps: BC7, sRGB
        if (HasAnyKeyword(name, "albedo", "diffuse", "diff", "basecolor", "color", "specular", "spec", "emissive", "emission", "emit"))
        {
            return new TextureLoadOptions
            {
                Compression = TextureCompressionFormat.BC7,
                IsSRgb = true
            };
        }

        // 5. Global fallback: BC7, sRGB
        return new TextureLoadOptions
        {
            Compression = TextureCompressionFormat.BC7,
            IsSRgb = true
        };
    }

    private static bool HasAnyKeyword(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (HasKeyword(text, keyword))
                return true;
        }
        return false;
    }

    private static bool HasKeyword(string text, string keyword)
    {
        if (text.Length < keyword.Length)
            return false;

        if (text.Length == keyword.Length)
            return text.Equals(keyword, StringComparison.OrdinalIgnoreCase);

        int index = 0;
        while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            bool startBoundary = index == 0 || !char.IsLetter(text[index - 1]);
            bool endBoundary = (index + keyword.Length >= text.Length) || !char.IsLetter(text[index + keyword.Length]);
            if (startBoundary && endBoundary)
                return true;

            index += keyword.Length;
        }

        return false;
    }

    private static string? FindSidecarFile(string assetPath)
    {
        string candidate1 = assetPath + ".json";
        if (File.Exists(candidate1))
            return Path.GetFullPath(candidate1);

        string dir = Path.GetDirectoryName(assetPath) ?? "";
        string nameWithoutExt = Path.GetFileNameWithoutExtension(assetPath);
        string candidate2 = Path.Combine(dir, nameWithoutExt + ".json");
        if (File.Exists(candidate2))
            return Path.GetFullPath(candidate2);

        return null;
    }

    private static string NormalizeAssetKey(string path)
    {
        string normalized = path.Replace('\\', '/');
        int contentIdx = normalized.IndexOf("Content/", StringComparison.OrdinalIgnoreCase);
        if (contentIdx >= 0)
            return normalized[(contentIdx + "Content/".Length)..];

        return normalized.TrimStart('/');
    }
}
