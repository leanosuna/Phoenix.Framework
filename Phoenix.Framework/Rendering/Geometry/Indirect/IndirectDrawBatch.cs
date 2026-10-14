using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Geometry.Indirect;

/// <summary>
/// Encapsulates GPU resources for a multi-draw indirect batch, containing packed vertex/index buffers,
/// indirect command storage buffer, and submesh metadata SSBO descriptor set.
/// </summary>
public sealed class IndirectDrawBatch : IDisposable
{
    private readonly Graphics _graphics;
    private bool _disposed;

    public VulkanBuffer PackedVertexBuffer { get; }
    public VulkanBuffer PackedIndexBuffer { get; }
    public VulkanStorageBuffer IndirectCommandBuffer { get; }
    public VulkanStorageBuffer MetadataBuffer { get; }
    public uint DrawCount => (uint)Commands.Length;
    public DrawIndexedIndirectCommand[] Commands { get; }
    public SubmeshMetadata[] Metadata { get; }
    public DescriptorPool MetadataDescriptorPool { get; }
    public DescriptorSet MetadataDescriptorSet { get; }
    public DescriptorSetLayout MetadataDescriptorSetLayout { get; }

    public IndirectDrawBatch(
        Graphics graphics,
        VulkanBuffer packedVertexBuffer,
        VulkanBuffer packedIndexBuffer,
        VulkanStorageBuffer indirectCommandBuffer,
        VulkanStorageBuffer metadataBuffer,
        DrawIndexedIndirectCommand[] commands,
        SubmeshMetadata[] metadata,
        DescriptorPool metadataDescriptorPool,
        DescriptorSet metadataDescriptorSet,
        DescriptorSetLayout metadataDescriptorSetLayout)
    {
        _graphics = graphics;
        PackedVertexBuffer = packedVertexBuffer;
        PackedIndexBuffer = packedIndexBuffer;
        IndirectCommandBuffer = indirectCommandBuffer;
        MetadataBuffer = metadataBuffer;
        Commands = commands;
        Metadata = metadata;
        MetadataDescriptorPool = metadataDescriptorPool;
        MetadataDescriptorSet = metadataDescriptorSet;
        MetadataDescriptorSetLayout = metadataDescriptorSetLayout;
    }

    /// <summary>
    /// Updates the submesh metadata buffer on the GPU for all or a subset of submeshes.
    /// </summary>
    public void UpdateMetadata(ReadOnlySpan<SubmeshMetadata> metadata, ulong offset = 0)
    {
        MetadataBuffer.SetData(metadata, offset);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        PackedVertexBuffer.Dispose();
        PackedIndexBuffer.Dispose();
        IndirectCommandBuffer.Dispose();
        MetadataBuffer.Dispose();

        if (MetadataDescriptorPool.Handle != 0)
        {
            unsafe
            {
                _graphics.Context.Vk.DestroyDescriptorPool(_graphics.Context.Device, MetadataDescriptorPool, null);
            }
        }

        _disposed = true;
    }
}
