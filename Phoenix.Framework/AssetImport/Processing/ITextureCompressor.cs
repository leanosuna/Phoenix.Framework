namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Defines a contract for compressing raw RGBA pixel data into GPU-ready block compressed formats.
/// </summary>
public interface ITextureCompressor
{
    /// <summary>
    /// Gets the human-readable display name of the compression backend.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Encodes raw RGBA32 pixel data into the specified block compression format, optionally generating mipmaps.
    /// </summary>
    CompressedTextureData Compress(
        byte[] rgbaPixels,
        int width,
        int height,
        TextureCompressionFormat format,
        bool generateMipmaps = true,
        bool isSRgb = true);
}
