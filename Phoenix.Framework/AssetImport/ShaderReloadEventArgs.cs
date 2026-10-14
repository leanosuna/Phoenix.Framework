namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Event arguments supplied when a shader asset has been recompiled at runtime.
/// </summary>
public sealed class ShaderReloadEventArgs : EventArgs
{
    public string RelativePath { get; }

    public ShaderReloadEventArgs(string relativePath)
    {
        RelativePath = relativePath;
    }
}
