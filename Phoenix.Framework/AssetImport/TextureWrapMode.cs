using System.Text.Json.Serialization;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Texture coordinate wrapping behavior outside the [0, 1] range.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextureWrapMode
{
    Repeat,
    MirroredRepeat,
    ClampToEdge,
    ClampToBorder
}
