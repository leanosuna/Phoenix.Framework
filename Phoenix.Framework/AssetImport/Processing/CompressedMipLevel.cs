namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Represents the dimensions and encoded block payload for a single compressed mipmap level.
/// </summary>
public readonly record struct CompressedMipLevel(int Width, int Height, byte[] Data);
