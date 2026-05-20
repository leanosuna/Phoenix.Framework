using Phoenix.Framework.Sound;
using System.Numerics;

namespace Phoenix.Framework.Video
{
    public class VideoData
    {
        public int Width { get; }
        public int Height { get; }
        public int FrameCount { get; }
        public float FrameRate { get; }
        public float Duration => FrameCount / FrameRate;
        public byte CompressionFormat { get; }
        public bool GenerateMipMaps { get; }
        public bool HasAudio { get; }
        public SoundData? Audio { get; }

        public byte[][] Frames { get; }
        public int[][]? MipLengths { get; }
        public Vector2[][]? MipSizes { get; }
        public int MipCount { get; }

        public string? FilePath { get; }
        public int[] FrameOffsets { get; } = Array.Empty<int>();
        public int[] FrameLengths { get; } = Array.Empty<int>();
        public bool IsStreaming { get; }

        public VideoData(int width, int height, int frameCount, float frameRate, 
            byte compressionFormat, bool generateMipMaps, bool hasAudio, SoundData? audio,
            byte[][] frames, int[][]? mipLengths, Vector2[][]? mipSizes, int mipCount)
        {
            Width = width;
            Height = height;
            FrameCount = frameCount;
            FrameRate = frameRate;
            CompressionFormat = compressionFormat;
            GenerateMipMaps = generateMipMaps;
            HasAudio = hasAudio;
            Audio = audio;
            Frames = frames;
            MipLengths = mipLengths;
            MipSizes = mipSizes;
            MipCount = mipCount;
            IsStreaming = false;
        }

        public VideoData(int width, int height, int frameCount, float frameRate,
            byte compressionFormat, bool generateMipMaps, bool hasAudio, SoundData? audio,
            string filePath, int[] frameOffsets, int[] frameLengths, int mipCount)
        {
            Width = width;
            Height = height;
            FrameCount = frameCount;
            FrameRate = frameRate;
            CompressionFormat = compressionFormat;
            GenerateMipMaps = generateMipMaps;
            HasAudio = hasAudio;
            Audio = audio;
            Frames = Array.Empty<byte[]>();
            MipCount = mipCount;
            FilePath = filePath;
            FrameOffsets = frameOffsets;
            FrameLengths = frameLengths;
            IsStreaming = true;
        }
    }
}
