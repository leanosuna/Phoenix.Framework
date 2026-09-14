namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Intermediate model part grouping one or more processed meshes.
/// </summary>
internal sealed class ProcessedPart
{
    public string Name { get; set; } = "";
    public List<ProcessedMesh> Meshes { get; set; } = [];
}
