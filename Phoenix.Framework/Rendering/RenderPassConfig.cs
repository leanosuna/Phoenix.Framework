using System.Numerics;

namespace Phoenix.Framework.Rendering;

/// <summary>
/// Configuration specifying clear values, load/store actions, and depth storage for dynamic rendering passes.
/// </summary>
public readonly struct RenderPassConfig
{
    public Vector4? ClearColor { get; init; }
    public Vector4[]? ClearColors { get; init; }
    public float? ClearDepth { get; init; }
    public bool StoreDepth { get; init; }

    public RenderPassConfig()
    {
        ClearColor = null;
        ClearColors = null;
        ClearDepth = null;
        StoreDepth = true;
    }

    public static RenderPassConfig Clear(Vector4 color, float depth = 1.0f, bool storeDepth = true) => new()
    {
        ClearColor = color,
        ClearDepth = depth,
        StoreDepth = storeDepth
    };

    public static RenderPassConfig ClearMrt(Vector4[] colors, float depth = 1.0f, bool storeDepth = true) => new()
    {
        ClearColors = colors,
        ClearDepth = depth,
        StoreDepth = storeDepth
    };

    public static RenderPassConfig DepthOnly(float depth = 1.0f) => new()
    {
        ClearColor = null,
        ClearColors = null,
        ClearDepth = depth,
        StoreDepth = true
    };

    public static RenderPassConfig LoadAll(bool storeDepth = true) => new()
    {
        ClearColor = null,
        ClearColors = null,
        ClearDepth = null,
        StoreDepth = storeDepth
    };
}
