using Phoenix.Framework.Rendering.RT;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Rendering
{
    public class RenderViewport
    {
        public Vector2 Size { 
            get => _game.WindowSize * Scale;
            set
            {
                if (value.X < 0 || value.Y < 0)
                    return;

                Scale = value / _game.WindowSize;
            }
        }
        public float Width => Size.X;

        public float Height => Size.Y;

        public Vector2 Scale
        {
            get;
            set
            {
                if (value.X < 0 || value.X > 1 || value.Y < 0 || value.Y > 1)
                    return;

                field = value;
                _game.RTManager.HandleWindowResize();
            }
        } = Vector2.One;

        public BlitFramebufferFilter Filter { get; set; } = BlitFramebufferFilter.Linear;
        private PhoenixGame _game;
        
        internal RenderViewport(PhoenixGame game)
        {
            _game = game;
                
        }

        
    }
}
