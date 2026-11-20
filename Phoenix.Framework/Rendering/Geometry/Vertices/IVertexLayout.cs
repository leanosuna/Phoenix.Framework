using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Geometry.Vertices;

/// <summary>
/// Defines static abstract vertex layout contract for pipeline creation.
/// </summary>
public interface IVertexLayout
{
    static abstract VertexInputBindingDescription GetBindingDescription(uint binding = 0);
    static abstract VertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0);
}
