using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Rendering.RT
{
    public class RenderTextureInfo
    {
        public bool FollowsWindowSize { get; internal set; } = true;
        public Vector2 SizeMultiplier { get; internal set; } = Vector2.One;
        public Vector2 Size { get; internal set; } = Vector2.Zero;
        public InternalFormat Format { get; internal set; } = InternalFormat.Rgba;
        public GLEnum WrapS { get; internal set; } = GLEnum.ClampToEdge;
        public GLEnum WrapT { get; internal set; } = GLEnum.ClampToEdge;
        public GLEnum MinFilter { get; internal set; } = GLEnum.Linear;
        public GLEnum MagFilter { get; internal set; } = GLEnum.Linear;


        public RenderTextureInfo()
        {
            
        }

    }
}
