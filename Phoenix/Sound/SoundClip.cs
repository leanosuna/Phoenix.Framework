using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Sound
{
    public sealed class SoundClip
    {
        internal uint BufferId { get; }

        internal SoundClip(uint bufferId)
        {
            BufferId = bufferId;
        }
    }
}
