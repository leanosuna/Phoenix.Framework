using System.Text.Json.Serialization;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Texture magnification, minification, and mipmap interpolation filter modes.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextureFilterMode
{
    Linear,
    Nearest
}
