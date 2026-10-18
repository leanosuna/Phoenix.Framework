using Phoenix.AssetTool.Core.Metadata;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Indirect;

/// <summary>
/// Packs multiple submeshes and model hierarchies into contiguous vertex, index, indirect command,
/// and metadata storage buffers for single-dispatch multi-draw indirect rendering.
/// </summary>
public sealed class IndirectMeshBuilder
{
    private readonly List<ModelVertex> _packedVertices = [];
    private readonly List<uint> _packedIndices = [];
    private readonly List<DrawIndexedIndirectCommand> _commands = [];
    private readonly List<SubmeshMetadata> _metadata = [];
    private readonly Dictionary<MeshInfo, (int VertexOffset, uint FirstIndex)> _meshGeometryOffsets = new(ReferenceEqualityComparer.Instance);

    public int SubmeshCount => _commands.Count;
    public int TotalVertexCount => _packedVertices.Count;
    public int TotalIndexCount => _packedIndices.Count;

    /// <summary>
    /// Adds a submesh to the batch with raw geometry and metadata.
    /// </summary>
    public void AddSubmesh(
        ReadOnlySpan<ModelVertex> vertices,
        ReadOnlySpan<uint> indices,
        in Matrix4x4 world,
        uint albedoIndex = 0,
        uint normalIndex = 0,
        uint specularIndex = 0,
        uint materialFlags = 0)
    {
        int vertexOffset = _packedVertices.Count;
        uint firstIndex = (uint)_packedIndices.Count;

        _packedVertices.AddRange(vertices.ToArray());
        _packedIndices.AddRange(indices.ToArray());

        DrawIndexedIndirectCommand cmd = new(
            indexCount: (uint)indices.Length,
            instanceCount: 1,
            firstIndex: firstIndex,
            vertexOffset: vertexOffset,
            firstInstance: 0);
        _commands.Add(cmd);

        SubmeshMetadata meta = new(
            world: world,
            albedoIndex: albedoIndex,
            normalIndex: normalIndex,
            specularIndex: specularIndex,
            materialFlags: materialFlags);
        _metadata.Add(meta);
    }

    /// <summary>
    /// Adds an entire ModelInfo hierarchy into the batch, placing each submesh with its local mesh transform multiplied by world.
    /// Automatically deduplicates shared mesh geometry in packed vertex and index buffers.
    /// </summary>
    public void AddModel(
        ModelInfo modelInfo,
        in Matrix4x4 world,
        uint baseTextureSlot = 0,
        uint normalBaseTextureSlot = 0,
        uint materialFlags = 0)
    {
        foreach (var mesh in modelInfo.Meshes)
        {
            var meshWorld = mesh.Transform * world;

            uint albedo = baseTextureSlot > 0
                ? (mesh.MaterialIndex > 0 ? baseTextureSlot + (uint)mesh.MaterialIndex : baseTextureSlot)
                : (mesh.MaterialIndex >= 0 ? (uint)mesh.MaterialIndex : 0);
            uint normal = normalBaseTextureSlot > 0
                ? (mesh.NormalIndex > 0 ? normalBaseTextureSlot + (uint)mesh.NormalIndex : normalBaseTextureSlot)
                : (mesh.NormalIndex >= 0 ? (uint)mesh.NormalIndex : 0);

            if (!_meshGeometryOffsets.TryGetValue(mesh, out var offsets))
            {
                offsets = (_packedVertices.Count, (uint)_packedIndices.Count);
                _packedVertices.AddRange(mesh.Vertices);
                _packedIndices.AddRange(mesh.Indices);
                _meshGeometryOffsets[mesh] = offsets;
            }

            if (mesh.Submeshes.Count > 0)
            {
                foreach (var sub in mesh.Submeshes)
                {
                    uint subAlbedo = sub.MaterialIndex >= 0
                        ? (baseTextureSlot > 0 ? (sub.MaterialIndex > 0 ? baseTextureSlot + (uint)sub.MaterialIndex : baseTextureSlot) : (uint)sub.MaterialIndex)
                        : albedo;
                    uint subNormal = sub.NormalIndex >= 0
                        ? (normalBaseTextureSlot > 0 ? (sub.NormalIndex > 0 ? normalBaseTextureSlot + (uint)sub.NormalIndex : normalBaseTextureSlot) : (uint)sub.NormalIndex)
                        : normal;

                    DrawIndexedIndirectCommand cmd = new(
                        indexCount: sub.IndexCount,
                        instanceCount: 1,
                        firstIndex: offsets.FirstIndex + sub.FirstIndex,
                        vertexOffset: offsets.VertexOffset + sub.VertexOffset,
                        firstInstance: 0);
                    _commands.Add(cmd);

                    SubmeshMetadata meta = new(
                        world: meshWorld,
                        albedoIndex: subAlbedo,
                        normalIndex: subNormal,
                        specularIndex: 0,
                        materialFlags: materialFlags);
                    _metadata.Add(meta);
                }
            }
            else
            {
                DrawIndexedIndirectCommand cmd = new(
                    indexCount: (uint)mesh.Indices.Length,
                    instanceCount: 1,
                    firstIndex: offsets.FirstIndex,
                    vertexOffset: offsets.VertexOffset,
                    firstInstance: 0);
                _commands.Add(cmd);

                SubmeshMetadata meta = new(
                    world: meshWorld,
                    albedoIndex: albedo,
                    normalIndex: normal,
                    specularIndex: 0,
                    materialFlags: materialFlags);
                _metadata.Add(meta);
            }
        }
    }

