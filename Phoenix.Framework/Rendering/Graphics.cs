
using Phoenix.Framework.AssetImport;
using Phoenix.Framework.AssetImport.Processing;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Numerics;

namespace Phoenix.Framework.Rendering;

/// <summary>
/// Manages Vulkan rendering state, swapchain presentation, uniform buffers, and graphics resource allocation.
/// </summary>
public sealed class Graphics : IDisposable
{
    private readonly PhoenixGame _game;
    private bool _disposed;

    /// <summary>
    /// Gets the Vulkan instance, physical device, logical device, and queue context.
    /// </summary>
    public VulkanContext Context { get; private set; } = default!;

    /// <summary>
    /// Gets the Vulkan swapchain and presentation resources.
    /// </summary>
    public VulkanSwapchain Swapchain { get; private set; } = default!;

    /// <summary>
    /// Gets the CommonUBO uniform buffer manager.
    /// </summary>
    internal CommonUBOManager CommonUbo { get; private set; } = default!;

    /// <summary>
    /// Gets the global bindless texture manager for Set 1.
    /// </summary>
    public BindlessManager BindlessManager { get; private set; } = default!;

    /// <summary>
    /// Gets the active rendering command recording context.
    /// </summary>
    public RenderContext RenderContext { get; private set; } = default!;

    /// <summary>
    /// Gets the viewport scaling and filtering controller.
    /// </summary>
    public RenderViewport Viewport { get; private set; } = default!;

    /// <summary>
    /// Gets the default CommonUBO descriptor set layout.
    /// </summary>
    public DescriptorSetLayout CommonUBOLayout => CommonUbo.DescriptorSetLayout;

    /// <summary>
    /// Gets the bindless textures descriptor set layout.
    /// </summary>
    public DescriptorSetLayout BindlessLayout => BindlessManager.DescriptorSetLayout;

    /// <summary>
    /// Gets the descriptor set layout for skeletal animation bone uniform buffers (Set 2).
    /// </summary>
    public DescriptorSetLayout BoneLayout { get; private set; }

    /// <summary>
    /// Gets or sets the default background clear color for dynamic render passes.
    /// </summary>
    public Vector4 ClearColor { get; set; } = new(0.1f, 0.12f, 0.16f, 1.0f);

    /// <summary>
    /// Gets or sets the keyboard key that toggles rendering freeze for debugging.
    /// </summary>
    public Key RenderHaltKey { get; set; } = Key.F11;

    /// <summary>
    /// Gets or sets whether scene rendering is currently halted.
    /// </summary>
    public bool RenderHalt { get; set; }

    /// <summary>
    /// Gets or sets whether vertical synchronization is enabled.
    /// Recreates the Vulkan swapchain if the present mode changed.
    /// </summary>
    public bool VSync
    {
        get => _game.Window.VSync;
        set
        {
            _game.Window.VSync = value;

            if (Swapchain != null && Swapchain.IsVSyncEnabled != value)
            {
                Swapchain.Recreate(_game.Window.FramebufferSize);
            }
        }
    }

    /// <summary>
    /// Gets the primary offscreen scene render target.
    /// </summary>
    public VulkanRenderTarget SceneRenderTarget { get; private set; } = default!;

    /// <summary>
    /// Initializes graphics subsystems, Vulkan context, swapchain, uniform buffers, bindless manager, and viewport.
    /// </summary>
    internal Graphics(PhoenixGame game)
    {
        _game = game;
        Context = new VulkanContext(game.Window, game.Window.Title);
        Swapchain = new VulkanSwapchain(Context, game.Window);
        CommonUbo = new CommonUBOManager(Context);
        BindlessManager = new BindlessManager(Context);
        BoneLayout = CreateBoneLayout();
        Viewport = new RenderViewport(game);
        SceneRenderTarget = CreateRenderTarget(Viewport.Width, Viewport.Height, Swapchain.ImageFormat, true, Swapchain.DepthFormat);
        RenderContext = new RenderContext(this);
    }

    /// <summary>
    /// Creates an offscreen render target with optional depth buffer.
    /// </summary>
    public VulkanRenderTarget CreateRenderTarget(uint width, uint height, Format colorFormat = Format.R8G8B8A8Unorm, bool hasDepth = true, Format? depthFormat = null)
    {
        return new VulkanRenderTarget(Context, BindlessManager, width, height, colorFormat, hasDepth, depthFormat);
    }

    /// <summary>
    /// Creates a multi-render-target (MRT) offscreen framebuffer with optional depth buffer.
    /// </summary>
    public VulkanRenderTarget CreateRenderTarget(uint width, uint height, ReadOnlySpan<Format> colorFormats, bool hasDepth = true, Format? depthFormat = null)
    {
        return new VulkanRenderTarget(Context, BindlessManager, width, height, colorFormats, hasDepth, depthFormat);
    }

