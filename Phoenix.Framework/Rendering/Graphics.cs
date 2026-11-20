
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
    /// Gets the FrameUniforms uniform buffer manager.
    /// </summary>
    internal FrameUniformsManager FrameUniformsManager { get; private set; } = default!;

    /// <summary>
    /// Gets the global bindless texture manager for Set 1.
    /// </summary>
    public BindlessManager BindlessManager { get; private set; } = default!;

    /// <summary>
    /// Convenient alias for BindlessManager.
    /// </summary>
    public BindlessManager Bindless => BindlessManager;

    /// <summary>
    /// Gets the active rendering command recording context.
    /// </summary>
    public RenderContext RenderContext { get; private set; } = default!;

    /// <summary>
    /// Gets the viewport scaling and filtering controller.
    /// </summary>
    public RenderViewport Viewport { get; private set; } = default!;

    /// <summary>
    /// Gets the default FrameUniforms descriptor set layout.
    /// </summary>
    public DescriptorSetLayout FrameUniformsLayout => FrameUniformsManager.DescriptorSetLayout;

    /// <summary>
    /// Gets the bindless textures descriptor set layout.
    /// </summary>
    public DescriptorSetLayout BindlessLayout => BindlessManager.DescriptorSetLayout;

    /// <summary>
    /// Gets the descriptor set layout for skeletal animation bone uniform buffers (Set 3).
    /// </summary>
    public DescriptorSetLayout BoneLayout { get; private set; }

    /// <summary>
    /// Gets the descriptor set layout for multi-draw indirect submesh metadata storage buffers (Set 2).
    /// </summary>
    public DescriptorSetLayout IndirectMetadataLayout { get; private set; }

    private VulkanDescriptorAllocator? _descriptorAllocator;

    /// <summary>
    /// Gets the shared growable descriptor allocator used for dynamic uniform and storage buffer descriptor sets.
    /// </summary>
    public VulkanDescriptorAllocator DescriptorAllocator => _descriptorAllocator!;

    private readonly List<VulkanRenderTarget> _screenTargets = new();

    /// <summary>
    /// Gets the current render viewport width in pixels.
    /// </summary>
    public uint RenderWidth => Viewport.Width;

    /// <summary>
    /// Gets the current render viewport height in pixels.
    /// </summary>
    public uint RenderHeight => Viewport.Height;

    /// <summary>
    /// Gets the default white fallback texture from the bindless manager.
    /// </summary>
    public VulkanTexture? DefaultWhiteTexture => BindlessManager?.DefaultWhiteTexture;

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
    internal Graphics(PhoenixGame game, bool enableValidation = false)
    {
        _game = game;
        Context = new VulkanContext(game.Window, game.Window.Title, enableValidation);
        Swapchain = new VulkanSwapchain(Context, game.Window);
        FrameUniformsManager = new FrameUniformsManager(Context);
        BindlessManager = new BindlessManager(Context);
        BoneLayout = CreateBoneLayout();
        IndirectMetadataLayout = CreateStorageBufferLayout(ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0);
        _descriptorAllocator = new VulkanDescriptorAllocator(Context);
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

    /// <summary>
    /// Creates an offscreen render target scaled relative to the viewport resolution that auto-resizes when the viewport scale or window size changes.
    /// </summary>
    public VulkanRenderTarget CreateScreenRenderTarget(
        Format[] colorFormats,
        bool hasDepth = true,
        float scale = 1.0f,
        SamplerDescription? samplerDesc = null,
        Format? depthFormat = null)
    {
        uint w = (uint)Math.Max(1, (int)(RenderWidth * scale));
        uint h = (uint)Math.Max(1, (int)(RenderHeight * scale));
        var rt = new VulkanRenderTarget(Context, BindlessManager, w, h, colorFormats, hasDepth, depthFormat, samplerDesc);
        rt.ViewportScale = scale;
        _screenTargets.Add(rt);
        return rt;
    }

    /// <summary>
    /// Creates a single color attachment render target scaled relative to the viewport resolution that auto-resizes.
    /// </summary>
    public VulkanRenderTarget CreateScreenRenderTarget(
        float scale = 1.0f,
        Format colorFormat = Format.R16G16B16A16Sfloat,
        bool hasDepth = true,
        SamplerDescription? samplerDesc = null)
    {
        return CreateScreenRenderTarget([colorFormat], hasDepth, scale, samplerDesc);
    }

    /// <summary>
    /// Unregisters an auto-resizing screen render target.
    /// </summary>
    public void UnregisterScreenRenderTarget(VulkanRenderTarget target)
    {
        _screenTargets.Remove(target);
    }

    internal void HandleViewportResize()
    {
        if (SceneRenderTarget != null && (SceneRenderTarget.Width != Viewport.Width || SceneRenderTarget.Height != Viewport.Height))
        {
            Context.WaitIdle();
            SceneRenderTarget.Resize(Viewport.Width, Viewport.Height);
        }

        for (int i = _screenTargets.Count - 1; i >= 0; i--)
        {
            var rt = _screenTargets[i];
            uint w = (uint)Math.Max(1, (int)(RenderWidth * rt.ViewportScale));
            uint h = (uint)Math.Max(1, (int)(RenderHeight * rt.ViewportScale));
            rt.Resize(w, h);
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
    /// Creates a fluent graphics pipeline builder.
    /// </summary>
    public PipelineBuilder CreatePipeline() => new(this);

    /// <summary>
    /// Creates a fluent compute pipeline builder.
    /// </summary>
    public ComputePipelineBuilder CreateComputePipeline() => new(this);

    /// <summary>
    /// Creates a graphics pipeline configured for dynamic rendering and depth testing.
    /// Automatically injects Set 0 (FrameUniforms), Set 1 (Bindless), and Set 2 (IndirectMetadata) descriptor set layouts if not present.
    /// </summary>
    public VulkanPipeline CreatePipeline(VulkanPipelineDescription description)
    {
        var layouts = description.DescriptorSetLayouts;
        if (layouts.Length > 0 && layouts[0].Handle == FrameUniformsManager.DescriptorSetLayout.Handle)
        {
            return new VulkanPipeline(Context, description);
        }

        List<DescriptorSetLayout> list = [FrameUniformsManager.DescriptorSetLayout, BindlessManager.DescriptorSetLayout, IndirectMetadataLayout];
        if (layouts.Length > 0)
        {
            list.AddRange(layouts);
        }

        var descWithUbo = description with { DescriptorSetLayouts = [.. list] };
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
    /// When hostVisible is true, the buffer is mapped for low-latency per-frame CPU updates without staging.
    /// </summary>
    public VulkanStorageBuffer CreateStorageBuffer(ulong size, bool hostVisible = false)
    {
        return new VulkanStorageBuffer(Context, size, hostVisible);
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
    /// Allocates a descriptor set for a bone uniform buffer from the growable descriptor allocator.
    /// </summary>
    public unsafe (DescriptorPool Pool, DescriptorSet Set) CreateBoneDescriptorSet(VulkanBuffer buffer)
    {
        var set = _descriptorAllocator!.Allocate(BoneLayout);

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

        return (default, set);
    }

    /// <summary>
    /// Frees a previously allocated bone descriptor set back to the allocator.
    /// </summary>
    public void FreeBoneDescriptorSet(DescriptorSet set) => _descriptorAllocator?.Free(set);

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
    /// Allocates a descriptor set bound to a storage buffer from the growable descriptor allocator.
    /// </summary>
    public unsafe (DescriptorPool Pool, DescriptorSet Set) CreateStorageBufferDescriptorSet(DescriptorSetLayout layout, VulkanBuffer buffer, ulong size, uint binding = 0)
    {
        var set = _descriptorAllocator!.Allocate(layout);

        DescriptorBufferInfo bufferInfo = new()
        {
            Buffer = buffer.Buffer,
            Offset = 0,
            Range = size
        };

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

        return (default, set);
    }

    /// <summary>
    /// Allocates a descriptor set bound to a storage buffer from the growable descriptor allocator.
    /// </summary>
    public (DescriptorPool Pool, DescriptorSet Set) CreateStorageBufferDescriptorSet(DescriptorSetLayout layout, VulkanStorageBuffer buffer, uint binding = 0)
    {
        return CreateStorageBufferDescriptorSet(layout, buffer.DeviceBuffer, buffer.Size, binding);
    }

    /// <summary>
    /// Frees a previously allocated storage buffer descriptor set back to the allocator.
    /// </summary>
    public void FreeStorageBufferDescriptorSet(DescriptorSet set) => _descriptorAllocator?.Free(set);

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
    /// Captures the current rendered scene or presented frame to a PNG image file on disk.
    /// </summary>
    public void TakeScreenshot(string filePath, bool fromSceneTarget = true)
    {
        WaitIdle();

        SceneRenderTarget?.ColorTexture?.SaveToFile(filePath, ImageLayout.ShaderReadOnlyOptimal);
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

        if (IndirectMetadataLayout.Handle != 0)
        {
            unsafe
            {
                Context.Vk.DestroyDescriptorSetLayout(Context.Device, IndirectMetadataLayout, null);
            }
            IndirectMetadataLayout = default;
        }

        _descriptorAllocator?.Dispose();
        _descriptorAllocator = null;

        for (int i = _screenTargets.Count - 1; i >= 0; i--)
        {
            _screenTargets[i]?.Dispose();
        }
        _screenTargets.Clear();

        SceneRenderTarget?.Dispose();
        BindlessManager.Dispose();
        FrameUniformsManager.Dispose();
        Swapchain.Dispose();
        Context.Dispose();

        _disposed = true;
    }
}

