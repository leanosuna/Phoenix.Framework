using Phoenix.Framework.Rendering.Vulkan;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Event arguments supplied when a 2D texture asset has been reloaded or re-uploaded to GPU memory at runtime.
/// </summary>
public sealed class TextureReloadEventArgs : EventArgs
{
    public string RelativePath { get; }
    public VulkanTexture? ReloadedTexture { get; }

    public TextureReloadEventArgs(string relativePath, VulkanTexture? reloadedTexture)
    {
        RelativePath = relativePath;
        ReloadedTexture = reloadedTexture;
    }
}
