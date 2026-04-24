using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering.RT
{
    public class RenderTarget
    {
        public string Name { get; private set; } = default!;
        public uint FrameBuffer
        {
            get;
            internal set;
        } = uint.MaxValue;
        internal bool IsBound => FrameBuffer != uint.MaxValue;
        public RenderTexture[] RenderTextures { get; internal set; } = default!;
        public int TexturesCount => RenderTextures.Length;

        private GL GL = default!;
        public DepthBuffer DepthBuffer { get; internal set; } = default!;
        internal RenderTarget(GL gl, string name, uint handle, RenderTexture[] targets)
        {
            Name = name;
            GL = gl;
            FrameBuffer = handle;
            RenderTextures = targets;
        }
    }
}
