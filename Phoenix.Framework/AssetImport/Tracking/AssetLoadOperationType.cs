namespace Phoenix.Framework.AssetImport.Tracking;

/// <summary>
/// Categorizes an asset operation tracked during background loading.
/// </summary>
public enum AssetLoadOperationType
{
    Model,
    Texture,
    Shader,
    Audio
}
