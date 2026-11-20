using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Manages Set 0 descriptor layout, pools, and per-frame-in-flight uniform buffers for global camera and timing frame data.
/// </summary>
internal sealed unsafe class FrameUniformsManager : IDisposable
{
    public const int MaxViewsPerFrame = 32;
    private readonly VulkanContext _context;
    private DescriptorSetLayout _descriptorSetLayout;
    private DescriptorPool _descriptorPool;
    private readonly DescriptorSet[] _descriptorSets = new DescriptorSet[VulkanSwapchain.MaxFramesInFlight];
    private readonly VulkanBuffer[] _uboBuffers = new VulkanBuffer[VulkanSwapchain.MaxFramesInFlight];
    private readonly int[] _frameSlotIndex = new int[VulkanSwapchain.MaxFramesInFlight];
    private bool _disposed;

    public DescriptorSetLayout DescriptorSetLayout => _descriptorSetLayout;
    public FrameUniforms CurrentUniforms { get; private set; }

    /// <summary>
    /// Returns the descriptor set for the specified frame in flight.
    /// </summary>
    public DescriptorSet GetDescriptorSet(int frameIndex) => _descriptorSets[frameIndex];

    /// <summary>
    /// Initializes the Set 0 descriptor set layout, uniform buffers, descriptor pool, and writes descriptor bindings.
    /// </summary>
    public FrameUniformsManager(VulkanContext context)
    {
        _context = context;

        CreateDescriptorSetLayout();
        CreateDescriptorPool();
        CreateBuffersAndSets();
    }

    /// <summary>
    /// Creates the descriptor set layout with a single dynamic uniform buffer binding at binding 0.
    /// </summary>
    private void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBinding binding = new()
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBufferDynamic,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit
        };

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding
        };

        VulkanHelper.Check(_context.Vk.CreateDescriptorSetLayout(_context.Device, in layoutInfo, null, out _descriptorSetLayout),
            "Failed to create FrameUniforms descriptor set layout.");
    }

    /// <summary>
    /// Creates the descriptor pool capable of allocating dynamic uniform buffer descriptor sets for all frames in flight.
    /// </summary>
    private void CreateDescriptorPool()
    {
        DescriptorPoolSize poolSize = new()
        {
            Type = DescriptorType.UniformBufferDynamic,
            DescriptorCount = VulkanSwapchain.MaxFramesInFlight
        };

        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
            MaxSets = VulkanSwapchain.MaxFramesInFlight
        };

        VulkanHelper.Check(_context.Vk.CreateDescriptorPool(_context.Device, in poolInfo, null, out _descriptorPool),
            "Failed to create FrameUniforms descriptor pool.");
    }

    /// <summary>
    /// Allocates host-visible uniform buffers and descriptor sets, and binds the buffers to descriptor set slots.
    /// </summary>
    private void CreateBuffersAndSets()
    {
        ulong slotSize = (ulong)sizeof(FrameUniforms);
        ulong totalBufferSize = slotSize * MaxViewsPerFrame;
        DescriptorSetLayout* layouts = stackalloc DescriptorSetLayout[VulkanSwapchain.MaxFramesInFlight];

        for (int i = 0; i < VulkanSwapchain.MaxFramesInFlight; i++)
        {
            layouts[i] = _descriptorSetLayout;
            _uboBuffers[i] = new VulkanBuffer(_context, totalBufferSize, BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        }

        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = VulkanSwapchain.MaxFramesInFlight,
            PSetLayouts = layouts
        };

        fixed (DescriptorSet* pSets = _descriptorSets)
        {
            VulkanHelper.Check(_context.Vk.AllocateDescriptorSets(_context.Device, in allocInfo, pSets),
                "Failed to allocate FrameUniforms descriptor sets.");
        }

        for (int i = 0; i < VulkanSwapchain.MaxFramesInFlight; i++)
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = _uboBuffers[i].Buffer,
                Offset = 0,
                Range = slotSize
            };

            WriteDescriptorSet write = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _descriptorSets[i],
                DstBinding = 0,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.UniformBufferDynamic,
                DescriptorCount = 1,
                PBufferInfo = &bufferInfo
            };

            _context.Vk.UpdateDescriptorSets(_context.Device, 1, in write, 0, null);
        }
    }

    /// <summary>
    /// Uploads frame uniform values into slot 0 of the uniform buffer assigned to the current frame in flight.
    /// </summary>
    public void Update(int frameIndex, in FrameUniforms uniforms)
    {
        _frameSlotIndex[frameIndex] = 0;
        CurrentUniforms = uniforms;
        ReadOnlySpan<FrameUniforms> span = new(in uniforms);
        _uboBuffers[frameIndex].SetData(span, 0);
    }

    /// <summary>
    /// Allocates a new view slot for the current frame, writes uniform data, and returns the byte offset for dynamic binding.
    /// </summary>
    public uint AllocateAndWrite(int frameIndex, in FrameUniforms uniforms)
    {
        int slot = _frameSlotIndex[frameIndex] + 1;
        if (slot >= MaxViewsPerFrame)
            throw new InvalidOperationException($"Exceeded maximum views per frame ({MaxViewsPerFrame}).");

        _frameSlotIndex[frameIndex] = slot;
        CurrentUniforms = uniforms;
        uint byteOffset = (uint)(slot * sizeof(FrameUniforms));
        ReadOnlySpan<FrameUniforms> span = new(in uniforms);
        _uboBuffers[frameIndex].SetData(span, byteOffset);
        return byteOffset;
    }

    /// <summary>
    /// Disposes uniform buffers, descriptor pool, and descriptor set layout.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        for (int i = 0; i < VulkanSwapchain.MaxFramesInFlight; i++)
        {
            _uboBuffers[i]?.Dispose();
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
