using Silk.NET.Vulkan;
using Buffer = System.Buffer;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace Phoenix.Framework.Rendering.Vulkan;

public sealed unsafe class VulkanBuffer : IDisposable
{
    private readonly VulkanContext _context;
    private VkBuffer _buffer;
    private DeviceMemory _memory;
    private readonly ulong _size;
    private readonly BufferUsageFlags _usage;
    private readonly MemoryPropertyFlags _memoryProperties;
    private void* _mappedData;
    private bool _disposed;

    public VkBuffer Buffer => _buffer;
    public DeviceMemory Memory => _memory;
    public ulong Size => _size;
    public BufferUsageFlags Usage => _usage;
    public MemoryPropertyFlags MemoryProperties => _memoryProperties;
    internal void* MappedData => _mappedData;


    /// <summary>
    /// Creates and allocates a GPU buffer with the specified size, usage flags, and memory properties.
    /// </summary>
    public VulkanBuffer(VulkanContext context, ulong size, BufferUsageFlags usage, MemoryPropertyFlags memoryProperties)
    {
        _context = context;
        _size = size;
        _usage = usage;
        _memoryProperties = memoryProperties;

        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        VulkanHelper.Check(_context.Vk.CreateBuffer(_context.Device, in bufferInfo, null, out _buffer),
            "Failed to create Vulkan buffer.");

        _context.Vk.GetBufferMemoryRequirements(_context.Device, _buffer, out var memReqs);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = _context.FindMemoryType(memReqs.MemoryTypeBits, memoryProperties)
        };

        VulkanHelper.Check(_context.Vk.AllocateMemory(_context.Device, in allocInfo, null, out _memory),
            "Failed to allocate Vulkan buffer memory.");

        VulkanHelper.Check(_context.Vk.BindBufferMemory(_context.Device, _buffer, _memory, 0),
            "Failed to bind Vulkan buffer memory.");

        if ((memoryProperties & MemoryPropertyFlags.HostVisibleBit) != 0)
        {
            void* mapped = null;
            VulkanHelper.Check(_context.Vk.MapMemory(_context.Device, _memory, 0, _size, 0, ref mapped),
                "Failed to map Vulkan buffer memory.");
            _mappedData = mapped;
        }
    }

    /// <summary>
    /// Copies elements from a read-only span into the buffer memory at the specified byte offset.
    /// </summary>
    public void SetData<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged
    {
        ulong byteCount = (ulong)(data.Length * sizeof(T));
        if (offset + byteCount > _size)
            throw new ArgumentOutOfRangeException(nameof(data), "Data size exceeds buffer capacity.");

        if (_mappedData == null)
            throw new InvalidOperationException("Buffer is not host-visible or mapped.");

        fixed (T* pData = data)
        {
            System.Buffer.MemoryCopy(pData, (byte*)_mappedData + offset, _size - offset, byteCount);
        }


        if ((_memoryProperties & MemoryPropertyFlags.HostCoherentBit) == 0)
        {
            MappedMemoryRange range = new()
            {
                SType = StructureType.MappedMemoryRange,
                Memory = _memory,
                Offset = offset,
                Size = byteCount
            };
            _context.Vk.FlushMappedMemoryRanges(_context.Device, 1, in range);
        }
    }

    /// <summary>
    /// Unmaps buffer memory, destroys the buffer handle, and frees allocated device memory.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_mappedData != null)
        {
            _context.Vk.UnmapMemory(_context.Device, _memory);
            _mappedData = null;
        }

        if (_buffer.Handle != 0)
        {
            _context.Vk.DestroyBuffer(_context.Device, _buffer, null);
            _buffer = default;
        }

        if (_memory.Handle != 0)
        {
            _context.Vk.FreeMemory(_context.Device, _memory, null);
            _memory = default;
        }

        _disposed = true;
    }
}
