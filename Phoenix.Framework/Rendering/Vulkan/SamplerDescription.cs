using Phoenix.Framework.AssetImport;
using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Immutable descriptor defining sampler states for filtering, coordinate wrapping, and anisotropic filtering.
/// </summary>
public readonly record struct SamplerDescription(
    SamplerAddressMode AddressModeU,
    SamplerAddressMode AddressModeV,
    SamplerAddressMode AddressModeW,
    Filter MinFilter,
    Filter MagFilter,
    SamplerMipmapMode MipmapMode,
    bool AnisotropyEnable,
    float MaxAnisotropy)
{
    public static SamplerDescription FromTextureOptions(TextureLoadOptions options)
    {
        var addressU = MapWrapMode(options.WrapU);
        var addressV = MapWrapMode(options.WrapV);
        var minFilter = options.MinFilter == TextureFilterMode.Nearest ? Filter.Nearest : Filter.Linear;
        var magFilter = options.MagFilter == TextureFilterMode.Nearest ? Filter.Nearest : Filter.Linear;
        var mipmapMode = options.MipmapMode == TextureFilterMode.Nearest ? SamplerMipmapMode.Nearest : SamplerMipmapMode.Linear;
        bool anisoEnable = options.Anisotropic > 1.0f;
        float maxAniso = anisoEnable ? MathF.Min(options.Anisotropic, 16.0f) : 1.0f;

        return new SamplerDescription(
            addressU,
            addressV,
            SamplerAddressMode.Repeat,
            minFilter,
            magFilter,
            mipmapMode,
            anisoEnable,
            maxAniso);
    }

    private static SamplerAddressMode MapWrapMode(TextureWrapMode mode) => mode switch
    {
        TextureWrapMode.Repeat => SamplerAddressMode.Repeat,
        TextureWrapMode.MirroredRepeat => SamplerAddressMode.MirroredRepeat,
        TextureWrapMode.ClampToEdge => SamplerAddressMode.ClampToEdge,
        TextureWrapMode.ClampToBorder => SamplerAddressMode.ClampToBorder,
        _ => SamplerAddressMode.Repeat
    };
}
