using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Coalesces multiple buffer staging copies into a single command buffer submission and fence wait.
/// </summary>
public sealed unsafe class VulkanUploadBatch : IDisposable
{
    private readonly VulkanContext _context;
    private readonly CommandPool _pool;
    private readonly Queue _queue;
    private CommandBuffer _cmd;
    private readonly List<VulkanBuffer> _stagingBuffers = new();
    private bool _submitted;

    public CommandBuffer CommandBuffer => _cmd;

    public VulkanUploadBatch(VulkanContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _pool = context.TransferCommandPool.Handle != 0 ? context.TransferCommandPool : context.CommandPool;
        _queue = (context.TransferQueue.HasValue && context.TransferQueue.Value.Handle != 0)
            ? context.TransferQueue.Value
            : context.GraphicsQueue;

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = _pool,
            CommandBufferCount = 1
        };

        VulkanHelper.Check(_context.Vk.AllocateCommandBuffers(_context.Device, in allocInfo, out _cmd),
            "Failed to allocate command buffer for VulkanUploadBatch.");

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        VulkanHelper.Check(_context.Vk.BeginCommandBuffer(_cmd, in beginInfo),
            "Failed to begin command buffer for VulkanUploadBatch.");
    }

    public void UploadBuffer<T>(VulkanBuffer target, ReadOnlySpan<T> data, ulong dstOffset = 0) where T : unmanaged
    {
        if (_submitted)
            throw new ObjectDisposedException(nameof(VulkanUploadBatch));

        if (data.IsEmpty)
            return;

        ulong size = (ulong)(data.Length * sizeof(T));
        var staging = new VulkanBuffer(
            _context,
            size,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        staging.SetData(data);
        _stagingBuffers.Add(staging);

        BufferCopy region = new()
        {
            SrcOffset = 0,
            DstOffset = dstOffset,
            Size = size
        };

        _context.Vk.CmdCopyBuffer(_cmd, staging.Buffer, target.Buffer, 1, in region);
    }

    public void SubmitAndWait()
    {
        if (_submitted)
            return;

        _submitted = true;

        if (_stagingBuffers.Count == 0)
        {
            _context.Vk.EndCommandBuffer(_cmd);
            _context.Vk.FreeCommandBuffers(_context.Device, _pool, 1, in _cmd);
            return;
        }

        MemoryBarrier2 barrier = new()
        {
            SType = StructureType.MemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.CopyBit,
            SrcAccessMask = AccessFlags2.TransferWriteBit,
            DstStageMask = PipelineStageFlags2.AllCommandsBit,
            DstAccessMask = AccessFlags2.MemoryReadBit | AccessFlags2.MemoryWriteBit
        };

        DependencyInfo dep = new()
        {
            SType = StructureType.DependencyInfo,
            MemoryBarrierCount = 1,
            PMemoryBarriers = &barrier
        };

        _context.Vk.CmdPipelineBarrier2(_cmd, in dep);

        VulkanHelper.Check(_context.Vk.EndCommandBuffer(_cmd),
            "Failed to end command buffer in VulkanUploadBatch.");

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo
        };

        VulkanHelper.Check(_context.Vk.CreateFence(_context.Device, in fenceInfo, null, out var fence),
            "Failed to create fence for VulkanUploadBatch.");

        try
        {
            var cmd = _cmd;
            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &cmd
            };

            var queueLock = (_queue.Handle == _context.TransferQueue?.Handle)
                ? _context.TransferQueueLock
                : _context.GraphicsQueueLock;

            lock (queueLock)
            {
                VulkanHelper.Check(_context.Vk.QueueSubmit(_queue, 1, in submitInfo, fence),
                    "Failed to submit command buffer in VulkanUploadBatch.");
            }

            VulkanHelper.Check(_context.Vk.WaitForFences(_context.Device, 1, in fence, true, ulong.MaxValue),
                "Failed waiting for VulkanUploadBatch fence.");
        }
        finally
        {
            _context.Vk.DestroyFence(_context.Device, fence, null);
            _context.Vk.FreeCommandBuffers(_context.Device, _pool, 1, in _cmd);

            for (int i = 0; i < _stagingBuffers.Count; i++)
            {
                _stagingBuffers[i].Dispose();
            }
            _stagingBuffers.Clear();
        }
    }

    public void Dispose() => SubmitAndWait();
}
