namespace Phoenix.Framework.Rendering.Geometry.Model;

/// <summary>
/// Groups a collection of submeshes comprising a named part of a 3D model.
/// </summary>
public sealed class ModelPart : IDisposable
{
    private bool _disposed;

    public string Name { get; }
    public List<ModelMesh> Meshes { get; }

    public ModelPart(string name, List<ModelMesh> meshes)
    {
        Name = name;
        Meshes = meshes;
    }

    /// <summary>
    /// Renders all meshes belonging to this model part.
    /// </summary>
    public void Draw(RenderContext rc)
    {
        for (int i = 0; i < Meshes.Count; i++)
        {
            Meshes[i].Draw(rc);
        }
    }

    /// <summary>
    /// Disposes all child meshes and their GPU resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        for (int i = 0; i < Meshes.Count; i++)
        {
            Meshes[i].Dispose();
        }

        _disposed = true;
    }
}
