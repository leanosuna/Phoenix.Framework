using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Model;

/// <summary>
/// Vertex structure for static and animated 3D models supporting positions, texture coordinates, normals, tangents, bitangents, bone IDs, and bone weights.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ModelVertex
{
    public Vector3 Position;
    public Vector2 TexCoords;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Vector3 Bitangent;
    public Vector4 BoneIds;
    public Vector4 Weights;

    public ModelVertex(Vector3 position, Vector2 texCoords, Vector3 normal, Vector3 tangent = default,
        Vector3 bitangent = default, Vector4 boneIds = default, Vector4 weights = default)
    {
        Position = position;
        TexCoords = texCoords;
        Normal = normal;
        Tangent = tangent;
        Bitangent = bitangent;
        BoneIds = boneIds;
        Weights = weights;
    }

    /// <summary>
    /// Gets the vertex input binding description for pipeline creation.
    /// </summary>
    public static VertexInputBindingDescription GetBindingDescription(uint binding = 0)
    {
        return new VertexInputBindingDescription
        {
            Binding = binding,
            Stride = (uint)Marshal.SizeOf<ModelVertex>(),
            InputRate = VertexInputRate.Vertex
        };
    }

    /// <summary>
    /// Gets the vertex input attribute descriptions for shader locations 0 through 6.
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
                Offset = (uint)Marshal.OffsetOf<ModelVertex>(nameof(Position))
            },
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 1,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<ModelVertex>(nameof(TexCoords))
            },
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 2,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<ModelVertex>(nameof(Normal))
            },
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 3,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<ModelVertex>(nameof(Tangent))
            },
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 4,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<ModelVertex>(nameof(Bitangent))
            },
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 5,
                Format = Format.R32G32B32A32Sfloat,
                Offset = (uint)Marshal.OffsetOf<ModelVertex>(nameof(BoneIds))
            },
            new VertexInputAttributeDescription
            {
                Binding = binding,
                Location = 6,
                Format = Format.R32G32B32A32Sfloat,
                Offset = (uint)Marshal.OffsetOf<ModelVertex>(nameof(Weights))
            }
        ];
    }
}
