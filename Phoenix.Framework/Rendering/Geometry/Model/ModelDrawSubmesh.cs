using Phoenix.AssetTool.Core.Metadata;
using System.Numerics;
using Submesh = Phoenix.Framework.Rendering.Geometry.Model.ModelMesh;
using MaterialData = Phoenix.AssetTool.Core.Metadata.MaterialInfo;

namespace Phoenix.Framework.Rendering.Geometry.Model;

/// <summary>
/// Lightweight readonly submesh drawing context passed to custom push constant selectors.
/// </summary>
public readonly struct ModelDrawSubmesh
{
    public int SubmeshIndex { get; init; }
    public Submesh Submesh { get; init; }
    public ModelMesh Mesh => Submesh;
    public ModelPart? Part { get; init; }
    public MaterialData Material { get; init; }
    public Matrix4x4 WorldMatrix { get; init; }
    public Matrix4x4 NormalMatrix { get; init; }
    public uint AlbedoTextureId { get; init; }
    public uint NormalTextureId { get; init; }
    public uint MetallicRoughnessTextureId { get; init; }
    public uint EmissiveTextureId { get; init; }
    public uint OcclusionTextureId { get; init; }
}
