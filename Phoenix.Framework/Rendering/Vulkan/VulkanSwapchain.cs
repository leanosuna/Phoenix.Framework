using Phoenix;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using System.Numerics;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace Phoenix.Framework.Rendering.Vulkan;

public sealed unsafe class VulkanSwapchain : IDisposable
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
    private Image _depthImage;
    private DeviceMemory _depthImageMemory;
    private ImageView _depthImageView;
    private Format _depthFormat;
    private CommandPool _commandPool;
    private CommandBuffer[] _commandBuffers = new CommandBuffer[MaxFramesInFlight];
    private VkSemaphore[] _imageAvailableSemaphores = new VkSemaphore[MaxFramesInFlight];
    private VkSemaphore[] _renderFinishedSemaphores = [];
    private Fence[] _inFlightFences = new Fence[MaxFramesInFlight];
    private PresentModeKHR _presentMode;
    private int _currentFrame;
    private bool _disposed;

    public SwapchainKHR Swapchain => _swapchain;
    public Format ImageFormat => _imageFormat;
    public Extent2D Extent => _extent;
    public int CurrentFrame => _currentFrame;
    public uint ImageCount => (uint)_images.Length;
    public Image DepthImage => _depthImage;
    public ImageView DepthImageView => _depthImageView;
    public Format DepthFormat => _depthFormat;
    public PresentModeKHR PresentMode => _presentMode;
    public bool IsVSyncEnabled => _presentMode is PresentModeKHR.FifoKhr;


    /// <summary>
    /// Returns the color image view for a given swapchain image index.
    /// </summary>
    public ImageView GetImageView(uint index) => _imageViews[index];

    /// <summary>
    /// Returns the color image handle for a given swapchain image index.
    /// </summary>
    public Image GetImage(uint index) => _images[index];


    /// <summary>
    /// Initializes the Vulkan swapchain, image views, command buffers, and frames-in-flight synchronization primitives.
    /// </summary>
    public VulkanSwapchain(VulkanContext context, IWindow window)
    {
        _context = context;
        _window = window;

        CreateSwapchain(_window.FramebufferSize);
        CreateImageViews();
        CreateDepthResources();
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

        _presentMode = ChooseSwapPresentMode();
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
            PresentMode = _presentMode,
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
    /// Finds a supported depth format, creates the depth buffer image, allocates device memory, and creates the depth image view.
    /// </summary>
    private void CreateDepthResources()
    {
        _depthFormat = _context.FindSupportedFormat(
            [Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint],
            ImageTiling.Optimal,
            FormatFeatureFlags.DepthStencilAttachmentBit);

        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(_extent.Width, _extent.Height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Format = _depthFormat,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = ImageUsageFlags.DepthStencilAttachmentBit,
            SharingMode = SharingMode.Exclusive,
            Samples = SampleCountFlags.Count1Bit
        };

        VulkanHelper.Check(_context.Vk.CreateImage(_context.Device, in imageInfo, null, out _depthImage),
            "Failed to create depth image.");

        _context.Vk.GetImageMemoryRequirements(_context.Device, _depthImage, out var memReqs);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = _context.FindMemoryType(memReqs.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };

        VulkanHelper.Check(_context.Vk.AllocateMemory(_context.Device, in allocInfo, null, out _depthImageMemory),
            "Failed to allocate depth image memory.");

        VulkanHelper.Check(_context.Vk.BindImageMemory(_context.Device, _depthImage, _depthImageMemory, 0),
            "Failed to bind depth image memory.");

        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _depthImage,
            ViewType = ImageViewType.Type2D,
            Format = _depthFormat,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.DepthBit, 0, 1, 0, 1)
        };

        VulkanHelper.Check(_context.Vk.CreateImageView(_context.Device, in viewInfo, null, out _depthImageView),
            "Failed to create depth image view.");
    }

    /// <summary>
    /// Destroys the depth image view, frees depth image device memory, and destroys the depth image.
    /// </summary>
    private void DestroyDepthResources()
    {
        if (_depthImageView.Handle != 0)
        {
            _context.Vk.DestroyImageView(_context.Device, _depthImageView, null);
            _depthImageView = default;
        }

        if (_depthImageMemory.Handle != 0)
        {
            _context.Vk.FreeMemory(_context.Device, _depthImageMemory, null);
            _depthImageMemory = default;
        }

        if (_depthImage.Handle != 0)
        {
            _context.Vk.DestroyImage(_context.Device, _depthImage, null);
            _depthImage = default;
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
    /// Selects the swapchain present mode based on VSync configuration and device capabilities.
    /// When VSync is disabled, prefers Immediate or Mailbox; when enabled, uses Fifo.
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

        PresentModeKHR selected;
        if (!_window.VSync)
        {
            if (modes.Contains(PresentModeKHR.ImmediateKhr))
                selected = PresentModeKHR.ImmediateKhr;
            else if (modes.Contains(PresentModeKHR.MailboxKhr))
                selected = PresentModeKHR.MailboxKhr;
            else
                selected = PresentModeKHR.FifoKhr;
        }
        else
        {
            selected = PresentModeKHR.FifoKhr;
        }

        Log.Info($"[VulkanSwapchain] Present mode selected: {selected} (VSync: {_window.VSync}, Available: [{string.Join(", ", modes)}])");
        return selected;
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

        if (_extent.Width == (uint)newSize.X && _extent.Height == (uint)newSize.Y && IsVSyncEnabled == _window.VSync)
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

        DestroyDepthResources();

        var oldSwapchain = _swapchain;
        CreateSwapchain(newSize, oldSwapchain);
        _context.KhrSwapchain.DestroySwapchain(_context.Device, oldSwapchain, null);

        CreateImageViews();
        CreateDepthResources();
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

        DestroyDepthResources();

        for (int i = 0; i < _imageViews.Length; i++)
        {
            _context.Vk.DestroyImageView(_context.Device, _imageViews[i], null);
        }

        _context.KhrSwapchain.DestroySwapchain(_context.Device, _swapchain, null);

        _disposed = true;
    }
}

