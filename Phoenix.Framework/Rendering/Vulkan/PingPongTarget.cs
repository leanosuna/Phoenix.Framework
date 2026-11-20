using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Wraps two VulkanRenderTargets for iterative ping-pong post-processing passes (blur, bloom, accumulation).
/// </summary>
public sealed class PingPongTarget : IDisposable
{
    private readonly Graphics _graphics;
    private readonly VulkanRenderTarget _a;
    private readonly VulkanRenderTarget _b;
    private readonly bool _screenRelative;
    private bool _flipped;
    private bool _disposed;

    /// <summary>
    /// Gets the current read target containing the output of previous passes.
    /// </summary>
    public VulkanRenderTarget Read => _flipped ? _b : _a;

    /// <summary>
    /// Gets the current write target to render the next iteration into.
    /// </summary>
    public VulkanRenderTarget Write => _flipped ? _a : _b;

    /// <summary>
    /// Gets bindless texture slot ID of the primary color texture in the current read target.
    /// </summary>
    public uint ReadTextureId => Read.TextureId;

    /// <summary>
    /// Gets bindless texture slot ID of the primary color texture in the current write target.
    /// </summary>
    public uint WriteTextureId => Write.TextureId;

    /// <summary>
    /// Creates a screen-relative ping-pong target that automatically resizes with viewport scaling.
    /// </summary>
    public PingPongTarget(Graphics graphics, float scale = 1.0f, Format colorFormat = Format.R16G16B16A16Sfloat, bool hasDepth = false, SamplerDescription? samplerDesc = null)
    {
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _screenRelative = true;
        _a = graphics.CreateScreenRenderTarget([colorFormat], hasDepth, scale, samplerDesc);
        _b = graphics.CreateScreenRenderTarget([colorFormat], hasDepth, scale, samplerDesc);
    }

    /// <summary>
    /// Creates an explicit pixel-sized ping-pong target.
    /// </summary>
    public PingPongTarget(Graphics graphics, uint width, uint height, Format colorFormat = Format.R16G16B16A16Sfloat, bool hasDepth = false, SamplerDescription? samplerDesc = null)
    {
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _screenRelative = false;
        _a = new VulkanRenderTarget(graphics.Context, graphics.BindlessManager, width, height, colorFormat, hasDepth, samplerDesc: samplerDesc);
        _b = new VulkanRenderTarget(graphics.Context, graphics.BindlessManager, width, height, colorFormat, hasDepth, samplerDesc: samplerDesc);
    }

    /// <summary>
    /// Exchanges read and write targets for the next pass.
    /// </summary>
    public void Swap() => _flipped = !_flipped;

    /// <summary>
    /// Explicitly resizes both underlying render targets.
    /// </summary>
    public void Resize(uint width, uint height)
    {
        _a.Resize(width, height);
        _b.Resize(width, height);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_screenRelative)
        {
            _graphics.UnregisterScreenRenderTarget(_a);
            _graphics.UnregisterScreenRenderTarget(_b);
        }

        _a.Dispose();
        _b.Dispose();
        _disposed = true;
    }
}
