using Silk.NET.Vulkan;
using Buffer = System.Buffer;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using piko.VulkanMemoryAllocator;

namespace Phoenix.Framework.Rendering.Vulkan;

public sealed unsafe class VulkanBuffer : IDisposable
{
    private readonly VulkanContext _context;
    private VkBuffer _buffer;
    private DeviceMemory _memory;
    private Allocation _allocation;
    private readonly ulong _size;
    private readonly BufferUsageFlags _usage;
    private readonly MemoryPropertyFlags _memoryProperties;
    private void* _mappedData;
    private bool _disposed;

    public VkBuffer Buffer => _buffer;
    public DeviceMemory Memory => _memory;
    public Allocation Allocation => _allocation;
    public ulong Size => _size;
    public BufferUsageFlags Usage => _usage;
    public MemoryPropertyFlags MemoryProperties => _memoryProperties;
    internal void* MappedData => _mappedData;

    /// <summary>
    /// Creates and allocates a GPU buffer via VMA with the specified size, usage flags, and memory properties.
    /// </summary>
    public VulkanBuffer(VulkanContext context, ulong size, BufferUsageFlags usage, MemoryPropertyFlags memoryProperties)
    {
        _context = context;
        _size = size;
        _usage = usage;
        _memoryProperties = memoryProperties;

        if ((memoryProperties & MemoryPropertyFlags.HostVisibleBit) == 0)
        {
            usage |= BufferUsageFlags.TransferDstBit | BufferUsageFlags.TransferSrcBit;
        }

        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        AllocationCreateInfo allocCreateInfo = new();
        if ((memoryProperties & MemoryPropertyFlags.HostVisibleBit) != 0)
        {
            allocCreateInfo.Usage = MemoryUsage.Auto;
            allocCreateInfo.Flags = (uint)(AllocationCreateFlagBits.HostAccessSequentialWriteBit | AllocationCreateFlagBits.MappedBit);
        }
        else
        {
            allocCreateInfo.Usage = MemoryUsage.AutoPreferDevice;
        }

        Silk.NET.Vulkan.Buffer buf;
        Allocation alloc;
        AllocationInfo allocDetails;
        VulkanHelper.Check(
            VMA.CreateBuffer(_context.Allocator, &bufferInfo, &allocCreateInfo, &buf, &alloc, &allocDetails),
            "Failed to create Vulkan buffer via VMA.");

        _buffer = buf;
        _allocation = alloc;
        _memory = allocDetails.DeviceMemory;
        if ((memoryProperties & MemoryPropertyFlags.HostVisibleBit) != 0)
        {
            _mappedData = (void*)allocDetails.PMappedData;
        }
    }

    /// <summary>
    /// Copies elements from a read-only span into the buffer memory at the specified byte offset.
    /// When the buffer is host-visible, writes directly to mapped memory.
    /// When the buffer is device-local, executes a staging upload command buffer.
    /// </summary>
    public void SetData<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged
    {
        ulong byteCount = (ulong)(data.Length * sizeof(T));
        if (offset + byteCount > _size)
            throw new ArgumentOutOfRangeException(nameof(data), "Data size exceeds buffer capacity.");

        if (_mappedData != null)
        {
            fixed (T* pData = data)
            {
                System.Buffer.MemoryCopy(pData, (byte*)_mappedData + offset, _size - offset, byteCount);
            }

            if ((_memoryProperties & MemoryPropertyFlags.HostCoherentBit) == 0)
            {
                VMA.FlushAllocation(_context.Allocator, _allocation, offset, byteCount);
            }
        }
        else
        {
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
            _context.Vk.CmdCopyBuffer(cmd, staging.Buffer, _buffer, 1, in copyRegion);
            _context.EndSingleTimeCommands(cmd);
        }
    }

    /// <summary>
    /// Copies elements from the buffer into the destination span.
    /// When the buffer is host-visible, reads directly from mapped memory.
    /// When the buffer is device-local, executes a staging download command buffer.
    /// </summary>
    public void GetData<T>(Span<T> destination, ulong offset = 0) where T : unmanaged
    {
        ulong byteCount = (ulong)(destination.Length * sizeof(T));
        if (offset + byteCount > _size)
            throw new ArgumentOutOfRangeException(nameof(destination), "Data size exceeds buffer capacity.");

        if (_mappedData != null)
        {
            if ((_memoryProperties & MemoryPropertyFlags.HostCoherentBit) == 0)
            {
                VMA.InvalidateAllocation(_context.Allocator, _allocation, offset, byteCount);
            }

            fixed (T* pDest = destination)
            {
                System.Buffer.MemoryCopy((byte*)_mappedData + offset, pDest, byteCount, byteCount);
            }
        }
        else
        {
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
            _context.Vk.CmdCopyBuffer(cmd, _buffer, staging.Buffer, 1, in copyRegion);
            _context.EndSingleTimeCommands(cmd);

            fixed (T* pDest = destination)
            {
                System.Buffer.MemoryCopy(staging.MappedData, pDest, byteCount, byteCount);
            }
        }
    }

    /// <summary>
    /// Destroys the buffer and frees the VMA allocation.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_buffer.Handle != 0)
        {
            VMA.DestroyBuffer(_context.Allocator, _buffer, _allocation);
            _buffer = default;
            _allocation = default;
            _memory = default;
            _mappedData = null;
        }

        _disposed = true;
    }
}
