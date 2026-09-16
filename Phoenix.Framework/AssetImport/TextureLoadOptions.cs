namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Configuration options for loading, mipmapping, and sampling 2D textures.
/// </summary>
public sealed class TextureLoadOptions
{
    public bool GenerateMipmaps { get; set; } = true;
    public TextureCompressionFormat Compression { get; set; } = TextureCompressionFormat.BC7;
    public bool IsSRgb { get; set; } = true;
    public bool LimitSize { get; set; } = true;
    public int MaxSize { get; set; } = 1024;
    public float Anisotropic { get; set; } = 16.0f;
    public TextureWrapMode WrapU { get; set; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; set; } = TextureWrapMode.Repeat;
    public TextureFilterMode MinFilter { get; set; } = TextureFilterMode.Linear;
    public TextureFilterMode MagFilter { get; set; } = TextureFilterMode.Linear;
    public TextureFilterMode MipmapMode { get; set; } = TextureFilterMode.Linear;

    public TextureLoadOptions Clone() => new()
    {
        GenerateMipmaps = GenerateMipmaps,
        Compression = Compression,
        IsSRgb = IsSRgb,
        LimitSize = LimitSize,
        MaxSize = MaxSize,
        Anisotropic = Anisotropic,
        WrapU = WrapU,
        WrapV = WrapV,
        MinFilter = MinFilter,
        MagFilter = MagFilter,
        MipmapMode = MipmapMode
    };
}
