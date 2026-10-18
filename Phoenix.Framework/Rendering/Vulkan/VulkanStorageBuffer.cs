using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Encapsulates a GPU storage buffer (SSBO) for compute shaders, indirect draw commands, and data transfers.
/// </summary>
public sealed unsafe class VulkanStorageBuffer : IDisposable
{
    private readonly VulkanContext _context;
    private readonly VulkanBuffer _deviceBuffer;
    private readonly ulong _size;
    private bool _disposed;

    /// <summary>
    /// Gets the underlying Vulkan buffer.
    /// </summary>
    public VulkanBuffer DeviceBuffer => _deviceBuffer;

    /// <summary>
    /// Gets the raw Vulkan buffer handle.
    /// </summary>
    public VkBuffer Buffer => _deviceBuffer.Buffer;

    /// <summary>
    /// Gets the allocated buffer size in bytes.
    /// </summary>
    public ulong Size => _size;

    /// <summary>
    /// Gets whether the storage buffer is host-visible and persistently mapped.
    /// </summary>
    public bool IsHostVisible => _deviceBuffer.MappedData != null;

    /// <summary>
    /// Allocates a storage buffer capable of compute shader access, indirect drawing, and data transfers.
    /// When hostVisible is true, the buffer is allocated in host-accessible memory for low-latency per-frame CPU updates without staging.
    /// </summary>
    public VulkanStorageBuffer(VulkanContext context, ulong size, bool hostVisible = false)
    {
        _context = context;
        _size = size;

        var memoryFlags = hostVisible
            ? MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
            : MemoryPropertyFlags.DeviceLocalBit;

        _deviceBuffer = new VulkanBuffer(
            context,
            size,
            BufferUsageFlags.StorageBufferBit |
            BufferUsageFlags.TransferDstBit |
            BufferUsageFlags.TransferSrcBit |
            BufferUsageFlags.IndirectBufferBit,
            memoryFlags);
    }

    /// <summary>
    /// Uploads CPU data to the GPU storage buffer.
    /// When host-visible, writes directly to mapped memory without GPU pipeline stalls.
    /// When device-local, executes a staging buffer transfer.
    /// </summary>
    public void SetData<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged
    {
        _deviceBuffer.SetData(data, offset);
    }

    /// <summary>
    /// Downloads GPU data from the storage buffer into CPU memory.
    /// </summary>
    public void GetData<T>(Span<T> destination, ulong offset = 0) where T : unmanaged
    {
        _deviceBuffer.GetData(destination, offset);
    }

    /// <summary>
    /// Produces a descriptor buffer info struct for descriptor set bindings.
    /// </summary>
    public DescriptorBufferInfo GetDescriptorInfo(ulong offset = 0, ulong range = Vk.WholeSize)
    {
        return new DescriptorBufferInfo
        {
            Buffer = _deviceBuffer.Buffer,
            Offset = offset,
            Range = range
        };
    }

    /// <summary>
    /// Frees GPU buffer resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _deviceBuffer.Dispose();
        _disposed = true;
    }
}
