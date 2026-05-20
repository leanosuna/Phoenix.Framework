using Phoenix.Framework.AssetImport;
using Phoenix.Framework.Sound;
using Silk.NET.OpenAL;
using System.Numerics;

namespace Phoenix.Framework.Video
{
    internal static class BinaryVideoReader
    {
        private const long EagerLoadThreshold = 512L * 1024 * 1024;

        public static VideoData Load(string path)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            var magic = br.ReadString();
            if (magic != "PHXV")
                throw new Exception($"Invalid video format: expected PHXV, got {magic}");

            var version = br.ReadUInt32();

            var width = br.ReadInt32();
            var height = br.ReadInt32();
            var frameCount = br.ReadInt32();
            var frameRate = br.ReadSingle();
            var compressionFormat = br.ReadByte();
            var generateMipMaps = br.ReadBoolean();
            var hasAudio = br.ReadBoolean();

            SoundData? audio = null;
            if (hasAudio)
            {
                var audioSampleRate = br.ReadInt32();
                var audioChannels = br.ReadInt16();
                var audioBitsPerSample = br.ReadInt16();
                var audioDataLength = br.ReadInt32();
                var audioPCM = br.ReadBytes(audioDataLength);

                var format = GetBufferFormat(audioChannels, audioBitsPerSample);
                audio = new SoundData(audioPCM, format, audioSampleRate);
            }

            var frameIndexOffset = br.ReadInt32();
            var indexFrameCount = br.ReadInt32();

            var frameOffsets = new int[indexFrameCount];
            var frameLengths = new int[indexFrameCount];
            long totalFrameDataSize = 0;

            for (int i = 0; i < indexFrameCount; i++)
            {
                frameOffsets[i] = br.ReadInt32();
                frameLengths[i] = br.ReadInt32();
                totalFrameDataSize += frameLengths[i];
            }

            var mipCount = generateMipMaps ? CalculateMipCount(width, height) : 1;

            if (totalFrameDataSize < EagerLoadThreshold)
            {
                return LoadEager(br, fs, path, width, height, frameCount, frameRate,
                    compressionFormat, generateMipMaps, hasAudio, audio,
                    frameOffsets, frameLengths, mipCount);
            }
            else
            {
                return new VideoData(width, height, frameCount, frameRate,
                    compressionFormat, generateMipMaps, hasAudio, audio,
                    path, frameOffsets, frameLengths, mipCount);
            }
        }

        private static VideoData LoadEager(BinaryReader br, FileStream fs, string path,
            int width, int height, int frameCount, float frameRate,
            byte compressionFormat, bool generateMipMaps, bool hasAudio, SoundData? audio,
            int[] frameOffsets, int[] frameLengths, int mipCount)
        {
            var frames = new byte[frameCount][];
            int[][]? mipLengths = generateMipMaps ? new int[frameCount][] : null;
            Vector2[][]? mipSizes = generateMipMaps ? new Vector2[frameCount][] : null;

            for (int i = 0; i < frameCount; i++)
            {
                fs.Position = frameOffsets[i];
                frames[i] = br.ReadBytes(frameLengths[i]);

                if (generateMipMaps)
                {
                    using var ms = new MemoryStream(frames[i]);
                    using var mipBr = new BinaryReader(ms);

                    var firstMipLength = mipBr.ReadInt32();
                    var mipData = new List<byte>(firstMipLength);
                    mipData.AddRange(mipBr.ReadBytes(firstMipLength));

                    var mips = CalculateMipCount(width, height);
                    var mipLens = new int[mips];
                    var mipSz = new Vector2[mips];
                    mipLens[0] = firstMipLength;
                    mipSz[0] = new Vector2(width, height);

                    for (int m = 1; m < mips; m++)
                    {
                        mipLens[m] = mipBr.ReadInt32();
                        mipSz[m].X = mipBr.ReadInt32();
                        mipSz[m].Y = mipBr.ReadInt32();
                        var mipBytes = mipBr.ReadBytes(mipLens[m]);
                        mipData.AddRange(mipBytes);
                    }

                    mipLengths[i] = mipLens;
                    mipSizes[i] = mipSz;
                    frames[i] = mipData.ToArray();
                }
            }

            return new VideoData(width, height, frameCount, frameRate,
                compressionFormat, generateMipMaps, hasAudio, audio,
                frames, mipLengths, mipSizes, mipCount);
        }

        private static int CalculateMipCount(int width, int height)
        {
            return (int)Math.Floor(Math.Log2(Math.Max(width, height))) + 1;
        }

        private static BufferFormat GetBufferFormat(short channels, short bitsPerSample)
        {
            return (channels, bitsPerSample) switch
            {
                (1, 8) => BufferFormat.Mono8,
                (1, 16) => BufferFormat.Mono16,
                (2, 8) => BufferFormat.Stereo8,
                (2, 16) => BufferFormat.Stereo16,
                _ => throw new NotSupportedException($"Unsupported audio format: {channels} channels, {bitsPerSample} bits.")
            };
        }
    }
}