    internal void HandleViewportResize()
    {
        if (SceneRenderTarget != null && (SceneRenderTarget.Width != Viewport.Width || SceneRenderTarget.Height != Viewport.Height))
        {
            Context.WaitIdle();
            SceneRenderTarget.Resize(Viewport.Width, Viewport.Height);
        }
    }

    /// <summary>
    /// Allocates and binds a Vulkan buffer for geometry, indices, or uniforms.
    /// </summary>
    public VulkanBuffer CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
    {
        return new VulkanBuffer(Context, size, usage, properties);
    }

    /// <summary>
    /// Creates and uploads a 2D texture from raw RGBA pixel data to the GPU and registers it into the bindless descriptor set.
    /// </summary>
    public VulkanTexture CreateTexture(int width, int height, ReadOnlySpan<byte> rgbaPixels, TextureLoadOptions? options = null, Format format = Format.R8G8B8A8Unorm, uint? preallocatedSlot = null)
    {
        return new VulkanTexture(Context, BindlessManager, width, height, rgbaPixels, options, format, isDefaultSlot0: false, preallocatedSlot);
    }

    /// <summary>
    /// Creates and uploads a 2D texture from pre-compressed BCn mipmap data to the GPU.
    /// </summary>
    public VulkanTexture CreateTexture(CompressedTextureData data, TextureLoadOptions? options = null, uint? preallocatedSlot = null)
    {
        return new VulkanTexture(Context, BindlessManager, data, options, preallocatedSlot);
    }

    /// <summary>
    /// Creates a solid color 2D texture of the specified dimensions.
    /// </summary>
    public VulkanTexture CreateTexture2D(int width, int height, Vector4 color, TextureLoadOptions? options = null)
    {
        byte r = (byte)Math.Clamp((int)(color.X * 255.0f), 0, 255);
        byte g = (byte)Math.Clamp((int)(color.Y * 255.0f), 0, 255);
        byte b = (byte)Math.Clamp((int)(color.Z * 255.0f), 0, 255);
        byte a = (byte)Math.Clamp((int)(color.W * 255.0f), 0, 255);

        byte[] pixels = new byte[width * height * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;
        }

        return CreateTexture(width, height, pixels, options);
    }

    /// <summary>
    /// Loads an image file from disk and uploads it as a bindless texture.
    /// </summary>
    public VulkanTexture LoadTexture(string filePath, TextureLoadOptions? options = null, Format format = Format.R8G8B8A8Srgb)
    {
        return VulkanTexture.FromFile(Context, BindlessManager, filePath, options, format);
    }

    /// <summary>
    /// Loads an image from a stream and uploads it as a bindless texture.
    /// </summary>
    public VulkanTexture LoadTexture(Stream stream, TextureLoadOptions? options = null, Format format = Format.R8G8B8A8Srgb)
    {
        return VulkanTexture.FromStream(Context, BindlessManager, stream, options, format);
    }

    /// <summary>
    /// Creates a graphics pipeline configured for dynamic rendering and depth testing.
    /// Automatically injects Set 0 (CommonUBO) and Set 1 (Bindless) descriptor set layouts if not present.
    /// </summary>
    public VulkanPipeline CreatePipeline(VulkanPipelineDescription description)
    {
        var layouts = description.DescriptorSetLayouts;
        if (layouts.Length == 0)
        {
            layouts = [CommonUbo.DescriptorSetLayout, BindlessManager.DescriptorSetLayout];
        }
        else
        {
            List<DescriptorSetLayout> list = [.. layouts];
            if (list.Count < 1 || list[0].Handle != CommonUbo.DescriptorSetLayout.Handle)
                list.Insert(0, CommonUbo.DescriptorSetLayout);
            if (list.Count < 2 || list[1].Handle != BindlessManager.DescriptorSetLayout.Handle)
                list.Insert(1, BindlessManager.DescriptorSetLayout);
            layouts = [.. list];
        }

        var descWithUbo = description with { DescriptorSetLayouts = layouts };
        return new VulkanPipeline(Context, descWithUbo);
    }

    /// <summary>
    /// Creates a compute pipeline layout and VkPipeline handle from compute SPIR-V bytecode.
    /// </summary>
    public VulkanComputePipeline CreateComputePipeline(byte[] computeSpv, DescriptorSetLayout[]? descriptorSetLayouts = null, uint pushConstantsSize = 80)
    {
        return new VulkanComputePipeline(Context, computeSpv, descriptorSetLayouts, pushConstantsSize);
    }

