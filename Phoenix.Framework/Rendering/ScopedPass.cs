namespace Phoenix.Framework.Rendering;

/// <summary>
/// Scoped wrapper that automatically ends a dynamic rendering pass upon disposal.
/// </summary>
public readonly ref struct ScopedPass
{
    private readonly RenderContext _context;

    /// <summary>
    /// Initializes a scoped rendering pass wrapper.
    /// </summary>
    internal ScopedPass(RenderContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ends the dynamic rendering pass upon scope exit.
    /// </summary>
    public void Dispose()
    {
        _context.EndPass();
    }
}
