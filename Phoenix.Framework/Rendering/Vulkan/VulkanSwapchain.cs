using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using System.Numerics;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace Phoenix.Framework.Rendering.Vulkan;

internal sealed unsafe class VulkanSwapchain : IDisposable
{
    public const int MaxFramesInFlight = 2;

    private readonly VulkanContext _context;
    private readonly IWindow _window;
    private SwapchainKHR _swapchain;
    private Format _imageFormat;
    private ColorSpaceKHR _colorSpace;
    private Extent2D _extent;
    private Image[] _images = [];
    private ImageView[] _imageViews = [];
    private CommandPool _commandPool;
    private CommandBuffer[] _commandBuffers = new CommandBuffer[MaxFramesInFlight];
    private VkSemaphore[] _imageAvailableSemaphores = new VkSemaphore[MaxFramesInFlight];
    private VkSemaphore[] _renderFinishedSemaphores = [];
    private Fence[] _inFlightFences = new Fence[MaxFramesInFlight];
    private int _currentFrame;
    private bool _disposed;

    public SwapchainKHR Swapchain => _swapchain;
    public Format ImageFormat => _imageFormat;
    public Extent2D Extent => _extent;
    public int CurrentFrame => _currentFrame;
    public uint ImageCount => (uint)_images.Length;

    /// <summary>
    /// Initializes the Vulkan swapchain, image views, command buffers, and frames-in-flight synchronization primitives.
    /// </summary>
    public VulkanSwapchain(VulkanContext context, IWindow window)
    {
        _context = context;
        _window = window;

        CreateSwapchain(_window.FramebufferSize);
        CreateImageViews();
        CreateCommandPoolAndBuffers();
        CreateSyncObjects();
    }

    /// <summary>
    /// Queries surface formats and capabilities to create or recreate the VkSwapchainKHR.
    /// </summary>
    private void CreateSwapchain(Vector2D<int> size, SwapchainKHR oldSwapchain = default)
    {
        var vk = _context.Vk;
        var khrSurface = _context.KhrSurface;
        var physicalDevice = _context.PhysicalDevice;
        var surface = _context.Surface;

        khrSurface.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out var capabilities);

        var surfaceFormat = ChooseSwapSurfaceFormat();
        _imageFormat = surfaceFormat.Format;
        _colorSpace = surfaceFormat.ColorSpace;

        var presentMode = ChooseSwapPresentMode();
        _extent = ChooseSwapExtent(capabilities, size);

        uint imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
            imageCount = capabilities.MaxImageCount;

