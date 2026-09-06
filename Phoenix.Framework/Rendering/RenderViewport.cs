using Silk.NET.Vulkan;
using System.Numerics;

namespace Phoenix.Framework.Rendering
{
    public class RenderViewport
    {
        public Vector2 Size { 
            get => _game.FramebufferSize * Scale;
            set
            {
                if (value.X < 0 || value.Y < 0 ||
                    float.IsNaN(value.X) || float.IsNaN(value.Y) ||
                    float.IsInfinity(value.X) || float.IsInfinity(value.Y))
                    return;

                var fb = _game.FramebufferSize;
                if (fb.X <= 0 || fb.Y <= 0)
                    return;

                Scale = value / fb;
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
            }
        } = Vector2.One;

        public Filter Filter { get; set; } = Filter.Linear;
        private PhoenixGame _game;
        
        internal RenderViewport(PhoenixGame game)
        {
            _game = game;
                
        }

        
    }
}
