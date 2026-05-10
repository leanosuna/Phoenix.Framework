using Silk.NET.OpenGL;
using Phoenix.Framework.Rendering.Textures;
using System.Numerics;

namespace Phoenix.Framework.Rendering.RT
{
    public class RenderTexture
    {
        public bool FollowsWindowSize { get; private set; }

        public Vector2 SizeMultiplier { get; private set; }
        public Vector2 Size
        {
            get;
            internal set;
        }

        public float Width => Size.X;
        public float Height => Size.Y;

        internal GLTexture Texture = default!;
        
        internal RenderTexture(GLTexture tex, RenderTextureInfo ti)
        {
            Texture = tex;
            FollowsWindowSize = ti.FollowsWindowSize;
            SizeMultiplier = ti.SizeMultiplier;
            Size = ti.Size;
        }
        
        internal RenderTexture(PhoenixGame game)
        {
            var ti = new RenderTextureInfo();
            var tex = new GLTexture(game.GL, ti);

            Texture = tex;
            FollowsWindowSize = ti.FollowsWindowSize;
            SizeMultiplier = ti.SizeMultiplier;
            Size = game.WindowSize;
        }

        public static implicit operator uint(RenderTexture target)
        {
            return target.Texture.Handle;
        }
    }
}
