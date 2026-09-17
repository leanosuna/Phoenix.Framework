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
    /// Allocates a device-local storage buffer capable of compute shader access and transfer operations.
    /// </summary>
    public VulkanStorageBuffer(VulkanContext context, ulong size)
    {
        _context = context;
        _size = size;

        _deviceBuffer = new VulkanBuffer(
            context,
            size,
            BufferUsageFlags.StorageBufferBit |
            BufferUsageFlags.TransferDstBit |
            BufferUsageFlags.TransferSrcBit |
            BufferUsageFlags.IndirectBufferBit,
            MemoryPropertyFlags.DeviceLocalBit);
    }

    /// <summary>
    /// Uploads CPU data to the GPU storage buffer using a staging buffer.
    /// </summary>
    public void SetData<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged
    {
        ulong byteCount = (ulong)(data.Length * sizeof(T));
        if (offset + byteCount > _size)
            throw new ArgumentOutOfRangeException(nameof(data), "Data size exceeds storage buffer capacity.");

        using var staging = new VulkanBuffer(
            _context,
            byteCount,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        staging.SetData(data);

        var cmd = _context.BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            SrcOffset = 0,
            DstOffset = offset,
            Size = byteCount
        };

        _context.Vk.CmdCopyBuffer(cmd, staging.Buffer, _deviceBuffer.Buffer, 1, in copyRegion);

        _context.EndSingleTimeCommands(cmd);
    }

    /// <summary>
    /// Downloads GPU data from the storage buffer into CPU memory using a staging buffer.
    /// </summary>
    public void GetData<T>(Span<T> destination, ulong offset = 0) where T : unmanaged
    {
        ulong byteCount = (ulong)(destination.Length * sizeof(T));
        if (offset + byteCount > _size)
            throw new ArgumentOutOfRangeException(nameof(destination), "Destination size exceeds storage buffer capacity.");

        using var staging = new VulkanBuffer(
            _context,
            byteCount,
            BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var cmd = _context.BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            SrcOffset = offset,
            DstOffset = 0,
            Size = byteCount
        };

        _context.Vk.CmdCopyBuffer(cmd, _deviceBuffer.Buffer, staging.Buffer, 1, in copyRegion);

        _context.EndSingleTimeCommands(cmd);

        fixed (T* pDest = destination)
        {
            System.Buffer.MemoryCopy(staging.MappedData, pDest, byteCount, byteCount);
        }
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
