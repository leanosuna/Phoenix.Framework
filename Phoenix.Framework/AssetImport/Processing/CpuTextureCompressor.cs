using BCnEncoder.Encoder;
using BCnEncoder.Shared;

namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Provides multithreaded CPU block compression and mipmap generation for 2D textures.
/// </summary>
public static class CpuTextureCompressor
{
    /// <summary>
    /// Encodes raw RGBA32 pixel data to the specified BCn compression format, generating mipmaps if requested.
    /// </summary>
    public static CompressedTextureData Compress(
        byte[] rgbaPixels,
        int width,
        int height,
        TextureCompressionFormat format,
        bool generateMipmaps = true,
        bool isSRgb = true)
    {
        if (format == TextureCompressionFormat.None)
        {
            List<CompressedMipLevel> rawMips =
            [
                new CompressedMipLevel(width, height, rgbaPixels)
            ];
            return new CompressedTextureData(width, height, format, isSRgb, rawMips);
        }

        var bcFormat = MapFormat(format);
        var encoder = new BcEncoder
        {
            OutputOptions =
            {
                Format = bcFormat,
                GenerateMipMaps = generateMipmaps,
                Quality = CompressionQuality.Balanced
            }
        };

        int mipCount = 1;
        if (generateMipmaps)
        {
            mipCount = encoder.CalculateNumberOfMipLevels(width, height);
        }

        byte[][] encodedMips = encoder.EncodeToRawBytes(rgbaPixels, width, height, PixelFormat.Rgba32);

        List<CompressedMipLevel> mips = new(encodedMips.Length);
        for (int i = 0; i < encodedMips.Length; i++)
        {
            int mW = Math.Max(1, width >> i);
            int mH = Math.Max(1, height >> i);
            mips.Add(new CompressedMipLevel(mW, mH, encodedMips[i]));
        }

        return new CompressedTextureData(width, height, format, isSRgb, mips);
    }

    private static CompressionFormat MapFormat(TextureCompressionFormat format) => format switch
    {
        TextureCompressionFormat.BC1 => CompressionFormat.Bc1,
        TextureCompressionFormat.BC3 => CompressionFormat.Bc3,
        TextureCompressionFormat.BC4 => CompressionFormat.Bc4,
        TextureCompressionFormat.BC5 => CompressionFormat.Bc5,
        TextureCompressionFormat.BC7 => CompressionFormat.Bc7,
        _ => CompressionFormat.Bc7
    };
}
