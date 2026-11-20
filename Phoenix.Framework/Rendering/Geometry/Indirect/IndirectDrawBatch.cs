using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Geometry.Indirect;

/// <summary>
/// Encapsulates GPU resources for a multi-draw indirect batch, containing packed vertex/index buffers,
/// indirect command storage buffer, and frame-synchronized submesh metadata SSBO descriptor sets.
/// </summary>
public sealed class IndirectDrawBatch : IDisposable
{
    private readonly Graphics _graphics;
    private readonly VulkanBuffer[] _metadataBuffers;
    private readonly DescriptorSet[] _metadataDescriptorSets;
    private bool _disposed;

    public VulkanBuffer PackedVertexBuffer { get; }
    public VulkanBuffer PackedIndexBuffer { get; }
    public VulkanStorageBuffer IndirectCommandBuffer { get; }

    public VulkanBuffer MetadataBuffer => _metadataBuffers[_graphics.Context.CurrentFrame];
    public DescriptorSet MetadataDescriptorSet => _metadataDescriptorSets[_graphics.Context.CurrentFrame];

    public uint DrawCount => (uint)Commands.Length;
    public DrawIndexedIndirectCommand[] Commands { get; }
    public SubmeshMetadata[] Metadata { get; }
    public DescriptorPool MetadataDescriptorPool { get; }
    public DescriptorSetLayout MetadataDescriptorSetLayout { get; }

    public IndirectDrawBatch(
        Graphics graphics,
        VulkanBuffer packedVertexBuffer,
        VulkanBuffer packedIndexBuffer,
        VulkanStorageBuffer indirectCommandBuffer,
        VulkanBuffer[] metadataBuffers,
        DescriptorSet[] metadataDescriptorSets,
        DrawIndexedIndirectCommand[] commands,
        SubmeshMetadata[] metadata,
        DescriptorSetLayout metadataDescriptorSetLayout)
    {
        _graphics = graphics;
        PackedVertexBuffer = packedVertexBuffer;
        PackedIndexBuffer = packedIndexBuffer;
        IndirectCommandBuffer = indirectCommandBuffer;
        _metadataBuffers = metadataBuffers;
        _metadataDescriptorSets = metadataDescriptorSets;
        Commands = commands;
        Metadata = metadata;
        MetadataDescriptorSetLayout = metadataDescriptorSetLayout;
    }

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
        MetadataDescriptorPool = metadataDescriptorPool;
        MetadataDescriptorSetLayout = metadataDescriptorSetLayout;
        Commands = commands;
        Metadata = metadata;

        _metadataBuffers = new VulkanBuffer[VulkanSwapchain.MaxFramesInFlight];
        _metadataDescriptorSets = new DescriptorSet[VulkanSwapchain.MaxFramesInFlight];
        for (int i = 0; i < VulkanSwapchain.MaxFramesInFlight; i++)
        {
            _metadataBuffers[i] = metadataBuffer.DeviceBuffer;
            _metadataDescriptorSets[i] = metadataDescriptorSet;
        }
    }

    public VulkanBuffer GetMetadataBuffer(int frameIndex) => _metadataBuffers[frameIndex];

    public DescriptorSet GetMetadataDescriptorSet(int frameIndex) => _metadataDescriptorSets[frameIndex];

    /// <summary>
    /// Updates the submesh metadata buffer for the active frame on the GPU for all or a subset of submeshes.
    /// </summary>
    public void UpdateMetadata(ReadOnlySpan<SubmeshMetadata> metadata, ulong offset = 0)
    {
        UpdateMetadata(_graphics.Context.CurrentFrame, metadata, offset);
    }

    /// <summary>
    /// Updates the submesh metadata buffer for a specific frame index on the GPU.
    /// </summary>
    public void UpdateMetadata(int frameIndex, ReadOnlySpan<SubmeshMetadata> metadata, ulong offset = 0)
    {
        if (frameIndex >= 0 && frameIndex < _metadataBuffers.Length)
        {
            _metadataBuffers[frameIndex].SetData(metadata, offset);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        PackedVertexBuffer.Dispose();
        PackedIndexBuffer.Dispose();
        IndirectCommandBuffer.Dispose();

        for (int i = 0; i < _metadataBuffers.Length; i++)
        {
            _metadataBuffers[i]?.Dispose();
        }

        for (int i = 0; i < _metadataDescriptorSets.Length; i++)
        {
            if (_metadataDescriptorSets[i].Handle != 0)
            {
                _graphics.FreeStorageBufferDescriptorSet(_metadataDescriptorSets[i]);
            }
        }

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
