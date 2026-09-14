namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Encapsulates a complete texture image payload including base dimensions, compression format,
/// color space information, and all pre-encoded mipmap levels.
/// </summary>
public sealed class CompressedTextureData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public TextureCompressionFormat Format { get; set; }
    public bool IsSRgb { get; set; } = true;
    public List<CompressedMipLevel> Mips { get; set; } = [];

    public CompressedTextureData()
    {
    }

    public CompressedTextureData(int width, int height, TextureCompressionFormat format, bool isSRgb, List<CompressedMipLevel> mips)
    {
        Width = width;
        Height = height;
        Format = format;
        IsSRgb = isSRgb;
        Mips = mips;
    }
}
