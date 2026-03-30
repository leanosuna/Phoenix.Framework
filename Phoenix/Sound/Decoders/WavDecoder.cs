using Silk.NET.OpenAL;
using System.Text;

namespace Phoenix.Sound.Decoders
{
    public sealed class WavDecoder : IAudioDecoder
    {
        public bool CanDecode(string path)
            => Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase);

        public SoundData Decode(string path)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: false);

            string riff = new string(br.ReadChars(4));
            if (riff != "RIFF")
                throw new InvalidDataException("Invalid WAV: Missing RIFF header.");

            br.ReadInt32(); // file size

            string wave = new string(br.ReadChars(4));
            if (wave != "WAVE")
                throw new InvalidDataException("Invalid WAV: Missing WAVE header.");

            short channels = 0;
            int sampleRate = 0;
            short bitsPerSample = 0;
            byte[]? pcmData = null;

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                string chunkId = new string(br.ReadChars(4));
                int chunkSize = br.ReadInt32();

                switch (chunkId)
                {
                    case "fmt ":
                        {
                            short audioFormat = br.ReadInt16();
                            channels = br.ReadInt16();
                            sampleRate = br.ReadInt32();
                            br.ReadInt32(); // byteRate
                            br.ReadInt16(); // blockAlign
                            bitsPerSample = br.ReadInt16();

                            if (audioFormat != 1)
                                throw new NotSupportedException("Only PCM WAV is supported.");

                            // skip any extra fmt bytes
                            if (chunkSize > 16)
                                br.BaseStream.Position += (chunkSize - 16);
                            break;
                        }

                    case "data":
                        {
                            pcmData = br.ReadBytes(chunkSize);
                            break;
                        }

                    default:
                        {
                            br.BaseStream.Position += chunkSize;
                            break;
                        }
                }

                // WAV chunks are word-aligned
                if ((chunkSize & 1) != 0)
                    br.BaseStream.Position += 1;
            }

            if (pcmData == null)
                throw new InvalidDataException("Invalid WAV: Missing data chunk.");

            BufferFormat format = GetFormat(channels, bitsPerSample);

            return new SoundData(pcmData, format, sampleRate);
        }

        private static BufferFormat GetFormat(short channels, short bitsPerSample)
        {
            return (channels, bitsPerSample) switch
            {
                (1, 8) => BufferFormat.Mono8,
                (1, 16) => BufferFormat.Mono16,
                (2, 8) => BufferFormat.Stereo8,
                (2, 16) => BufferFormat.Stereo16,
                _ => throw new NotSupportedException(
                    $"Unsupported WAV format: {channels} channels, {bitsPerSample} bits.")
            };
        }
    }
}