        SwapchainCreateInfoKHR createInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = surface,
            MinImageCount = imageCount,
            ImageFormat = _imageFormat,
            ImageColorSpace = _colorSpace,
            ImageExtent = _extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = oldSwapchain
        };

        uint* queueFamilyIndices = stackalloc uint[] { _context.GraphicsFamilyIndex, _context.PresentFamilyIndex };
        if (_context.GraphicsFamilyIndex != _context.PresentFamilyIndex)
        {
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            createInfo.PQueueFamilyIndices = queueFamilyIndices;
        }
        else
        {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        VulkanHelper.Check(_context.KhrSwapchain.CreateSwapchain(_context.Device, in createInfo, null, out _swapchain),
            "Failed to create Vulkan swapchain.");

        uint actualImageCount = 0;
        _context.KhrSwapchain.GetSwapchainImages(_context.Device, _swapchain, ref actualImageCount, null);
        _images = new Image[actualImageCount];
        fixed (Image* pImages = _images)
        {
            _context.KhrSwapchain.GetSwapchainImages(_context.Device, _swapchain, ref actualImageCount, pImages);
        }
    }

    /// <summary>
    /// Creates a VkImageView for each swapchain color image.
    /// </summary>
    private void CreateImageViews()
    {
        _imageViews = new ImageView[_images.Length];
        for (int i = 0; i < _images.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = _images[i],
                ViewType = ImageViewType.Type2D,
                Format = _imageFormat,
                Components = new ComponentMapping(
                    ComponentSwizzle.Identity,
                    ComponentSwizzle.Identity,
                    ComponentSwizzle.Identity,
                    ComponentSwizzle.Identity),
                SubresourceRange = new ImageSubresourceRange(
                    ImageAspectFlags.ColorBit,
                    0, 1, 0, 1)
            };

            VulkanHelper.Check(_context.Vk.CreateImageView(_context.Device, in createInfo, null, out _imageViews[i]),
                $"Failed to create swapchain image view at index {i}.");
        }
    }

    /// <summary>
    /// Creates the command pool and allocates primary command buffers for each frame in flight.
    /// </summary>
    private void CreateCommandPoolAndBuffers()
    {
        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = _context.GraphicsFamilyIndex
        };

        VulkanHelper.Check(_context.Vk.CreateCommandPool(_context.Device, in poolInfo, null, out _commandPool),
            "Failed to create Vulkan command pool.");

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = MaxFramesInFlight
        };

        fixed (CommandBuffer* pBuffers = _commandBuffers)
        {
            VulkanHelper.Check(_context.Vk.AllocateCommandBuffers(_context.Device, in allocInfo, pBuffers),
                "Failed to allocate Vulkan command buffers.");
        }
    }

    /// <summary>
    /// Creates semaphores and fences for coordinating rendering and presentation across frames in flight.
    /// </summary>
    private void CreateSyncObjects()
    {
        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            VulkanHelper.Check(_context.Vk.CreateSemaphore(_context.Device, in semaphoreInfo, null, out _imageAvailableSemaphores[i]),
                "Failed to create image available semaphore.");
            VulkanHelper.Check(_context.Vk.CreateFence(_context.Device, in fenceInfo, null, out _inFlightFences[i]),
                "Failed to create in-flight fence.");
        }

        CreateImageSemaphores();
    }

    /// <summary>
    /// Creates dedicated render finished semaphores for each swapchain image.
    /// </summary>
    private void CreateImageSemaphores()
    {
        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };

        _renderFinishedSemaphores = new VkSemaphore[_images.Length];
        for (int i = 0; i < _images.Length; i++)
        {
            VulkanHelper.Check(_context.Vk.CreateSemaphore(_context.Device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]),
                "Failed to create render finished semaphore.");
        }
    }

    /// <summary>
    /// Selects a suitable surface color format, preferring standard 32-bit sRGB formats.
    /// </summary>
    private SurfaceFormatKHR ChooseSwapSurfaceFormat()
    {
        uint count = 0;
        _context.KhrSurface.GetPhysicalDeviceSurfaceFormats(_context.PhysicalDevice, _context.Surface, ref count, null);
        SurfaceFormatKHR[] formats = new SurfaceFormatKHR[count];
        fixed (SurfaceFormatKHR* pFormats = formats)
        {
            _context.KhrSurface.GetPhysicalDeviceSurfaceFormats(_context.PhysicalDevice, _context.Surface, ref count, pFormats);
        }

        foreach (var format in formats)
        {
            if (format.Format is Format.B8G8R8A8Srgb or Format.R8G8B8A8Srgb && format.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
                return format;
        }

        return formats[0];
    }

    /// <summary>
    /// Selects the swapchain present mode, preferring Mailbox for low latency, falling back to Fifo.
    /// </summary>
    private PresentModeKHR ChooseSwapPresentMode()
    {
        uint count = 0;
        _context.KhrSurface.GetPhysicalDeviceSurfacePresentModes(_context.PhysicalDevice, _context.Surface, ref count, null);
        PresentModeKHR[] modes = new PresentModeKHR[count];
        fixed (PresentModeKHR* pModes = modes)
        {
            _context.KhrSurface.GetPhysicalDeviceSurfacePresentModes(_context.PhysicalDevice, _context.Surface, ref count, pModes);
        }

        foreach (var mode in modes)
        {
            if (mode == PresentModeKHR.MailboxKhr)
                return mode;
        }

        return PresentModeKHR.FifoKhr;
    }

    /// <summary>
    /// Calculates the swapchain image extent clamped to the physical device surface capabilities.
    /// </summary>
    private static Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities, Vector2D<int> size)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
            return capabilities.CurrentExtent;

        uint width = Math.Clamp((uint)size.X, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
        uint height = Math.Clamp((uint)size.Y, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);
        return new Extent2D(width, height);
    }

    /// <summary>
    /// Waits on the current frame fence and acquires the next swapchain image index.
    /// </summary>
    public bool AcquireNextImage(out uint imageIndex)
    {
        imageIndex = 0;
        var vk = _context.Vk;

        vk.WaitForFences(_context.Device, 1, in _inFlightFences[_currentFrame], true, ulong.MaxValue);

        var result = _context.KhrSwapchain.AcquireNextImage(
            _context.Device,
            _swapchain,
            ulong.MaxValue,
            _imageAvailableSemaphores[_currentFrame],
            default,
            ref imageIndex);

        switch (result)
        {
            case Result.ErrorOutOfDateKhr:
                Recreate(_window.FramebufferSize);
                return false;
            case Result.Success or Result.SuboptimalKhr:
                vk.ResetFences(_context.Device, 1, in _inFlightFences[_currentFrame]);
                return true;
            case var error:
                VulkanHelper.Check(error, "Failed to acquire next swapchain image.");
                return false;
        }
    }

    /// <summary>
    /// Resets and begins recording into the command buffer for the current frame in flight.
    /// </summary>
    public CommandBuffer BeginCommandBuffer()
    {
        var cmd = _commandBuffers[_currentFrame];
        _context.Vk.ResetCommandBuffer(cmd, CommandBufferResetFlags.None);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        VulkanHelper.Check(_context.Vk.BeginCommandBuffer(cmd, in beginInfo),
            "Failed to begin recording Vulkan command buffer.");

        return cmd;
    }

    /// <summary>
    /// Records a dynamic rendering pass that transitions the swapchain image, clears it with a color, and transitions for presentation.
    /// </summary>
    public void RecordClearPass(CommandBuffer cmd, uint imageIndex, Vector4 clearColor)
    {
        var vk = _context.Vk;
        var image = _images[imageIndex];
        var imageView = _imageViews[imageIndex];

        ImageMemoryBarrier2 toColorAttachment = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            SrcAccessMask = AccessFlags2.None,
            DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags2.ColorAttachmentWriteBit,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.ColorAttachmentOptimal,
            Image = image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        DependencyInfo toColorDep = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &toColorAttachment
        };

        vk.CmdPipelineBarrier2(cmd, in toColorDep);

        ClearColorValue colorValue = new(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        RenderingAttachmentInfo colorAttachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = imageView,
            ImageLayout = ImageLayout.ColorAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new ClearValue { Color = colorValue }
        };

        RenderingInfo renderingInfo = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new Rect2D(new Offset2D(0, 0), _extent),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachment
        };

        vk.CmdBeginRendering(cmd, in renderingInfo);

        Viewport viewport = new(0, 0, _extent.Width, _extent.Height, 0.0f, 1.0f);
        vk.CmdSetViewport(cmd, 0, 1, in viewport);

        Rect2D scissor = new(new Offset2D(0, 0), _extent);
        vk.CmdSetScissor(cmd, 0, 1, in scissor);

        vk.CmdEndRendering(cmd);

        ImageMemoryBarrier2 toPresentSrc = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            SrcAccessMask = AccessFlags2.ColorAttachmentWriteBit,
            DstStageMask = PipelineStageFlags2.BottomOfPipeBit,
            DstAccessMask = AccessFlags2.None,
            OldLayout = ImageLayout.ColorAttachmentOptimal,
            NewLayout = ImageLayout.PresentSrcKhr,
            Image = image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        DependencyInfo toPresentDep = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &toPresentSrc
        };

        vk.CmdPipelineBarrier2(cmd, in toPresentDep);
    }

    /// <summary>
    /// Ends command buffer recording, submits it to the graphics queue, and queues the swapchain image for presentation.
    /// </summary>
    public void SubmitAndPresent(CommandBuffer cmd, uint imageIndex)
    {
        var vk = _context.Vk;
        VulkanHelper.Check(vk.EndCommandBuffer(cmd), "Failed to end Vulkan command buffer.");

        PipelineStageFlags waitStage = PipelineStageFlags.ColorAttachmentOutputBit;
        var waitSemaphore = _imageAvailableSemaphores[_currentFrame];
        var signalSemaphore = _renderFinishedSemaphores[imageIndex];
        var fence = _inFlightFences[_currentFrame];

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = &waitStage,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore
        };

        VulkanHelper.Check(vk.QueueSubmit(_context.GraphicsQueue, 1, in submitInfo, fence),
            "Failed to submit command buffer to graphics queue.");

        var swapchain = _swapchain;
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex
        };

        var presentResult = _context.KhrSwapchain.QueuePresent(_context.PresentQueue, in presentInfo);
        switch (presentResult)
        {
            case Result.ErrorOutOfDateKhr or Result.SuboptimalKhr:
                Recreate(_window.FramebufferSize);
                break;
            case Result.Success:
                break;
            case var error:
                VulkanHelper.Check(error, "Failed to present swapchain image.");
                break;
        }

        _currentFrame = (_currentFrame + 1) % MaxFramesInFlight;
    }

    /// <summary>
    /// Recreates the swapchain, image views, and extent when the window size changes.
    /// </summary>
    public void Recreate(Vector2D<int> newSize)
    {
        if (newSize.X <= 0 || newSize.Y <= 0)
            return;

        _context.Vk.DeviceWaitIdle(_context.Device);

        for (int i = 0; i < _renderFinishedSemaphores.Length; i++)
        {
            _context.Vk.DestroySemaphore(_context.Device, _renderFinishedSemaphores[i], null);
        }

        for (int i = 0; i < _imageViews.Length; i++)
        {
            _context.Vk.DestroyImageView(_context.Device, _imageViews[i], null);
        }

        var oldSwapchain = _swapchain;
        CreateSwapchain(newSize, oldSwapchain);
        _context.KhrSwapchain.DestroySwapchain(_context.Device, oldSwapchain, null);

        CreateImageViews();
        CreateImageSemaphores();
    }

    /// <summary>
    /// Destroys all swapchain images, synchronization objects, and command buffers.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _context.Vk.DeviceWaitIdle(_context.Device);

        for (int i = 0; i < _renderFinishedSemaphores.Length; i++)
        {
            _context.Vk.DestroySemaphore(_context.Device, _renderFinishedSemaphores[i], null);
        }

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            _context.Vk.DestroySemaphore(_context.Device, _imageAvailableSemaphores[i], null);
            _context.Vk.DestroyFence(_context.Device, _inFlightFences[i], null);
        }

        _context.Vk.DestroyCommandPool(_context.Device, _commandPool, null);

        for (int i = 0; i < _imageViews.Length; i++)
        {
            _context.Vk.DestroyImageView(_context.Device, _imageViews[i], null);
        }

        _context.KhrSwapchain.DestroySwapchain(_context.Device, _swapchain, null);

        _disposed = true;
    }
}
