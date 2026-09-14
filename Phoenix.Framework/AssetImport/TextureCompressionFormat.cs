using System.Text.Json.Serialization;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Specifies the hardware block compression format for 2D textures.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextureCompressionFormat
{
    None = 0,
    BC1 = 1,
    BC3 = 2,
    BC4 = 3,
    BC5 = 4,
    BC7 = 5
}
