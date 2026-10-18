using System.Runtime.InteropServices;

namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Reads standard DirectDraw Surface (.dds) files directly into GPU-compatible compressed texture data
/// without CPU decompression or transcoding.
/// </summary>
public static class DdsReader
{
    private const uint DdsMagic = 0x20534444;      // "DDS "
    private const uint DdsFourCC_DXT1 = 0x31545844; // "DXT1"
    private const uint DdsFourCC_DXT3 = 0x33545844; // "DXT3"
    private const uint DdsFourCC_DXT5 = 0x35545844; // "DXT5"
    private const uint DdsFourCC_ATI1 = 0x31495441; // "ATI1" (BC4)
    private const uint DdsFourCC_BC4U = 0x55344342; // "BC4U"
    private const uint DdsFourCC_ATI2 = 0x32495441; // "ATI2" (BC5)
    private const uint DdsFourCC_BC5U = 0x55354342; // "BC5U"
    private const uint DdsFourCC_DX10 = 0x30315844; // "DX10"

    /// <summary>
    /// Reads a compressed DDS file from disk into a CompressedTextureData payload.
    /// </summary>
    public static CompressedTextureData Read(string filePath, bool? isSRgb = null)
    {
        using var stream = File.OpenRead(filePath);
        return Read(stream, isSRgb);
    }

    /// <summary>
    /// Reads a compressed DDS stream into a CompressedTextureData payload.
    /// </summary>
    public static CompressedTextureData Read(Stream stream, bool? isSRgb = null)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        uint magic = reader.ReadUInt32();
        if (magic != DdsMagic)
            throw new InvalidDataException("Invalid DDS magic identifier.");

        uint headerSize = reader.ReadUInt32();
        if (headerSize != 124)
            throw new InvalidDataException($"Invalid DDS header size: {headerSize}");

        uint flags = reader.ReadUInt32();
        int height = (int)reader.ReadUInt32();
        int width = (int)reader.ReadUInt32();
        uint pitchOrLinearSize = reader.ReadUInt32();
        uint depth = reader.ReadUInt32();
        int mipMapCount = (int)reader.ReadUInt32();
        if (mipMapCount <= 0)
            mipMapCount = 1;

        // Skip 11 reserved uints (44 bytes)
        reader.BaseStream.Seek(44, SeekOrigin.Current);

        // DDS_PIXELFORMAT (32 bytes)
        uint pfSize = reader.ReadUInt32();
        uint pfFlags = reader.ReadUInt32();
        uint pfFourCC = reader.ReadUInt32();
        uint pfRGBBitCount = reader.ReadUInt32();
        uint pfRBitMask = reader.ReadUInt32();
        uint pfGBitMask = reader.ReadUInt32();
        uint pfBBitMask = reader.ReadUInt32();
        uint pfABitMask = reader.ReadUInt32();

        // Caps (20 bytes: dwCaps, dwCaps2, dwCaps3, dwCaps4, dwReserved2)
        reader.BaseStream.Seek(20, SeekOrigin.Current);

        TextureCompressionFormat format = TextureCompressionFormat.BC7;
        bool srgb = isSRgb ?? true;
        int blockSize = 16;

        if (pfFourCC == DdsFourCC_DX10)
        {
            uint dxgiFormat = reader.ReadUInt32();
            uint resourceDimension = reader.ReadUInt32();
            uint miscFlag = reader.ReadUInt32();
            uint arraySize = reader.ReadUInt32();
            uint miscFlags2 = reader.ReadUInt32();

            switch (dxgiFormat)
            {
                case 70: // BC1_TYPELESS
                case 71: // BC1_UNORM
                    format = TextureCompressionFormat.BC1;
                    blockSize = 8;
                    srgb = isSRgb ?? false;
                    break;
                case 72: // BC1_UNORM_SRGB
                    format = TextureCompressionFormat.BC1;
                    blockSize = 8;
                    srgb = isSRgb ?? true;
                    break;
                case 73: // BC2_TYPELESS
                case 74: // BC2_UNORM
                    format = TextureCompressionFormat.BC2;
                    blockSize = 16;
                    srgb = isSRgb ?? false;
                    break;
                case 75: // BC2_UNORM_SRGB
                    format = TextureCompressionFormat.BC2;
                    blockSize = 16;
                    srgb = isSRgb ?? true;
                    break;
                case 76: // BC3_TYPELESS
                case 77: // BC3_UNORM
                    format = TextureCompressionFormat.BC3;
                    blockSize = 16;
                    srgb = isSRgb ?? false;
                    break;
                case 78: // BC3_UNORM_SRGB
                    format = TextureCompressionFormat.BC3;
                    blockSize = 16;
                    srgb = isSRgb ?? true;
                    break;
                case 79: // BC4_TYPELESS
                case 80: // BC4_UNORM
                case 81: // BC4_SNORM
                    format = TextureCompressionFormat.BC4;
                    blockSize = 8;
                    srgb = false;
                    break;
                case 82: // BC5_TYPELESS
                case 83: // BC5_UNORM
                case 84: // BC5_SNORM
                    format = TextureCompressionFormat.BC5;
                    blockSize = 16;
                    srgb = false;
                    break;
                case 94: // BC7_TYPELESS
                case 98: // BC7_UNORM
                    format = TextureCompressionFormat.BC7;
                    blockSize = 16;
                    srgb = isSRgb ?? false;
                    break;
                case 99: // BC7_UNORM_SRGB
                    format = TextureCompressionFormat.BC7;
                    blockSize = 16;
                    srgb = isSRgb ?? true;
                    break;
                default:
                    format = TextureCompressionFormat.BC7;
                    blockSize = 16;
                    break;
            }
        }
        else
        {
            switch (pfFourCC)
            {
                case DdsFourCC_DXT1:
                    format = TextureCompressionFormat.BC1;
                    blockSize = 8;
                    break;
                case DdsFourCC_DXT3:
                    format = TextureCompressionFormat.BC2;
                    blockSize = 16;
                    break;
                case DdsFourCC_DXT5:
                    format = TextureCompressionFormat.BC3;
                    blockSize = 16;
                    break;
                case DdsFourCC_ATI1:
                case DdsFourCC_BC4U:
                    format = TextureCompressionFormat.BC4;
                    blockSize = 8;
                    srgb = false;
                    break;
                case DdsFourCC_ATI2:
                case DdsFourCC_BC5U:
                    format = TextureCompressionFormat.BC5;
                    blockSize = 16;
                    srgb = false;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported DDS FourCC format: 0x{pfFourCC:X8}");
            }
        }

        List<CompressedMipLevel> mips = new(mipMapCount);
        int curW = width;
        int curH = height;

        for (int i = 0; i < mipMapCount; i++)
        {
            int blocksW = Math.Max(1, (curW + 3) / 4);
            int blocksH = Math.Max(1, (curH + 3) / 4);
            int mipBytes = blocksW * blocksH * blockSize;

            byte[] data = reader.ReadBytes(mipBytes);
            if (data.Length < mipBytes)
                throw new EndOfStreamException($"Unexpected end of stream while reading DDS mip level {i}. Expected {mipBytes} bytes, got {data.Length}.");

            mips.Add(new CompressedMipLevel(curW, curH, data));

            curW = Math.Max(1, curW / 2);
            curH = Math.Max(1, curH / 2);
        }

        return new CompressedTextureData(width, height, format, srgb, mips);
    }
}