    /// <summary>
    /// Builds the GPU buffers, allocates descriptor resources, and returns the constructed IndirectDrawBatch.
    /// </summary>
    public IndirectDrawBatch Build(Graphics graphics, DescriptorSetLayout? metadataLayout = null)
    {
        if (_commands.Count == 0)
            throw new InvalidOperationException("Cannot build an empty indirect draw batch.");

        ulong vertexBufferSize = (ulong)(_packedVertices.Count * Marshal.SizeOf<ModelVertex>());
        var vertexBuffer = graphics.CreateBuffer(
            vertexBufferSize,
            BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.DeviceLocalBit);
        vertexBuffer.SetData<ModelVertex>(CollectionsMarshal.AsSpan(_packedVertices));

        ulong indexBufferSize = (ulong)(_packedIndices.Count * sizeof(uint));
        var indexBuffer = graphics.CreateBuffer(
            indexBufferSize,
            BufferUsageFlags.IndexBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.DeviceLocalBit);
        indexBuffer.SetData<uint>(CollectionsMarshal.AsSpan(_packedIndices));

        ulong cmdBufferSize = (ulong)(_commands.Count * Marshal.SizeOf<DrawIndexedIndirectCommand>());
        var indirectCmdBuffer = new VulkanStorageBuffer(graphics.Context, cmdBufferSize, hostVisible: false);
        indirectCmdBuffer.SetData<DrawIndexedIndirectCommand>(CollectionsMarshal.AsSpan(_commands));

        ulong metaBufferSize = (ulong)(_metadata.Count * Marshal.SizeOf<SubmeshMetadata>());
        var metadataBuffer = new VulkanStorageBuffer(graphics.Context, metaBufferSize, hostVisible: true);
        metadataBuffer.SetData<SubmeshMetadata>(CollectionsMarshal.AsSpan(_metadata));

        var layout = metadataLayout ?? graphics.IndirectMetadataLayout;
        var (pool, set) = graphics.CreateStorageBufferDescriptorSet(layout, metadataBuffer, binding: 0);

        return new IndirectDrawBatch(
            graphics,
            vertexBuffer,
            indexBuffer,
            indirectCmdBuffer,
            metadataBuffer,
            _commands.ToArray(),
            _metadata.ToArray(),
            pool,
            set,
            layout);
    }
}
