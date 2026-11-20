using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Manages double-buffered host-visible storage buffers (SSBO) and descriptor sets synchronized across frames in flight.
/// Defaults to Set 4 for graphics pipelines following the engine's Set 0-3 reserved binding contract.
/// </summary>
public sealed class FrameStorageBuffer<T> : IDisposable where T : unmanaged
{
    private readonly Graphics _graphics;
    private readonly VulkanBuffer[] _buffers;
    private readonly DescriptorSet[] _descriptorSets;
    private readonly int _capacity;
    private readonly ulong _bufferSize;
    private DescriptorSetLayout _layout;
    private bool _disposed;

    /// <summary>
    /// Gets maximum number of elements this storage buffer can hold per frame.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Alias for Capacity.
    /// </summary>
    public int ElementCount => _capacity;

    /// <summary>
    /// Gets the allocated descriptor set layout for this storage buffer.
    /// </summary>
    public DescriptorSetLayout Layout => _layout;

    public FrameStorageBuffer(Graphics graphics, int elementCapacity, ShaderStageFlags stages = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit)
    {
        if (elementCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(elementCapacity), "Element capacity must be positive.");

        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _capacity = elementCapacity;
        _bufferSize = (ulong)(elementCapacity * Marshal.SizeOf<T>());

        _layout = graphics.CreateStorageBufferLayout(stages, binding: 0);

        _buffers = new VulkanBuffer[VulkanSwapchain.MaxFramesInFlight];
        _descriptorSets = new DescriptorSet[VulkanSwapchain.MaxFramesInFlight];

        for (int i = 0; i < VulkanSwapchain.MaxFramesInFlight; i++)
        {
            var buffer = graphics.CreateBuffer(
                _bufferSize,
                BufferUsageFlags.StorageBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            _buffers[i] = buffer;
            var (_, set) = graphics.CreateStorageBufferDescriptorSet(_layout, buffer, _bufferSize, binding: 0);
            _descriptorSets[i] = set;
        }
    }

    /// <summary>
    /// Gets the GPU buffer allocated for the specified frame in flight.
    /// </summary>
    public VulkanBuffer GetBuffer(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= _buffers.Length)
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        return _buffers[frameIndex];
    }

    /// <summary>
    /// Gets the descriptor set bound to the storage buffer for the specified frame in flight.
    /// </summary>
    public DescriptorSet GetDescriptorSet(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= _descriptorSets.Length)
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        return _descriptorSets[frameIndex];
    }

    /// <summary>
    /// Returns a direct memory span to the host-mapped memory for the specified frame index.
    /// </summary>
    public unsafe Span<T> MapFrame(int frameIndex)
    {
        var buffer = GetBuffer(frameIndex);
        if (buffer.MappedData == null)
            throw new InvalidOperationException("Storage buffer is not mapped.");
        return new Span<T>(buffer.MappedData, _capacity);
    }

    /// <summary>
    /// Writes element data into the specified frame's storage buffer without staging overhead.
    /// </summary>
    public void WriteFrame(int frameIndex, ReadOnlySpan<T> data)
    {
        if (data.Length > _capacity)
            throw new ArgumentOutOfRangeException(nameof(data), $"Data length ({data.Length}) exceeds buffer capacity ({_capacity}).");

        var buffer = GetBuffer(frameIndex);
        buffer.SetData(data);
    }

    /// <summary>
    /// Updates data for the active frame in flight on the render context.
    /// </summary>
    public void Update(RenderContext rc, ReadOnlySpan<T> data)
    {
        WriteFrame(rc.CurrentFrame, data);
    }

    /// <summary>
    /// Updates data for a specific frame index.
    /// </summary>
    public void Update(int frameIndex, ReadOnlySpan<T> data)
    {
        WriteFrame(frameIndex, data);
    }

    /// <summary>
    /// Binds the storage buffer descriptor set to a graphics pipeline, defaulting to Set 4.
    /// </summary>
    public void Bind(RenderContext rc, VulkanPipeline pipeline, uint setIndex = 4)
    {
        var set = GetDescriptorSet(rc.CurrentFrame);
        rc.BindDescriptorSet(pipeline, setIndex, set);
    }

    /// <summary>
    /// Binds the storage buffer descriptor set to a compute pipeline, defaulting to Set 0.
    /// </summary>
    public void Bind(RenderContext rc, VulkanComputePipeline pipeline, uint setIndex = 0)
    {
        var set = GetDescriptorSet(rc.CurrentFrame);
        rc.BindComputeDescriptorSet(pipeline, setIndex, set);
    }

    /// <summary>
    /// Alias for Bind with compute pipelines.
    /// </summary>
    public void BindCompute(RenderContext rc, VulkanComputePipeline pipeline, uint setIndex = 0)
    {
        Bind(rc, pipeline, setIndex);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        for (int i = 0; i < _buffers.Length; i++)
        {
            _buffers[i]?.Dispose();
        }

        for (int i = 0; i < _descriptorSets.Length; i++)
        {
            if (_descriptorSets[i].Handle != 0)
            {
                _graphics.FreeStorageBufferDescriptorSet(_descriptorSets[i]);
            }
        }

        if (_layout.Handle != 0)
        {
            unsafe
            {
                _graphics.Context.Vk.DestroyDescriptorSetLayout(_graphics.Context.Device, _layout, null);
            }
            _layout = default;
        }

        _disposed = true;
    }
}
