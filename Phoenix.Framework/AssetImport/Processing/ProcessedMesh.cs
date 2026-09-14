using System.Numerics;
using Phoenix.Framework.Rendering.Geometry.Model;

namespace Phoenix.Framework.AssetImport.Processing;

/// <summary>
/// Intermediate mesh representation containing converted vertices, indices, and part-relative transforms.
/// </summary>
internal sealed class ProcessedMesh
{
    public string Name { get; set; } = "";
    public int MaterialIndex { get; set; }
    public int NormalIndex { get; set; } = -1;
    public Matrix4x4 Transform { get; set; }
    public uint[] Indices { get; set; } = [];
    public ModelVertex[] Vertices { get; set; } = [];
}
