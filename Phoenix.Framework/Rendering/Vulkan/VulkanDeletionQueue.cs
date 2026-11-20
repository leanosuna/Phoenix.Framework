namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Manages deferred disposal of Vulkan resources indexed by frame-in-flight to avoid GPU-CPU race conditions.
/// </summary>
public sealed class VulkanDeletionQueue : IDisposable
{
    private readonly Queue<Action>[] _queues;

    public VulkanDeletionQueue(int framesInFlight = VulkanSwapchain.MaxFramesInFlight)
    {
        if (framesInFlight <= 0)
            throw new ArgumentOutOfRangeException(nameof(framesInFlight), "Frames in flight must be at least 1.");

        _queues = new Queue<Action>[framesInFlight];
        for (int i = 0; i < framesInFlight; i++)
        {
            _queues[i] = new Queue<Action>();
        }
    }

    public void Enqueue(int frameIndex, Action deleteAction)
    {
        ArgumentNullException.ThrowIfNull(deleteAction);
        int slot = Math.Clamp(frameIndex, 0, _queues.Length - 1);
        lock (_queues[slot])
        {
            _queues[slot].Enqueue(deleteAction);
        }
    }

    public void FlushFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= _queues.Length)
            return;

        var queue = _queues[frameIndex];
        lock (queue)
        {
            while (queue.Count > 0)
            {
                var action = queue.Dequeue();
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Log.Error($"[VulkanDeletionQueue] Error executing deletion action: {ex.Message}");
                }
            }
        }
    }

    public void FlushAll()
    {
        for (int i = 0; i < _queues.Length; i++)
        {
            FlushFrame(i);
        }
    }

    public void Dispose() => FlushAll();
}
