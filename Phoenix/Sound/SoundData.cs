using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Sound
{
    using Silk.NET.OpenAL;

    public sealed class SoundData
    {
        public byte[] PCM { get; }
        public BufferFormat Format { get; }
        public int SampleRate { get; }

        public SoundData(byte[] pcm, BufferFormat format, int sampleRate)
        {
            PCM = pcm;
            Format = format;
            SampleRate = sampleRate;
        }
    }
}
