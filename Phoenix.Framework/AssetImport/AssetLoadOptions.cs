namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Serializable container mapping relative asset paths to model and texture load options.
/// </summary>
public sealed class AssetLoadOptions
{
    public Dictionary<string, ModelLoadOptions> Models { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, TextureLoadOptions> Textures { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
