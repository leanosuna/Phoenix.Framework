using Phoenix;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Central facade and factory providing CPU texture block compression.
/// Defaults to Compressonator on Windows and Linux, falling back to BCnEncoder.NET on macOS or if native libraries are missing.
/// </summary>
public static class CpuTextureCompressor
{
    private static ITextureCompressor _compressor = CreateDefaultCompressor();

    /// <summary>
    /// Gets or sets the active texture compression engine.
    /// </summary>
    public static ITextureCompressor ActiveCompressor
    {
        get => _compressor;
        set => _compressor = value ?? CreateDefaultCompressor();
    }

    /// <summary>
    /// Encodes raw RGBA32 pixel data into the specified block compression format using the active compression engine.
    /// </summary>
    public static CompressedTextureData Compress(
        byte[] rgbaPixels,
        int width,
        int height,
        TextureCompressionFormat format,
        bool generateMipmaps = true,
        bool isSRgb = true)
    {
        return _compressor.Compress(rgbaPixels, width, height, format, generateMipmaps, isSRgb);
    }

    private static ITextureCompressor CreateDefaultCompressor()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                if (Compressonator.NET.SDK_NativeMethods.IsSupported)
                {
                    Log.Info("[CpuTextureCompressor] Selected Compressonator.NET (SuperFast, q=0.05) as active texture compressor.");
                    return new CompressonatorTextureCompressor();
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[CpuTextureCompressor] Compressonator native initialization failed: {ex.Message}. Falling back to BCnEncoder.NET.");
            }
        }
        else
        {
            Log.Info("[CpuTextureCompressor] macOS detected. Selected BCnEncoder.NET as active texture compressor.");
        }

        return new BCnEncoderTextureCompressor();
    }
}
