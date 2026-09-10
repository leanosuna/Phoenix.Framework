using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionColorTexture
{
    public Vector3 Position;
    public Vector4 Color;
    public Vector2 TexCoord;

    /// <summary>
    /// Initializes a vertex with position, color, and texture coordinate components.
    /// </summary>
    public VertexPositionColorTexture(Vector3 position, Vector4 color, Vector2 texCoord)
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
    }

    /// <summary>
    /// Returns the vertex input binding description for pipeline configuration.
    /// </summary>
    public static VertexInputBindingDescription GetBindingDescription(uint binding = 0)
    {
        return new VertexInputBindingDescription
        {
            Binding = binding,
            Stride = (uint)Marshal.SizeOf<VertexPositionColorTexture>(),
            InputRate = VertexInputRate.Vertex
        };
    }

    /// <summary>
    /// Returns the vertex input attribute descriptions for position, color, and texture coordinate vertex attributes.
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
                Offset = (uint)Marshal.OffsetOf<VertexPositionColorTexture>(nameof(Position))
            },
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 1,
                Format = Format.R32G32B32A32Sfloat,
                Offset = (uint)Marshal.OffsetOf<VertexPositionColorTexture>(nameof(Color))
            },
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<VertexPositionColorTexture>(nameof(TexCoord))
            }
        ];
    }
}
