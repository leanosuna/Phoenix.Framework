using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Sound.Decoders
{
    public interface IAudioDecoder
    {
        bool CanDecode(string path);
        SoundData Decode(string path);
    }
}
