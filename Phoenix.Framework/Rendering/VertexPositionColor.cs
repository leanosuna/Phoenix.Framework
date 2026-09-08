using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionColor
{
    public Vector3 Position;
    public Vector4 Color;

    /// <summary>
    /// Initializes a vertex with position and color components.
    /// </summary>
    public VertexPositionColor(Vector3 position, Vector4 color)
    {
        Position = position;
        Color = color;
    }

    /// <summary>
    /// Returns the vertex input binding description for pipeline configuration.
    /// </summary>
    public static VertexInputBindingDescription GetBindingDescription(uint binding = 0)
    {
        return new VertexInputBindingDescription
        {
            Binding = binding,
            Stride = (uint)Marshal.SizeOf<VertexPositionColor>(),
            InputRate = VertexInputRate.Vertex
        };
    }

    /// <summary>
    /// Returns the vertex input attribute descriptions for position and color vertex attributes.
    /// </summary>
    public static VertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0)
    {
        return
        [
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<VertexPositionColor>(nameof(Position))
            },
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 1,
                Format = Format.R32G32B32A32Sfloat,
                Offset = (uint)Marshal.OffsetOf<VertexPositionColor>(nameof(Color))
            }
        ];
    }
}
