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
    private readonly Stack<uint> _freeSlots = new();
    private uint _nextSlot = 1; // Slot 0 is reserved for default 1x1 white texture
    private readonly object _lock = new();
    private VulkanTexture? _defaultWhiteTexture;
    private bool _disposed;

    public DescriptorSetLayout DescriptorSetLayout => _descriptorSetLayout;
    public DescriptorSet DescriptorSet => _descriptorSet;
    public Sampler DefaultSampler => _defaultSampler;
    public VulkanTexture? DefaultWhiteTexture => _defaultWhiteTexture;

    /// <summary>
    /// Initializes the bindless descriptor set layout, pool, set, default sampler, and slot 0 default texture.
    /// </summary>
    public BindlessManager(VulkanContext context)
    {
        _context = context;

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
    /// Allocates and writes a 1x1 pure white texture to reserved slot 0 for untextured geometry fallback.
    /// </summary>
    private void InitializeDefaultTexture()
    {
        ReadOnlySpan<byte> whitePixel = [255, 255, 255, 255];
        _defaultWhiteTexture = new VulkanTexture(_context, this, 1, 1, whitePixel, Format.R8G8B8A8Unorm, isDefaultSlot0: true);
    }

    /// <summary>
    /// Registers an image view into the bindless descriptor set and returns its assigned integer slot ID.
    /// </summary>
    public uint RegisterTexture(ImageView imageView, Sampler? sampler = null)
    {
        lock (_lock)
        {
            uint slot;
            if (_freeSlots.Count > 0)
            {
                slot = _freeSlots.Pop();
            }
            else
            {
                if (_nextSlot >= MaxBindlessTextures)
                    throw new InvalidOperationException($"Exceeded maximum bindless textures capacity ({MaxBindlessTextures}).");
                slot = _nextSlot++;
            }

            UpdateDescriptor(slot, imageView, sampler ?? _defaultSampler);
            return slot;
        }
    }

    /// <summary>
    /// Updates the descriptor set entry at the specified slot index.
    /// </summary>
    internal void UpdateDescriptor(uint slot, ImageView imageView, Sampler sampler)
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

    /// <summary>
    /// Returns a texture slot to the free pool for future allocations.
    /// </summary>
    public void UnregisterTexture(uint slot)
    {
        if (slot == 0)
            return;

        lock (_lock)
        {
            _freeSlots.Push(slot);
        }
    }

    /// <summary>
    /// Releases the default texture, default sampler, descriptor pool, and descriptor set layout.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _defaultWhiteTexture?.Dispose();

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
