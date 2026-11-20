using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Manages growable descriptor pools for allocating UniformBuffer and StorageBuffer descriptor sets.
/// </summary>
public sealed unsafe class VulkanDescriptorAllocator : IDisposable
{
    private const uint DefaultSetsPerPool = 256;

    private readonly VulkanContext _context;
    private readonly uint _maxSets;
    private readonly List<DescriptorPool> _pools = new();
    private readonly Dictionary<ulong, DescriptorPool> _setPools = new();
    private readonly object _lock = new();
    private int _currentPoolIndex = -1;
    private bool _disposed;

    public VulkanDescriptorAllocator(VulkanContext context, uint maxSets = DefaultSetsPerPool)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _maxSets = maxSets;
    }

    private DescriptorPool CreatePool()
    {
        DescriptorPoolSize* poolSizes = stackalloc DescriptorPoolSize[2];
        poolSizes[0] = new DescriptorPoolSize
        {
            Type = DescriptorType.UniformBuffer,
            DescriptorCount = _maxSets
        };
        poolSizes[1] = new DescriptorPoolSize
        {
            Type = DescriptorType.StorageBuffer,
            DescriptorCount = _maxSets
        };

        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit,
            MaxSets = _maxSets,
            PoolSizeCount = 2,
            PPoolSizes = poolSizes
        };

        VulkanHelper.Check(_context.Vk.CreateDescriptorPool(_context.Device, in poolInfo, null, out var pool),
            "Failed to create descriptor pool in VulkanDescriptorAllocator.");
        return pool;
    }

    public DescriptorSet Allocate(DescriptorSetLayout layout)
    {
        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VulkanDescriptorAllocator));

            if (_currentPoolIndex < 0 || _currentPoolIndex >= _pools.Count)
            {
                var newPool = CreatePool();
                _pools.Add(newPool);
                _currentPoolIndex = _pools.Count - 1;
            }

            var pool = _pools[_currentPoolIndex];

            DescriptorSetAllocateInfo allocInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = pool,
                DescriptorSetCount = 1,
                PSetLayouts = &layout
            };

            var result = _context.Vk.AllocateDescriptorSets(_context.Device, in allocInfo, out var set);
            if (result is Result.ErrorOutOfPoolMemory or Result.ErrorFragmentedPool)
            {
                pool = CreatePool();
                _pools.Add(pool);
                _currentPoolIndex = _pools.Count - 1;

                allocInfo.DescriptorPool = pool;
                result = _context.Vk.AllocateDescriptorSets(_context.Device, in allocInfo, out set);
            }

            VulkanHelper.Check(result, "Failed to allocate descriptor set in VulkanDescriptorAllocator.");
            _setPools[set.Handle] = pool;
            return set;
        }
    }

    public void Free(DescriptorSet set)
    {
        if (set.Handle == 0)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;

            if (_setPools.Remove(set.Handle, out var pool))
            {
                _context.Vk.FreeDescriptorSets(_context.Device, pool, 1, in set);
            }
        }
    }

    public void ResetAll()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            for (int i = 0; i < _pools.Count; i++)
            {
                _context.Vk.ResetDescriptorPool(_context.Device, _pools[i], 0);
            }
            _setPools.Clear();
            if (_pools.Count > 0)
            {
                _currentPoolIndex = 0;
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            for (int i = 0; i < _pools.Count; i++)
            {
                if (_pools[i].Handle != 0)
                {
                    _context.Vk.DestroyDescriptorPool(_context.Device, _pools[i], null);
                }
            }
            _pools.Clear();
            _setPools.Clear();
            _disposed = true;
        }
    }
}
