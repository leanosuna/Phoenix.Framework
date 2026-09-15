using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Primitives;

/// <summary>
/// Configuration options controlling procedurally generated primitive vertex layouts, normals, UVs, and topology.
/// </summary>
public abstract class PrimitiveInfo
{
    public PrimitiveTopology MeshPrimitiveType { get; set; } = PrimitiveTopology.TriangleList;
    public bool Uv { get; set; }
    public bool Normals { get; set; }
    public bool Tangents { get; set; }
    public bool Bitangents { get; set; }
    public bool SaveVertices { get; set; }
}