    /// <summary>
    /// Creates a GPU storage buffer (SSBO) for compute shaders or indirect drawing.
    /// </summary>
    public VulkanStorageBuffer CreateStorageBuffer(ulong size)
    {
        return new VulkanStorageBuffer(Context, size);
    }


    /// <summary>
    /// Recreates swapchain resources and updates scene render target dimensions upon window resize.
    /// </summary>
    internal void Resize(Vector2D<int> size)
    {
        Swapchain.Recreate(size);
        HandleViewportResize();
    }

    /// <summary>
    /// Creates the descriptor set layout for skeletal animation bone uniform buffers (Set 2).
    /// </summary>
    private unsafe DescriptorSetLayout CreateBoneLayout()
    {
        DescriptorSetLayoutBinding binding = new()
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit
        };

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding
        };

        VulkanHelper.Check(Context.Vk.CreateDescriptorSetLayout(Context.Device, in layoutInfo, null, out var layout),
            "Failed to create bone descriptor set layout.");
        return layout;
    }

    /// <summary>
    /// Allocates a dedicated descriptor pool and descriptor set for a bone uniform buffer.
    /// </summary>
    public unsafe (DescriptorPool Pool, DescriptorSet Set) CreateBoneDescriptorSet(VulkanBuffer buffer)
    {
        DescriptorPoolSize poolSize = new()
        {
            Type = DescriptorType.UniformBuffer,
            DescriptorCount = 1
        };

        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
            MaxSets = 1
        };

        VulkanHelper.Check(Context.Vk.CreateDescriptorPool(Context.Device, in poolInfo, null, out var pool),
            "Failed to create bone descriptor pool.");

        var layout = BoneLayout;
        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = pool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout
        };

        VulkanHelper.Check(Context.Vk.AllocateDescriptorSets(Context.Device, in allocInfo, out var set),
            "Failed to allocate bone descriptor set.");

        DescriptorBufferInfo bufferInfo = new()
        {
            Buffer = buffer.Buffer,
            Offset = 0,
            Range = buffer.Size
        };

        WriteDescriptorSet write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo
        };

        Context.Vk.UpdateDescriptorSets(Context.Device, 1, in write, 0, null);

        return (pool, set);
    }

    /// <summary>
    /// Creates a descriptor set layout for a single storage buffer at the specified binding.
    /// </summary>
    public unsafe DescriptorSetLayout CreateStorageBufferLayout(ShaderStageFlags stageFlags = ShaderStageFlags.ComputeBit, uint binding = 0)
    {
        DescriptorSetLayoutBinding b = new()
        {
            Binding = binding,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = stageFlags
        };

        DescriptorSetLayoutCreateInfo info = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &b
        };

        VulkanHelper.Check(Context.Vk.CreateDescriptorSetLayout(Context.Device, in info, null, out var layout),
            "Failed to create storage buffer descriptor set layout.");

        return layout;
    }

    /// <summary>
    /// Allocates a descriptor pool and descriptor set bound to a storage buffer.
    /// </summary>
    public unsafe (DescriptorPool Pool, DescriptorSet Set) CreateStorageBufferDescriptorSet(DescriptorSetLayout layout, VulkanStorageBuffer buffer, uint binding = 0)
    {
        DescriptorPoolSize poolSize = new()
        {
            Type = DescriptorType.StorageBuffer,
            DescriptorCount = 1
        };

        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
            MaxSets = 1
        };

        VulkanHelper.Check(Context.Vk.CreateDescriptorPool(Context.Device, in poolInfo, null, out var pool),
            "Failed to create storage buffer descriptor pool.");

        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = pool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout
        };

        VulkanHelper.Check(Context.Vk.AllocateDescriptorSets(Context.Device, in allocInfo, out var set),
            "Failed to allocate storage buffer descriptor set.");

        var bufferInfo = buffer.GetDescriptorInfo();

        WriteDescriptorSet write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo
        };

        Context.Vk.UpdateDescriptorSets(Context.Device, 1, in write, 0, null);

        return (pool, set);
    }

    /// <summary>
    /// Waits for all submitted GPU commands across all queues to finish execution.
    /// </summary>
    public void WaitIdle()
    {
        lock (Context.GraphicsQueueLock)
        {
            Context.Vk.DeviceWaitIdle(Context.Device);
        }
    }

    /// <summary>
    /// Disposes bindless manager, bone layout, uniform buffers, swapchain, and Vulkan device context.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (BoneLayout.Handle != 0)
        {
            unsafe
            {
                Context.Vk.DestroyDescriptorSetLayout(Context.Device, BoneLayout, null);
            }
            BoneLayout = default;
        }

        SceneRenderTarget?.Dispose();
        BindlessManager.Dispose();
        CommonUbo.Dispose();
        Swapchain.Dispose();
        Context.Dispose();

        _disposed = true;
    }
}

