using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Manages the global bindless texture descriptor set and texture slot allocations for Set 1.
/// </summary>
public sealed unsafe class BindlessManager : IDisposable
{
    public const uint MaxBindlessTextures = 4096;

    private readonly VulkanContext _context;
    private DescriptorSetLayout _descriptorSetLayout;
    private DescriptorPool _descriptorPool;
    private DescriptorSet _descriptorSet;
    private Sampler _defaultSampler;
    private readonly SamplerManager _samplerManager;
    private readonly List<FreeRange> _freeRanges = [];
    private uint _nextSlot = 1; // Slot 0 is reserved for default checkerboard fallback texture
    private readonly object _lock = new();
    private VulkanTexture? _defaultCheckerboardTexture;
    private bool _disposed;

    public DescriptorSetLayout DescriptorSetLayout => _descriptorSetLayout;
    public DescriptorSet DescriptorSet => _descriptorSet;
    public Sampler DefaultSampler => _defaultSampler;
    public SamplerManager SamplerManager => _samplerManager;
    public VulkanTexture? DefaultCheckerboardTexture => _defaultCheckerboardTexture;
    public VulkanTexture? DefaultWhiteTexture => _defaultCheckerboardTexture;

    /// <summary>
    /// Initializes the bindless descriptor set layout, pool, set, default sampler, and slot 0 default texture.
    /// </summary>
    public BindlessManager(VulkanContext context)
    {
        _context = context;
        _samplerManager = new SamplerManager(context);

        CreateDescriptorSetLayout();
        CreateDescriptorPoolAndSet();
        CreateDefaultSampler();
        InitializeDefaultTexture();
    }

    /// <summary>
    /// Creates the descriptor set layout for Set 1 using partially bound and update after bind flags.
    /// </summary>
    private void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBinding binding = new()
        {
            Binding = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = MaxBindlessTextures,
            StageFlags = ShaderStageFlags.FragmentBit,
            PImmutableSamplers = null
        };

        DescriptorBindingFlags bindingFlags = DescriptorBindingFlags.PartiallyBoundBit |
                                              DescriptorBindingFlags.UpdateAfterBindBit;

