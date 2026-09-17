using Silk.NET.Vulkan;
using System.Numerics;

namespace Phoenix.Framework.Rendering;

/// <summary>
/// Controls internal rendering resolution scaling, dimension calculations, and upscaling filters.
/// </summary>
public sealed class RenderViewport
{
    private readonly PhoenixGame _game;
    private Vector2 _scale = Vector2.One;

    /// <summary>
    /// Gets or sets internal render resolution scale relative to the window framebuffer.
    /// Values are clamped between 0.05 and 4.0. Default is (1, 1).
    /// </summary>
    public Vector2 Scale
    {
        get => _scale;
        set
        {
            float x = Math.Clamp(value.X, 0.05f, 4.0f);
            float y = Math.Clamp(value.Y, 0.05f, 4.0f);
            Vector2 clamped = new(x, y);

            if (_scale == clamped)
                return;

            _scale = clamped;
            _game.Graphics.HandleViewportResize();
        }
    }

    /// <summary>
    /// Gets or sets uniform scale across both axes.
    /// </summary>
    public float UniformScale
    {
        get => _scale.X;
        set => Scale = new Vector2(value, value);
    }

    /// <summary>
    /// Gets effective rendering size in pixels.
    /// </summary>
    public Vector2 Size => new((float)Math.Max(1, (int)(_game.FramebufferWidth * _scale.X)),
                               (float)Math.Max(1, (int)(_game.FramebufferHeight * _scale.Y)));

    /// <summary>
    /// Gets effective rendering width in pixels.
    /// </summary>
    public uint Width => (uint)Math.Max(1, (int)Size.X);

    /// <summary>
    /// Gets effective rendering height in pixels.
    /// </summary>
    public uint Height => (uint)Math.Max(1, (int)Size.Y);

    /// <summary>
    /// Gets or sets hardware texture filtering applied when blitting the rendered scene to the swapchain.
    /// </summary>
    public Filter Filter { get; set; } = Filter.Linear;

    /// <summary>
    /// Gets or sets whether offscreen scene target scaling is enabled.
    /// When false, rendering passes write directly to the swapchain image.
    /// </summary>
    public bool Enabled { get; set; } = true;

    internal RenderViewport(PhoenixGame game)
    {
        _game = game;
    }
}
