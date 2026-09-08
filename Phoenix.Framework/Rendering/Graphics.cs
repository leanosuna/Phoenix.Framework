
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
    /// Initializes graphics subsystems, Vulkan context, swapchain, uniform buffers, and viewport.
    /// </summary>
    internal Graphics(PhoenixGame game)
    {
        _game = game;
        Context = new VulkanContext(game.Window, game.Window.Title);
        Swapchain = new VulkanSwapchain(Context, game.Window);
        CommonUbo = new CommonUBOManager(Context);
        RenderContext = new RenderContext(Context, Swapchain, CommonUbo);
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
    /// Creates a graphics pipeline configured for dynamic rendering and depth testing.
    /// Automatically injects the Set 0 CommonUBO descriptor set layout if not present.
    /// </summary>
    public VulkanPipeline CreatePipeline(VulkanPipelineDescription description)
    {
        var layouts = description.DescriptorSetLayouts;
        if (layouts.Length == 0)
        {
            layouts = [CommonUbo.DescriptorSetLayout];
        }
        else if (layouts[0].Handle != CommonUbo.DescriptorSetLayout.Handle)
        {
            layouts = [CommonUbo.DescriptorSetLayout, ..layouts];
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
    /// Disposes uniform buffers, swapchain, and Vulkan device context.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        CommonUbo.Dispose();
        Swapchain.Dispose();
        Context.Dispose();

        _disposed = true;
    }
}

