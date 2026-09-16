using BCnEncoder.Encoder;
using BCnEncoder.Shared;

namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Managed CPU texture compressor backed by BCnEncoder.NET.
/// Provides fully portable block compression without native binary dependencies.
/// </summary>
public sealed class BCnEncoderTextureCompressor : ITextureCompressor
{
    public string Name => "BCnEncoder.NET";

    public CompressedTextureData Compress(
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
                Quality = CompressionQuality.Fast
            }
        };

        byte[][] encodedMips = encoder.EncodeToRawBytes(rgbaPixels, width, height, BCnEncoder.Encoder.PixelFormat.Rgba32);

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
