
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
    /// Initializes graphics subsystems, Vulkan context, swapchain, uniform buffers, bindless manager, and viewport.
    /// </summary>
    internal Graphics(PhoenixGame game)
    {
        _game = game;
        Context = new VulkanContext(game.Window, game.Window.Title);
        Swapchain = new VulkanSwapchain(Context, game.Window);
        CommonUbo = new CommonUBOManager(Context);
        BindlessManager = new BindlessManager(Context);
        RenderContext = new RenderContext(Context, Swapchain, CommonUbo, BindlessManager);
        Viewport = new RenderViewport(game);
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
    public VulkanTexture CreateTexture(int width, int height, ReadOnlySpan<byte> rgbaPixels, Format format = Format.R8G8B8A8Unorm)
    {
        return new VulkanTexture(Context, BindlessManager, width, height, rgbaPixels, format);
    }

    /// <summary>
    /// Creates a solid color 2D texture of the specified dimensions.
    /// </summary>
    public VulkanTexture CreateTexture2D(int width, int height, Vector4 color)
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

        return CreateTexture(width, height, pixels);
    }

    /// <summary>
    /// Loads an image file from disk and uploads it as a bindless texture.
    /// </summary>
    public VulkanTexture LoadTexture(string filePath, Format format = Format.R8G8B8A8Srgb)
    {
        return VulkanTexture.FromFile(Context, BindlessManager, filePath, format);
    }

    /// <summary>
    /// Loads an image from a stream and uploads it as a bindless texture.
    /// </summary>
    public VulkanTexture LoadTexture(Stream stream, Format format = Format.R8G8B8A8Srgb)
    {
        return VulkanTexture.FromStream(Context, BindlessManager, stream, format);
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
    /// Recreates swapchain resources upon window resize.
    /// </summary>
    internal void Resize(Vector2D<int> size)
    {
        Swapchain.Recreate(size);
    }

    /// <summary>
    /// Disposes bindless manager, uniform buffers, swapchain, and Vulkan device context.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        BindlessManager.Dispose();
        CommonUbo.Dispose();
        Swapchain.Dispose();
        Context.Dispose();

        _disposed = true;
    }
}

