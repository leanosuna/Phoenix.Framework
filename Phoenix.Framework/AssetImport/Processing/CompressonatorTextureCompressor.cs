using System.Runtime.InteropServices;
using Compressonator.NET;
using Phoenix.Framework.AssetImport.Native;

namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// High-performance CPU texture compressor backed by AMD Compressonator native libraries.
/// Uses direct P/Invoke bindings configured with SuperFast presets and 0.05 quality.
/// </summary>
public sealed class CompressonatorTextureCompressor : ITextureCompressor
{
    private const int OptionsBufferSize = 4096;

    public string Name => "Compressonator.NET (SuperFast, q=0.05)";

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

        var cmpFormat = MapFormat(format);
        var options = new CMP_CompressOptions
        {
            size = (uint)Marshal.SizeOf<CMP_CompressOptions>(),
            compressionSpeed = CMP_Speed.CMP_Speed_SuperFast,
            quality = 0.05f
        };

        IntPtr optPtr = Marshal.AllocHGlobal(OptionsBufferSize);
        try
        {
            unsafe
            {
                new Span<byte>((void*)optPtr, OptionsBufferSize).Clear();
            }

            Marshal.StructureToPtr(options, optPtr, false);

            if (!generateMipmaps)
            {
                byte[] encoded = CompressLevel(rgbaPixels, width, height, cmpFormat, optPtr);
                List<CompressedMipLevel> singleMip = [new CompressedMipLevel(width, height, encoded)];
                return new CompressedTextureData(width, height, format, isSRgb, singleMip);
            }

            var mipChain = GenerateMipChain(rgbaPixels, width, height);
            List<CompressedMipLevel> mips = new(mipChain.Count);

            for (int i = 0; i < mipChain.Count; i++)
            {
                var (mW, mH, mPixels) = mipChain[i];
                byte[] encoded = CompressLevel(mPixels, mW, mH, cmpFormat, optPtr);
                mips.Add(new CompressedMipLevel(mW, mH, encoded));
            }

            return new CompressedTextureData(width, height, format, isSRgb, mips);
        }
        finally
        {
            Marshal.FreeHGlobal(optPtr);
        }
    }

    private static byte[] CompressLevel(byte[] rgbaPixels, int width, int height, CMP_FORMAT cmpFormat, IntPtr optPtr)
    {
        GCHandle srcHandle = GCHandle.Alloc(rgbaPixels, GCHandleType.Pinned);
        IntPtr destPtr = IntPtr.Zero;
        CMP_Texture? srcTex = null;
        CMP_Texture? destTex = null;

        try
        {
            srcTex = new CMP_Texture
            {
                size = (uint)Marshal.SizeOf<CMP_Texture>(),
                width = (uint)width,
                height = (uint)height,
                pitch = (uint)(width * 4),
                format = CMP_FORMAT.RGBA_8888,
                dataSize = (uint)rgbaPixels.Length,
                data = srcHandle.AddrOfPinnedObject()
            };

            destTex = new CMP_Texture
            {
                size = (uint)Marshal.SizeOf<CMP_Texture>(),
                width = (uint)width,
                height = (uint)height,
                pitch = 0,
                format = cmpFormat
            };

            destTex.dataSize = CompressonatorNativeMethods.CMP_CalculateBufferSize(destTex);
            destPtr = Marshal.AllocHGlobal((int)destTex.dataSize);
            destTex.data = destPtr;

            CMP_ERROR err = CompressonatorNativeMethods.CMP_ConvertTexture(srcTex, destTex, optPtr, IntPtr.Zero);
            if (err != CMP_ERROR.CMP_OK)
            {
                throw new InvalidOperationException($"Compressonator failed to compress {width}x{height} texture to {cmpFormat}: {err}");
            }

            byte[] encoded = new byte[destTex.dataSize];
            Marshal.Copy(destTex.data, encoded, 0, (int)destTex.dataSize);
            return encoded;
        }
        finally
        {
            if (srcTex != null)
            {
                srcTex.data = IntPtr.Zero;
                GC.SuppressFinalize(srcTex);
            }

            if (destTex != null)
            {
                destTex.data = IntPtr.Zero;
                GC.SuppressFinalize(destTex);
            }

            if (destPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(destPtr);
            }

            srcHandle.Free();
        }
    }

    private static List<(int width, int height, byte[] pixels)> GenerateMipChain(byte[] srcPixels, int width, int height)
    {
        var mips = new List<(int width, int height, byte[] pixels)> { (width, height, srcPixels) };
        int curW = width;
        int curH = height;
        byte[] prev = srcPixels;

        while (curW > 1 || curH > 1)
        {
            int nextW = Math.Max(1, curW / 2);
            int nextH = Math.Max(1, curH / 2);
            byte[] next = new byte[nextW * nextH * 4];

            for (int y = 0; y < nextH; y++)
            {
                int sy0 = Math.Min(y * 2, curH - 1);
                int sy1 = Math.Min(y * 2 + 1, curH - 1);
                int row0 = sy0 * curW * 4;
                int row1 = sy1 * curW * 4;
                int dstRow = y * nextW * 4;

                for (int x = 0; x < nextW; x++)
                {
                    int sx0 = Math.Min(x * 2, curW - 1) * 4;
                    int sx1 = Math.Min(x * 2 + 1, curW - 1) * 4;
                    int dstIdx = dstRow + x * 4;

                    int p00 = row0 + sx0;
                    int p10 = row0 + sx1;
                    int p01 = row1 + sx0;
                    int p11 = row1 + sx1;

                    next[dstIdx + 0] = (byte)((prev[p00 + 0] + prev[p10 + 0] + prev[p01 + 0] + prev[p11 + 0] + 2) >> 2);
                    next[dstIdx + 1] = (byte)((prev[p00 + 1] + prev[p10 + 1] + prev[p01 + 1] + prev[p11 + 1] + 2) >> 2);
                    next[dstIdx + 2] = (byte)((prev[p00 + 2] + prev[p10 + 2] + prev[p01 + 2] + prev[p11 + 2] + 2) >> 2);
                    next[dstIdx + 3] = (byte)((prev[p00 + 3] + prev[p10 + 3] + prev[p01 + 3] + prev[p11 + 3] + 2) >> 2);
                }
            }

            mips.Add((nextW, nextH, next));
            prev = next;
            curW = nextW;
            curH = nextH;
        }

        return mips;
    }

    private static CMP_FORMAT MapFormat(TextureCompressionFormat format) => format switch
    {
        TextureCompressionFormat.BC1 => CMP_FORMAT.BC1,
        TextureCompressionFormat.BC3 => CMP_FORMAT.BC3,
        TextureCompressionFormat.BC4 => CMP_FORMAT.BC4,
        TextureCompressionFormat.BC5 => CMP_FORMAT.BC5,
        TextureCompressionFormat.BC7 => CMP_FORMAT.BC7,
        _ => CMP_FORMAT.BC7
    };
}
