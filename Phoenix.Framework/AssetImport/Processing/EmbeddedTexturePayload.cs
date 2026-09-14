namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Holds raw in-memory texture payload extracted from an imported 3D scene before disk serialization.
/// </summary>
public sealed class EmbeddedTexturePayload
{
    public string BaseName { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public bool IsCompressed { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public byte[] Data { get; init; } = [];
}
