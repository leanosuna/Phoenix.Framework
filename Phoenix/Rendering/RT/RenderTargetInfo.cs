using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.Rendering.RT
{
    public class RenderTargetInfo
    {
        public string Name { get; internal set; } = default!;
        public Vector2 Size { get; internal set; } = default!;
        public List<RenderTexture> Textures { get; internal set; } = new List<RenderTexture>();
        public DepthBuffer DepthBuffer { get; internal set; } = default!;

        public RenderTargetInfo()
        {

        }
    }
}
