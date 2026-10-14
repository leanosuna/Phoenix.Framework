using Phoenix.Framework.Rendering.Geometry.Model;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Event arguments supplied when a 3D model asset has been recompiled or reloaded at runtime.
/// </summary>
public sealed class ModelReloadEventArgs : EventArgs
{
    public string RelativePath { get; }
    public Model? ReloadedModel { get; }

    public ModelReloadEventArgs(string relativePath, Model? reloadedModel)
    {
        RelativePath = relativePath;
        ReloadedModel = reloadedModel;
    }
}