        DescriptorSetLayoutBindingFlagsCreateInfo bindingFlagsInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
            BindingCount = 1,
            PBindingFlags = &bindingFlags
        };

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            PNext = &bindingFlagsInfo,
            Flags = DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit,
            BindingCount = 1,
            PBindings = &binding
        };

        VulkanHelper.Check(_context.Vk.CreateDescriptorSetLayout(_context.Device, in layoutInfo, null, out _descriptorSetLayout),
            "Failed to create bindless descriptor set layout.");
    }

    /// <summary>
    /// Creates the descriptor pool and allocates the global bindless descriptor set.
    /// </summary>
    private void CreateDescriptorPoolAndSet()
    {
        DescriptorPoolSize poolSize = new()
        {
            Type = DescriptorType.CombinedImageSampler,
            DescriptorCount = MaxBindlessTextures
        };

        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            Flags = DescriptorPoolCreateFlags.UpdateAfterBindBit,
            MaxSets = 1,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize
        };

        VulkanHelper.Check(_context.Vk.CreateDescriptorPool(_context.Device, in poolInfo, null, out _descriptorPool),
            "Failed to create bindless descriptor pool.");

        var layout = _descriptorSetLayout;
        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout
        };

        VulkanHelper.Check(_context.Vk.AllocateDescriptorSets(_context.Device, in allocInfo, out _descriptorSet),
            "Failed to allocate bindless descriptor set.");
    }

    /// <summary>
    /// Creates the global default trilinear repeat texture sampler.
    /// </summary>
    private void CreateDefaultSampler()
    {
        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            MipLodBias = 0.0f,
            AnisotropyEnable = true,
            MaxAnisotropy = 16.0f,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MinLod = 0.0f,
            MaxLod = 1000.0f,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false
        };

        VulkanHelper.Check(_context.Vk.CreateSampler(_context.Device, in samplerInfo, null, out _defaultSampler),
            "Failed to create bindless default sampler.");
    }

    /// <summary>
    /// Allocates and writes a 64x64 magenta/charcoal checkerboard texture to reserved slot 0 for untextured geometry fallback.
    /// </summary>
    private void InitializeDefaultTexture()
    {
        const int width = 64;
        const int height = 64;
        const int tileSize = 8;
        byte[] pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isTileA = ((x / tileSize) + (y / tileSize)) % 2 == 0;
                int idx = (y * width + x) * 4;
                if (isTileA)
                {
                    pixels[idx + 0] = 255; // R
                    pixels[idx + 1] = 0;   // G
                    pixels[idx + 2] = 255; // B
                    pixels[idx + 3] = 255; // A
                }
                else
                {
                    pixels[idx + 0] = 30;  // R
                    pixels[idx + 1] = 30;  // G
                    pixels[idx + 2] = 30;  // B
                    pixels[idx + 3] = 255; // A
                }
            }
        }

        _defaultCheckerboardTexture = new VulkanTexture(_context, this, width, height, pixels, options: null, Format.R8G8B8A8Unorm, isDefaultSlot0: true);
    }

    /// <summary>
    /// Allocates a contiguous block of descriptor slots to avoid fragmentation across models.
    /// Each reserved slot is initially populated with the default checkerboard fallback texture.
    /// </summary>
    public uint AllocateSlots(uint count)
    {
        if (count == 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Must allocate at least 1 slot.");

        lock (_lock)
        {
            uint allocatedStart;
            int foundIdx = -1;

            // Search for a suitable free range using best-fit strategy
            for (int i = 0; i < _freeRanges.Count; i++)
            {
                if (_freeRanges[i].Count >= count)
                {
                    if (foundIdx == -1 || _freeRanges[i].Count < _freeRanges[foundIdx].Count)
                    {
                        foundIdx = i;
                    }
                }
            }

            if (foundIdx != -1)
            {
                var range = _freeRanges[foundIdx];
                allocatedStart = range.Start;
                if (range.Count == count)
                {
                    _freeRanges.RemoveAt(foundIdx);
                }
                else
                {
                    _freeRanges[foundIdx] = new FreeRange(range.Start + count, range.Count - count);
                }
            }
            else
            {
                if (_nextSlot + count > MaxBindlessTextures)
                    throw new InvalidOperationException($"Exceeded maximum bindless textures capacity ({MaxBindlessTextures}).");

                allocatedStart = _nextSlot;
                _nextSlot += count;
            }

            // Immediately populate all reserved slots with the default checkerboard texture
            if (_defaultCheckerboardTexture != null)
            {
                for (uint i = 0; i < count; i++)
                {
                    UpdateDescriptor(allocatedStart + i, _defaultCheckerboardTexture.ImageView, _defaultSampler);
                }
            }

            return allocatedStart;
        }
    }

    /// <summary>
    /// Releases a contiguous block of texture slots back to the free pool and merges adjacent ranges.
    /// </summary>
    public void FreeSlots(uint startSlot, uint count)
    {
        if (startSlot == 0 || count == 0)
            return;

        lock (_lock)
        {
            int insertIdx = 0;
            while (insertIdx < _freeRanges.Count && _freeRanges[insertIdx].Start < startSlot)
                insertIdx++;

            _freeRanges.Insert(insertIdx, new FreeRange(startSlot, count));

            // Coalesce adjacent ranges
            for (int i = _freeRanges.Count - 1; i > 0; i--)
            {
                var prev = _freeRanges[i - 1];
                var curr = _freeRanges[i];
                if (prev.Start + prev.Count == curr.Start)
                {
                    _freeRanges[i - 1] = new FreeRange(prev.Start, prev.Count + curr.Count);
                    _freeRanges.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Registers an image view into the bindless descriptor set using a structural sampler description.
    /// </summary>
    public uint RegisterTexture(ImageView imageView, in SamplerDescription samplerDesc)
    {
        var sampler = _samplerManager.GetOrCreateSampler(samplerDesc);
        return RegisterTexture(imageView, sampler);
    }

    /// <summary>
    /// Registers an image view into the bindless descriptor set and returns its assigned integer slot ID.
    /// </summary>
    public uint RegisterTexture(ImageView imageView, Sampler? sampler = null)
    {
        lock (_lock)
        {
            uint slot = AllocateSlots(1);
            UpdateDescriptor(slot, imageView, sampler ?? _defaultSampler);
            return slot;
        }
    }

    /// <summary>
    /// Updates the descriptor set entry at the specified slot index in real time.
    /// </summary>
    public void UpdateDescriptor(uint slot, ImageView imageView, Sampler sampler)
    {
        lock (_lock)
        {
            DescriptorImageInfo imageInfo = new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = imageView,
                Sampler = sampler
            };

            WriteDescriptorSet write = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _descriptorSet,
                DstBinding = 0,
                DstArrayElement = slot,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                PImageInfo = &imageInfo
            };

            _context.Vk.UpdateDescriptorSets(_context.Device, 1, in write, 0, null);
        }
    }

    /// <summary>
    /// Returns a texture slot to the free pool for future allocations.
    /// </summary>
    public void UnregisterTexture(uint slot)
    {
        FreeSlots(slot, 1);
    }

    /// <summary>
    /// Releases the default texture, default sampler, descriptor pool, descriptor set layout, and cached samplers.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _defaultCheckerboardTexture?.Dispose();

        _samplerManager.Dispose();

        if (_defaultSampler.Handle != 0)
        {
            _context.Vk.DestroySampler(_context.Device, _defaultSampler, null);
            _defaultSampler = default;
        }

        if (_descriptorPool.Handle != 0)
        {
            _context.Vk.DestroyDescriptorPool(_context.Device, _descriptorPool, null);
            _descriptorPool = default;
        }

        if (_descriptorSetLayout.Handle != 0)
        {
            _context.Vk.DestroyDescriptorSetLayout(_context.Device, _descriptorSetLayout, null);
            _descriptorSetLayout = default;
        }

        _disposed = true;
    }
}
