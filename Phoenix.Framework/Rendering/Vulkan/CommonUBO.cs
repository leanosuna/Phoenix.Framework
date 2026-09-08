using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Vulkan;

[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct CommonUBOData
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Vector3 CameraPosition;
    public float Time;
    public float DeltaTime;
    private Vector3 _padding;

    /// <summary>
    /// Initializes common uniform buffer data with camera transformations, world position, and elapsed timing values.
    /// </summary>
    public CommonUBOData(Matrix4x4 view, Matrix4x4 projection, Vector3 cameraPosition, float time, float deltaTime)
    {
        View = view;
        Projection = projection;
        CameraPosition = cameraPosition;
        Time = time;
        DeltaTime = deltaTime;
        _padding = Vector3.Zero;
    }
}

internal sealed unsafe class CommonUBOManager : IDisposable
{
    private readonly VulkanContext _context;
    private DescriptorSetLayout _descriptorSetLayout;
    private DescriptorPool _descriptorPool;
    private readonly DescriptorSet[] _descriptorSets = new DescriptorSet[VulkanSwapchain.MaxFramesInFlight];
    private readonly VulkanBuffer[] _uboBuffers = new VulkanBuffer[VulkanSwapchain.MaxFramesInFlight];
    private bool _disposed;

    public DescriptorSetLayout DescriptorSetLayout => _descriptorSetLayout;

    /// <summary>
    /// Returns the descriptor set for the specified frame in flight.
    /// </summary>
    public DescriptorSet GetDescriptorSet(int frameIndex) => _descriptorSets[frameIndex];

    /// <summary>
    /// Initializes the Set 0 descriptor set layout, uniform buffers, descriptor pool, and writes descriptor bindings.
    /// </summary>
    public CommonUBOManager(VulkanContext context)
    {
        _context = context;

        CreateDescriptorSetLayout();
        CreateDescriptorPool();
        CreateBuffersAndSets();
    }

    /// <summary>
    /// Creates the descriptor set layout with a single uniform buffer binding at binding 0.
    /// </summary>
    private void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBinding binding = new()
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit
        };

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding
        };

        VulkanHelper.Check(_context.Vk.CreateDescriptorSetLayout(_context.Device, in layoutInfo, null, out _descriptorSetLayout),
            "Failed to create CommonUBO descriptor set layout.");
    }

    /// <summary>
    /// Creates the descriptor pool capable of allocating uniform buffer descriptor sets for all frames in flight.
    /// </summary>
    private void CreateDescriptorPool()
    {
        DescriptorPoolSize poolSize = new()
        {
            Type = DescriptorType.UniformBuffer,
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
            "Failed to create CommonUBO descriptor pool.");
    }

    /// <summary>
    /// Allocates host-visible uniform buffers and descriptor sets, and binds the buffers to descriptor set slots.
    /// </summary>
    private void CreateBuffersAndSets()
    {
        ulong uboSize = (ulong)sizeof(CommonUBOData);
        DescriptorSetLayout* layouts = stackalloc DescriptorSetLayout[VulkanSwapchain.MaxFramesInFlight];

        for (int i = 0; i < VulkanSwapchain.MaxFramesInFlight; i++)
        {
            layouts[i] = _descriptorSetLayout;
            _uboBuffers[i] = new VulkanBuffer(_context, uboSize, BufferUsageFlags.UniformBufferBit,
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
                "Failed to allocate CommonUBO descriptor sets.");
        }

        for (int i = 0; i < VulkanSwapchain.MaxFramesInFlight; i++)
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = _uboBuffers[i].Buffer,
                Offset = 0,
                Range = uboSize
            };

            WriteDescriptorSet write = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _descriptorSets[i],
                DstBinding = 0,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                PBufferInfo = &bufferInfo
            };

            _context.Vk.UpdateDescriptorSets(_context.Device, 1, in write, 0, null);
        }
    }

    /// <summary>
    /// Uploads camera and timing values into the uniform buffer assigned to the current frame in flight.
    /// </summary>
    public void Update(int frameIndex, in CommonUBOData data)
    {
        ReadOnlySpan<CommonUBOData> span = new(in data);
        _uboBuffers[frameIndex].SetData(span);
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
