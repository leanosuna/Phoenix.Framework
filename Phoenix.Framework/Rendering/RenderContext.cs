using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Phoenix.Framework.Rendering;

/// <summary>
/// Encapsulates command recording for dynamic rendering passes, pipeline bindings, compute dispatch, and indirect drawing.
/// </summary>
public sealed unsafe class RenderContext
{
    private readonly Graphics _graphics;
    private readonly VulkanContext _context;
    private readonly VulkanSwapchain _swapchain;
    private readonly FrameUniformsManager _frameUniformsManager;
    private readonly BindlessManager _bindlessManager;
    private CommandBuffer _commandBuffer;
    private uint _imageIndex;
    private int _currentFrame;
    private bool _passActive;
    private VulkanRenderTarget? _activeTarget;
    private uint _currentFrameUniformOffset;
    private VulkanPipeline? _boundPipeline;
    private Silk.NET.Vulkan.Buffer _boundVertexBuffer;
    private Silk.NET.Vulkan.Buffer _boundIndexBuffer;
    private bool _dynamicStateOverridden;

    /// <summary>
    /// Gets current frame uniforms from the active view slot.
    /// </summary>
    public FrameUniforms CurrentFrameUniforms => _frameUniformsManager.CurrentUniforms;

    /// <summary>
    /// Gets whether a render pass or blit has been executed in the current frame.
    /// </summary>
    public bool HasRenderedPass { get; private set; }

    /// <summary>
    /// Gets whether a render pass was executed specifically onto the scene render target this frame.
    /// </summary>
    public bool HasRenderedToSceneTarget { get; private set; }

    /// <summary>
    /// Gets whether a dynamic rendering pass is currently active.
    /// </summary>
    public bool IsPassActive => _passActive;

    /// <summary>
    /// Gets active command buffer handle.
    /// </summary>
    public CommandBuffer CommandBuffer => _commandBuffer;

    /// <summary>
    /// Gets current swapchain image index.
    /// </summary>
    public uint ImageIndex => _imageIndex;

    /// <summary>
    /// Gets current in-flight frame index.
    /// </summary>
    public int CurrentFrame => _currentFrame;

    /// <summary>
    /// Gets currently active offscreen render target, or null if rendering directly to swapchain.
    /// </summary>
    public VulkanRenderTarget? ActiveRenderTarget => _activeTarget;

    /// <summary>
    /// Initializes rendering context bound to graphics device subsystems.
    /// </summary>
    internal RenderContext(Graphics graphics)
    {
        _graphics = graphics;
        _context = graphics.Context;
        _swapchain = graphics.Swapchain;
        _frameUniformsManager = graphics.FrameUniformsManager;
        _bindlessManager = graphics.BindlessManager;
    }

    /// <summary>
    /// Prepares rendering context with the active command buffer and frame indices for the current frame.
    /// </summary>
    internal void Prepare(CommandBuffer commandBuffer, uint imageIndex, int currentFrame)
    {
        _commandBuffer = commandBuffer;
        _imageIndex = imageIndex;
        _currentFrame = currentFrame;
        _passActive = false;
        _currentFrameUniformOffset = 0;
        _boundPipeline = null;
        _boundVertexBuffer = default;
        _boundIndexBuffer = default;
        _dynamicStateOverridden = false;
        HasRenderedPass = false;
        HasRenderedToSceneTarget = false;
    }

    /// <summary>
    /// Begins a scoped dynamic rendering pass. Routes to scaled scene render target when viewport scaling is enabled,
    /// or directly to the swapchain backbuffer otherwise.
    /// </summary>
    public ScopedPass BeginPass(Vector4? clearColor = null, float? clearDepth = 1.0f)
    {
        Vector4 color = clearColor ?? _graphics.ClearColor;
        if (_graphics.Viewport.Enabled && _graphics.SceneRenderTarget != null)
        {
            return BeginPass(_graphics.SceneRenderTarget, color, clearDepth ?? 1.0f);
        }

        return BeginSwapchainPass(color, clearDepth ?? 1.0f);
    }

    /// <summary>
    /// Begins a scoped dynamic rendering pass directly on the swapchain image with the specified pass configuration.
    /// </summary>
    public ScopedPass BeginSwapchainPass(RenderPassConfig config)
    {
        if (_passActive)
            throw new InvalidOperationException("A render pass is already active on this context.");

        _boundPipeline = null;
        _boundVertexBuffer = default;
        _boundIndexBuffer = default;
        _dynamicStateOverridden = false;

        var vk = _context.Vk;
        var colorImage = _swapchain.GetImage(_imageIndex);
        var depthImage = _swapchain.DepthImage;

        bool isClear = config.ClearColor.HasValue;
        ImageMemoryBarrier2 colorBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            SrcAccessMask = isClear ? AccessFlags2.None : AccessFlags2.ColorAttachmentWriteBit,
            DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags2.ColorAttachmentWriteBit | (isClear ? AccessFlags2.None : AccessFlags2.ColorAttachmentReadBit),
            OldLayout = isClear ? ImageLayout.Undefined : ImageLayout.PresentSrcKhr,
            NewLayout = ImageLayout.ColorAttachmentOptimal,
            Image = colorImage,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        ImageMemoryBarrier2 depthBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.EarlyFragmentTestsBit | PipelineStageFlags2.LateFragmentTestsBit,
            SrcAccessMask = config.ClearDepth.HasValue ? AccessFlags2.None : AccessFlags2.DepthStencilAttachmentWriteBit,
            DstStageMask = PipelineStageFlags2.EarlyFragmentTestsBit | PipelineStageFlags2.LateFragmentTestsBit,
            DstAccessMask = AccessFlags2.DepthStencilAttachmentWriteBit | AccessFlags2.DepthStencilAttachmentReadBit,
            OldLayout = config.ClearDepth.HasValue ? ImageLayout.Undefined : ImageLayout.DepthAttachmentOptimal,
            NewLayout = ImageLayout.DepthAttachmentOptimal,
            Image = depthImage,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.DepthBit, 0, 1, 0, 1)
        };

        ImageMemoryBarrier2* barriers = stackalloc ImageMemoryBarrier2[] { colorBarrier, depthBarrier };
        DependencyInfo depInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 2,
            PImageMemoryBarriers = barriers
        };

        vk.CmdPipelineBarrier2(_commandBuffer, in depInfo);

        Vector4 col = config.ClearColor ?? Vector4.Zero;
        ClearColorValue colorValue = new(col.X, col.Y, col.Z, col.W);
        RenderingAttachmentInfo colorAttachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = _swapchain.GetImageView(_imageIndex),
            ImageLayout = ImageLayout.ColorAttachmentOptimal,
            LoadOp = isClear ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new ClearValue { Color = colorValue }
        };

        ClearDepthStencilValue depthValue = new(config.ClearDepth ?? 1.0f, 0);
        RenderingAttachmentInfo depthAttachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = _swapchain.DepthImageView,
            ImageLayout = ImageLayout.DepthAttachmentOptimal,
            LoadOp = config.ClearDepth.HasValue ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
            StoreOp = config.StoreDepth ? AttachmentStoreOp.Store : AttachmentStoreOp.DontCare,
            ClearValue = new ClearValue { DepthStencil = depthValue }
        };

        RenderingInfo renderingInfo = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new Rect2D(new Offset2D(0, 0), _swapchain.Extent),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachment,
            PDepthAttachment = &depthAttachment
        };

        vk.CmdBeginRendering(_commandBuffer, in renderingInfo);

        Viewport viewport = new(0, _swapchain.Extent.Height, _swapchain.Extent.Width, -(float)_swapchain.Extent.Height, 0.0f, 1.0f);
        vk.CmdSetViewport(_commandBuffer, 0, 1, in viewport);

        Rect2D scissor = new(new Offset2D(0, 0), _swapchain.Extent);
        vk.CmdSetScissor(_commandBuffer, 0, 1, in scissor);

        _activeTarget = null;
        _passActive = true;
        HasRenderedPass = true;

        return new ScopedPass(this);
    }

    /// <summary>
    /// Transitions swapchain color and depth attachments and begins a scoped dynamic rendering pass directly on the swapchain image.
    /// </summary>
    public ScopedPass BeginSwapchainPass(Vector4 clearColor, float clearDepth = 1.0f)
    {
        return BeginSwapchainPass(RenderPassConfig.Clear(clearColor, clearDepth));
    }

    /// <summary>
    /// Transitions offscreen color attachments and depth buffer and begins a scoped dynamic rendering pass with the specified config.
    /// </summary>
    public ScopedPass BeginPass(VulkanRenderTarget target, RenderPassConfig config)
    {
        return BeginPassInternal(target, config);
    }

    /// <summary>
    /// Transitions offscreen color attachments and depth buffer and begins a scoped dynamic rendering pass on a render target.
    /// </summary>
    public ScopedPass BeginPass(VulkanRenderTarget target, Vector4 clearColor, float clearDepth = 1.0f)
    {
        return BeginPass(target, RenderPassConfig.Clear(clearColor, clearDepth));
    }

    /// <summary>
    /// Transitions offscreen multi-render-target attachments and begins a scoped dynamic rendering pass with individual clear colors.
    /// </summary>
    public ScopedPass BeginPass(VulkanRenderTarget target, ReadOnlySpan<Vector4> clearColors, float clearDepth = 1.0f)
    {
        return BeginPass(target, RenderPassConfig.ClearMrt(clearColors.ToArray(), clearDepth));
    }

    private ScopedPass BeginPassInternal(VulkanRenderTarget target, RenderPassConfig config)
    {
        if (_passActive)
            throw new InvalidOperationException("A render pass is already active on this context.");

        _boundPipeline = null;
        _boundVertexBuffer = default;
        _boundIndexBuffer = default;
        _dynamicStateOverridden = false;

        var vk = _context.Vk;
        int colorCount = target.ColorAttachmentCount;
        int totalBarriers = colorCount + (target.HasDepth ? 1 : 0);

        if (totalBarriers > 0)
        {
            var barriers = stackalloc ImageMemoryBarrier2[totalBarriers];
            for (int i = 0; i < colorCount; i++)
            {
                var texture = target.GetColorTexture(i);
                bool loadColor = !config.ClearColor.HasValue && (config.ClearColors == null || i >= config.ClearColors.Length);
                var oldLayout = loadColor ? texture.CurrentLayout : ImageLayout.Undefined;
                if (loadColor && oldLayout == ImageLayout.Undefined)
                {
                    oldLayout = ImageLayout.ShaderReadOnlyOptimal;
                }

                barriers[i] = new ImageMemoryBarrier2
                {
                    SType = StructureType.ImageMemoryBarrier2,
                    SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit | PipelineStageFlags2.FragmentShaderBit | PipelineStageFlags2.TransferBit,
                    SrcAccessMask = AccessFlags2.ColorAttachmentWriteBit | AccessFlags2.ShaderReadBit | AccessFlags2.TransferReadBit,
                    DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
                    DstAccessMask = AccessFlags2.ColorAttachmentWriteBit | (loadColor ? AccessFlags2.ColorAttachmentReadBit : AccessFlags2.None),
                    OldLayout = oldLayout,
                    NewLayout = ImageLayout.ColorAttachmentOptimal,
                    Image = texture.Image,
                    SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
                };
                texture.CurrentLayout = ImageLayout.ColorAttachmentOptimal;
            }

            if (target.HasDepth)
            {
                ImageLayout oldDepthLayout = config.ClearDepth.HasValue
                    ? ImageLayout.Undefined
                    : (target.DepthLayout == ImageLayout.Undefined ? ImageLayout.DepthStencilAttachmentOptimal : target.DepthLayout);

                barriers[colorCount] = new ImageMemoryBarrier2
                {
                    SType = StructureType.ImageMemoryBarrier2,
                    SrcStageMask = PipelineStageFlags2.EarlyFragmentTestsBit | PipelineStageFlags2.LateFragmentTestsBit | PipelineStageFlags2.FragmentShaderBit,
                    SrcAccessMask = AccessFlags2.DepthStencilAttachmentWriteBit | AccessFlags2.ShaderReadBit,
                    DstStageMask = PipelineStageFlags2.EarlyFragmentTestsBit | PipelineStageFlags2.LateFragmentTestsBit,
                    DstAccessMask = AccessFlags2.DepthStencilAttachmentWriteBit | AccessFlags2.DepthStencilAttachmentReadBit,
                    OldLayout = oldDepthLayout,
                    NewLayout = ImageLayout.DepthStencilAttachmentOptimal,
                    Image = target.DepthImage,
                    SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.DepthBit, 0, 1, 0, 1)
                };
                target.DepthLayout = ImageLayout.DepthStencilAttachmentOptimal;
            }

            DependencyInfo depInfo = new()
            {
                SType = StructureType.DependencyInfo,
                ImageMemoryBarrierCount = (uint)totalBarriers,
                PImageMemoryBarriers = barriers
            };

            vk.CmdPipelineBarrier2(_commandBuffer, in depInfo);
        }

        RenderingAttachmentInfo* colorAttachments = null;
        if (colorCount > 0)
        {
            var colAlloc = stackalloc RenderingAttachmentInfo[colorCount];
            for (int i = 0; i < colorCount; i++)
            {
                Vector4 col = config.ClearColors != null && i < config.ClearColors.Length
                    ? config.ClearColors[i]
                    : (config.ClearColor ?? Vector4.Zero);

                ClearColorValue colorValue = new(col.X, col.Y, col.Z, col.W);
                bool isClear = config.ClearColor.HasValue || (config.ClearColors != null && i < config.ClearColors.Length);

                colAlloc[i] = new RenderingAttachmentInfo
                {
                    SType = StructureType.RenderingAttachmentInfo,
                    ImageView = target.GetColorTexture(i).ImageView,
                    ImageLayout = ImageLayout.ColorAttachmentOptimal,
                    LoadOp = isClear ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                    StoreOp = AttachmentStoreOp.Store,
                    ClearValue = new ClearValue { Color = colorValue }
                };
            }
            colorAttachments = colAlloc;
        }

        RenderingAttachmentInfo depthAttachment = default;
        if (target.HasDepth)
        {
            ClearDepthStencilValue depthValue = new(config.ClearDepth ?? 1.0f, 0);
            depthAttachment = new RenderingAttachmentInfo
            {
                SType = StructureType.RenderingAttachmentInfo,
                ImageView = target.DepthImageView,
                ImageLayout = ImageLayout.DepthStencilAttachmentOptimal,
                LoadOp = config.ClearDepth.HasValue ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                StoreOp = config.StoreDepth ? AttachmentStoreOp.Store : AttachmentStoreOp.DontCare,
                ClearValue = new ClearValue { DepthStencil = depthValue }
            };
        }

        RenderingInfo renderingInfo = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new Rect2D(new Offset2D(0, 0), target.Extent),
            LayerCount = 1,
            ColorAttachmentCount = (uint)colorCount,
            PColorAttachments = colorAttachments,
            PDepthAttachment = target.HasDepth ? &depthAttachment : null
        };

        vk.CmdBeginRendering(_commandBuffer, in renderingInfo);

        Viewport viewport = new(0, target.Height, target.Width, -(float)target.Height, 0.0f, 1.0f);
        vk.CmdSetViewport(_commandBuffer, 0, 1, in viewport);

        Rect2D scissor = new(new Offset2D(0, 0), target.Extent);
        vk.CmdSetScissor(_commandBuffer, 0, 1, in scissor);

        _activeTarget = target;
        _passActive = true;
        HasRenderedPass = true;
        if (target == _graphics.SceneRenderTarget)
            HasRenderedToSceneTarget = true;

        return new ScopedPass(this);
    }

    /// <summary>
    /// Begins a scoped dynamic rendering overlay pass retaining color and depth attachments for gizmo wireframe rendering.
    /// </summary>
    public ScopedPass BeginGizmoPass()
    {
        if (_graphics.Viewport.Enabled && _graphics.SceneRenderTarget != null && HasRenderedToSceneTarget)
        {
            return BeginGizmoPass(_graphics.SceneRenderTarget);
        }

        return BeginSwapchainGizmoPass();
    }

    /// <summary>
    /// Begins a scoped dynamic rendering overlay pass on the specified render target retaining color and depth attachments.
    /// </summary>
    public ScopedPass BeginGizmoPass(VulkanRenderTarget target)
    {
        return BeginPass(target, RenderPassConfig.LoadAll());
    }

    private ScopedPass BeginSwapchainGizmoPass()
    {
        return BeginSwapchainPass(RenderPassConfig.LoadAll());
    }

    /// <summary>
    /// Begins a scoped dynamic rendering overlay pass directly on the swapchain backbuffer preserving existing contents.
    /// </summary>
    public ScopedPass BeginUIPass()
    {
        if (_passActive)
            throw new InvalidOperationException("A render pass is already active on this context.");

        _boundPipeline = null;
        _boundVertexBuffer = default;
        _boundIndexBuffer = default;
        _dynamicStateOverridden = false;

        var vk = _context.Vk;
        var colorImage = _swapchain.GetImage(_imageIndex);

        ImageMemoryBarrier2 colorBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit | PipelineStageFlags2.TransferBit,
            SrcAccessMask = AccessFlags2.ColorAttachmentWriteBit | AccessFlags2.TransferWriteBit,
            DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags2.ColorAttachmentWriteBit | AccessFlags2.ColorAttachmentReadBit,
            OldLayout = ImageLayout.PresentSrcKhr,
            NewLayout = ImageLayout.ColorAttachmentOptimal,
            Image = colorImage,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        DependencyInfo depInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &colorBarrier
        };

        vk.CmdPipelineBarrier2(_commandBuffer, in depInfo);

        RenderingAttachmentInfo colorAttachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = _swapchain.GetImageView(_imageIndex),
            ImageLayout = ImageLayout.ColorAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Load,
            StoreOp = AttachmentStoreOp.Store
        };

        RenderingInfo renderingInfo = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new Rect2D(new Offset2D(0, 0), _swapchain.Extent),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachment,
            PDepthAttachment = null
        };

        vk.CmdBeginRendering(_commandBuffer, in renderingInfo);

        Viewport viewport = new(0, 0, _swapchain.Extent.Width, _swapchain.Extent.Height, 0.0f, 1.0f);
        vk.CmdSetViewport(_commandBuffer, 0, 1, in viewport);

        Rect2D scissor = new(new Offset2D(0, 0), _swapchain.Extent);
        vk.CmdSetScissor(_commandBuffer, 0, 1, in scissor);

        _activeTarget = null;
        _passActive = true;
        HasRenderedPass = true;

        return new ScopedPass(this);
    }

    /// <summary>
    /// Ends active dynamic rendering pass and transitions color attachments to shader-read or presentation layouts.
    /// </summary>
    public void EndPass()
    {
        if (!_passActive)
            return;

        var vk = _context.Vk;
        vk.CmdEndRendering(_commandBuffer);

        if (_activeTarget != null)
        {
            int colorCount = _activeTarget.ColorAttachmentCount;
            bool hasDepth = _activeTarget.HasDepth;
            int totalBarriers = colorCount + (hasDepth ? 1 : 0);

            if (totalBarriers > 0)
            {
                var barriers = stackalloc ImageMemoryBarrier2[totalBarriers];
                for (int i = 0; i < colorCount; i++)
                {
                    var texture = _activeTarget.GetColorTexture(i);
                    barriers[i] = new ImageMemoryBarrier2
                    {
                        SType = StructureType.ImageMemoryBarrier2,
                        SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
                        SrcAccessMask = AccessFlags2.ColorAttachmentWriteBit,
                        DstStageMask = PipelineStageFlags2.FragmentShaderBit | PipelineStageFlags2.ComputeShaderBit | PipelineStageFlags2.TransferBit,
                        DstAccessMask = AccessFlags2.ShaderReadBit | AccessFlags2.TransferReadBit,
                        OldLayout = ImageLayout.ColorAttachmentOptimal,
                        NewLayout = ImageLayout.ShaderReadOnlyOptimal,
                        Image = texture.Image,
                        SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
                    };
                    texture.CurrentLayout = ImageLayout.ShaderReadOnlyOptimal;
                }

                if (hasDepth)
                {
                    barriers[colorCount] = new ImageMemoryBarrier2
                    {
                        SType = StructureType.ImageMemoryBarrier2,
                        SrcStageMask = PipelineStageFlags2.EarlyFragmentTestsBit | PipelineStageFlags2.LateFragmentTestsBit,
                        SrcAccessMask = AccessFlags2.DepthStencilAttachmentWriteBit,
                        DstStageMask = PipelineStageFlags2.FragmentShaderBit | PipelineStageFlags2.ComputeShaderBit,
                        DstAccessMask = AccessFlags2.ShaderReadBit,
                        OldLayout = ImageLayout.DepthStencilAttachmentOptimal,
                        NewLayout = ImageLayout.ShaderReadOnlyOptimal,
                        Image = _activeTarget.DepthImage,
                        SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.DepthBit, 0, 1, 0, 1)
                    };
                    _activeTarget.DepthLayout = ImageLayout.ShaderReadOnlyOptimal;
                }

                DependencyInfo dep = new()
                {
                    SType = StructureType.DependencyInfo,
                    ImageMemoryBarrierCount = (uint)totalBarriers,
                    PImageMemoryBarriers = barriers
                };

                vk.CmdPipelineBarrier2(_commandBuffer, in dep);
            }

            _activeTarget = null;
        }
        else
        {
            ImageMemoryBarrier2 presentBarrier = new()
            {
                SType = StructureType.ImageMemoryBarrier2,
                SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
                SrcAccessMask = AccessFlags2.ColorAttachmentWriteBit,
                DstStageMask = PipelineStageFlags2.BottomOfPipeBit,
                DstAccessMask = AccessFlags2.None,
                OldLayout = ImageLayout.ColorAttachmentOptimal,
                NewLayout = ImageLayout.PresentSrcKhr,
                Image = _swapchain.GetImage(_imageIndex),
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
            };

            DependencyInfo presentDep = new()
            {
                SType = StructureType.DependencyInfo,
                ImageMemoryBarrierCount = 1,
                PImageMemoryBarriers = &presentBarrier
            };

            vk.CmdPipelineBarrier2(_commandBuffer, in presentDep);
        }

        _boundPipeline = null;
        _boundVertexBuffer = default;
        _boundIndexBuffer = default;
        _dynamicStateOverridden = false;
        _passActive = false;
    }

    /// <summary>
    /// Executes a minimal clear pass transitioning the swapchain image to present layout when no user pass was recorded.
    /// </summary>
    internal void FallbackClearPass(Vector4 clearColor)
    {
        using (BeginSwapchainPass(clearColor))
        {
        }
    }

    /// <summary>
    /// Blits the primary color attachment of an offscreen render target to the current swapchain image with hardware scaling.
    /// </summary>
    public void BlitToSwapchain(VulkanRenderTarget source, Filter filter = Filter.Linear)
    {
        if (_passActive)
            throw new InvalidOperationException("Cannot blit while a render pass is active. Call EndPass() first.");

        var vk = _context.Vk;
        var swapchainImage = _swapchain.GetImage(_imageIndex);
        var srcTexture = source.ColorTexture ?? throw new InvalidOperationException("Cannot blit a depth-only render target to the swapchain.");

        ImageMemoryBarrier2 srcBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit | PipelineStageFlags2.FragmentShaderBit,
            SrcAccessMask = AccessFlags2.ColorAttachmentWriteBit | AccessFlags2.ShaderReadBit,
            DstStageMask = PipelineStageFlags2.BlitBit,
            DstAccessMask = AccessFlags2.TransferReadBit,
            OldLayout = ImageLayout.ShaderReadOnlyOptimal,
            NewLayout = ImageLayout.TransferSrcOptimal,
            Image = srcTexture.Image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        ImageMemoryBarrier2 dstBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.TopOfPipeBit,
            SrcAccessMask = AccessFlags2.None,
            DstStageMask = PipelineStageFlags2.BlitBit,
            DstAccessMask = AccessFlags2.TransferWriteBit,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.TransferDstOptimal,
            Image = swapchainImage,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        ImageMemoryBarrier2* preBarriers = stackalloc ImageMemoryBarrier2[] { srcBarrier, dstBarrier };
        DependencyInfo preDep = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 2,
            PImageMemoryBarriers = preBarriers
        };

        vk.CmdPipelineBarrier2(_commandBuffer, in preDep);

        ImageBlit2 blitRegion = new()
        {
            SType = StructureType.ImageBlit2,
            SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
            DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1)
        };
        blitRegion.SrcOffsets.Element0 = new Offset3D(0, 0, 0);
        blitRegion.SrcOffsets.Element1 = new Offset3D((int)source.Width, (int)source.Height, 1);
        blitRegion.DstOffsets.Element0 = new Offset3D(0, 0, 0);
        blitRegion.DstOffsets.Element1 = new Offset3D((int)_swapchain.Extent.Width, (int)_swapchain.Extent.Height, 1);

        BlitImageInfo2 blitInfo = new()
        {
            SType = StructureType.BlitImageInfo2,
            SrcImage = srcTexture.Image,
            SrcImageLayout = ImageLayout.TransferSrcOptimal,
            DstImage = swapchainImage,
            DstImageLayout = ImageLayout.TransferDstOptimal,
            RegionCount = 1,
            PRegions = &blitRegion,
            Filter = filter
        };

        vk.CmdBlitImage2(_commandBuffer, in blitInfo);

        ImageMemoryBarrier2 postSrcBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.BlitBit,
            SrcAccessMask = AccessFlags2.TransferReadBit,
            DstStageMask = PipelineStageFlags2.FragmentShaderBit | PipelineStageFlags2.ComputeShaderBit,
            DstAccessMask = AccessFlags2.ShaderReadBit,
            OldLayout = ImageLayout.TransferSrcOptimal,
            NewLayout = ImageLayout.ShaderReadOnlyOptimal,
            Image = srcTexture.Image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        ImageMemoryBarrier2 postDstBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.BlitBit,
            SrcAccessMask = AccessFlags2.TransferWriteBit,
            DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit | PipelineStageFlags2.BottomOfPipeBit,
            DstAccessMask = AccessFlags2.ColorAttachmentWriteBit | AccessFlags2.ColorAttachmentReadBit,
            OldLayout = ImageLayout.TransferDstOptimal,
            NewLayout = ImageLayout.PresentSrcKhr,
            Image = swapchainImage,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        ImageMemoryBarrier2* postBarriers = stackalloc ImageMemoryBarrier2[] { postSrcBarrier, postDstBarrier };
        DependencyInfo postDep = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 2,
            PImageMemoryBarriers = postBarriers
        };

        vk.CmdPipelineBarrier2(_commandBuffer, in postDep);

        HasRenderedPass = true;
    }

    /// <summary>
    /// Binds default descriptor sets (Set 0 for FrameUniforms with dynamic offset and Set 1 for bindless textures) based on the pipeline's layout.
    /// </summary>
    public void BindDefaultDescriptorSets(VulkanPipeline pipeline)
    {
        BindDefaultDescriptorSets(pipeline.Layout, pipeline.BindPoint, pipeline.DescriptorSetCount);
    }

    /// <summary>
    /// Binds default descriptor sets using a specific pipeline layout and bind point.
    /// </summary>
    public void BindDefaultDescriptorSets(PipelineLayout layout, PipelineBindPoint bindPoint = PipelineBindPoint.Graphics, uint descriptorSetCount = 2)
    {
        if (descriptorSetCount >= 2)
        {
            DescriptorSet* sets = stackalloc DescriptorSet[]
            {
                _frameUniformsManager.GetDescriptorSet(_currentFrame),
                _bindlessManager.DescriptorSet
            };
            uint offset = _currentFrameUniformOffset;
            _context.Vk.CmdBindDescriptorSets(_commandBuffer, bindPoint, layout, 0, 2, sets, 1, &offset);
        }
        else if (descriptorSetCount == 1)
        {
            var uboSet = _frameUniformsManager.GetDescriptorSet(_currentFrame);
            uint offset = _currentFrameUniformOffset;
            _context.Vk.CmdBindDescriptorSets(_commandBuffer, bindPoint, layout, 0, 1, &uboSet, 1, &offset);
        }
    }

    /// <summary>
    /// Updates the per-pass FrameUniforms buffer slot and immediately rebinds Set 0 if a pipeline is active.
    /// </summary>
    public void SetFrameUniforms(in FrameUniforms uniforms)
    {
        _currentFrameUniformOffset = _frameUniformsManager.AllocateAndWrite(_currentFrame, in uniforms);
        if (_boundPipeline != null)
        {
            var uboSet = _frameUniformsManager.GetDescriptorSet(_currentFrame);
            uint offset = _currentFrameUniformOffset;
            _context.Vk.CmdBindDescriptorSets(_commandBuffer, _boundPipeline.BindPoint, _boundPipeline.Layout, 0, 1, &uboSet, 1, &offset);
        }
    }

    /// <summary>
    /// Sets a per-pass camera with custom view, projection, and position, preserving current timing and resolution.
    /// </summary>
    public void SetCamera(in Matrix4x4 view, in Matrix4x4 projection, Vector3 cameraPosition)
    {
        var current = _frameUniformsManager.CurrentUniforms;
        FrameUniforms uniforms = new(view, projection, cameraPosition, current.Resolution, current.Time, current.DeltaTime);
        SetFrameUniforms(in uniforms);
    }

    /// <summary>
    /// Binds a graphics pipeline, deduplicates redundant bindings, binds default descriptor sets, and applies default dynamic states.
    /// </summary>
    public void BindPipeline(VulkanPipeline pipeline)
    {
        if (_boundPipeline == pipeline)
        {
            if (_dynamicStateOverridden)
            {
                ApplyDefaultDynamicStates(pipeline);
                _dynamicStateOverridden = false;
            }
            return;
        }

        _boundPipeline = pipeline;
        _context.Vk.CmdBindPipeline(_commandBuffer, pipeline.BindPoint, pipeline.Pipeline);
        BindDefaultDescriptorSets(pipeline);
        ApplyDefaultDynamicStates(pipeline);
        _dynamicStateOverridden = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyDefaultDynamicStates(VulkanPipeline pipeline)
    {
        _context.Vk.CmdSetCullMode(_commandBuffer, pipeline.DefaultCullMode);
        _context.Vk.CmdSetFrontFace(_commandBuffer, pipeline.DefaultFrontFace);
        _context.Vk.CmdSetDepthTestEnable(_commandBuffer, pipeline.DefaultDepthTestEnable);
        _context.Vk.CmdSetDepthWriteEnable(_commandBuffer, pipeline.DefaultDepthWriteEnable);
        _context.Vk.CmdSetDepthCompareOp(_commandBuffer, pipeline.DefaultDepthCompareOp);
        _context.Vk.CmdSetDepthBias(_commandBuffer, pipeline.DefaultDepthBiasConstant, pipeline.DefaultDepthBiasClamp, pipeline.DefaultDepthBiasSlope);
    }

    /// <summary>
    /// Overrides the cull mode dynamic state for subsequent draw calls in the current pass.
    /// </summary>
    public void SetCullMode(CullModeFlags cullMode)
    {
        _dynamicStateOverridden = true;
        _context.Vk.CmdSetCullMode(_commandBuffer, cullMode);
    }

    /// <summary>
    /// Overrides the front face winding dynamic state for subsequent draw calls in the current pass.
    /// </summary>
    public void SetFrontFace(FrontFace frontFace)
    {
        _dynamicStateOverridden = true;
        _context.Vk.CmdSetFrontFace(_commandBuffer, frontFace);
    }

    /// <summary>
    /// Overrides depth testing, writing, and compare operator dynamic states for subsequent draw calls in the current pass.
    /// </summary>
    public void SetDepthState(bool testEnable, bool writeEnable, CompareOp compareOp = CompareOp.Less)
    {
        _dynamicStateOverridden = true;
        _context.Vk.CmdSetDepthTestEnable(_commandBuffer, testEnable);
        _context.Vk.CmdSetDepthWriteEnable(_commandBuffer, writeEnable);
        _context.Vk.CmdSetDepthCompareOp(_commandBuffer, compareOp);
    }

    /// <summary>
    /// Overrides depth bias dynamic state parameters for subsequent draw calls in the current pass.
    /// </summary>
    public void SetDepthBias(float constantFactor, float slopeFactor, float clamp = 0f)
    {
        _dynamicStateOverridden = true;
        _context.Vk.CmdSetDepthBias(_commandBuffer, constantFactor, clamp, slopeFactor);
    }

    /// <summary>
    /// Binds a single descriptor set to a specific set index for the current pipeline layout.
    /// </summary>
    public void BindDescriptorSet(VulkanPipeline pipeline, uint setIndex, DescriptorSet descriptorSet)
    {
        _context.Vk.CmdBindDescriptorSets(_commandBuffer, pipeline.BindPoint, pipeline.Layout, setIndex, 1, in descriptorSet, 0, null);
    }

    /// <summary>
    /// Binds multiple descriptor sets starting from the specified set index.
    /// </summary>
    public void BindDescriptorSets(VulkanPipeline pipeline, uint firstSet, ReadOnlySpan<DescriptorSet> descriptorSets)
    {
        fixed (DescriptorSet* pSets = descriptorSets)
        {
            _context.Vk.CmdBindDescriptorSets(_commandBuffer, pipeline.BindPoint, pipeline.Layout, firstSet, (uint)descriptorSets.Length, pSets, 0, null);
        }
    }

    /// <summary>
    /// Uploads push constants data directly to the command buffer with explicit shader stages.
    /// </summary>
    public void PushConstants<T>(VulkanPipeline pipeline, in T data, ShaderStageFlags stages) where T : unmanaged
    {
        fixed (T* pData = &data)
        {
            _context.Vk.CmdPushConstants(
                _commandBuffer,
                pipeline.Layout,
                stages,
                0,
                (uint)sizeof(T),
                pData);
        }
    }

    /// <summary>
    /// Uploads push constants data using the pipeline's configured push constant stage flags.
    /// </summary>
    public void PushConstants<T>(VulkanPipeline pipeline, in T data) where T : unmanaged
    {
        PushConstants(pipeline, in data, pipeline.PushConstantStages);
    }

    /// <summary>
    /// Binds a compute pipeline to the command buffer.
    /// </summary>
    public void BindComputePipeline(VulkanComputePipeline pipeline)
    {
        _context.Vk.CmdBindPipeline(_commandBuffer, PipelineBindPoint.Compute, pipeline.Pipeline);
    }

    /// <summary>
    /// Dispatches compute workgroups across three dimensions.
    /// </summary>
    public void Dispatch(uint groupCountX, uint groupCountY = 1, uint groupCountZ = 1)
    {
        _context.Vk.CmdDispatch(_commandBuffer, groupCountX, groupCountY, groupCountZ);
    }

    /// <summary>
    /// Uploads push constants data for a compute pipeline directly to the command buffer.
    /// </summary>
    public void PushComputeConstants<T>(VulkanComputePipeline pipeline, in T data) where T : unmanaged
    {
        fixed (T* pData = &data)
        {
            _context.Vk.CmdPushConstants(
                _commandBuffer,
                pipeline.Layout,
                ShaderStageFlags.ComputeBit,
                0,
                (uint)sizeof(T),
                pData);
        }
    }

    /// <summary>
    /// Binds a single descriptor set to a compute pipeline at the specified set index.
    /// </summary>
    public void BindComputeDescriptorSet(VulkanComputePipeline pipeline, uint setIndex, DescriptorSet descriptorSet)
    {
        _context.Vk.CmdBindDescriptorSets(_commandBuffer, PipelineBindPoint.Compute, pipeline.Layout, setIndex, 1, in descriptorSet, 0, null);
    }

    /// <summary>
    /// Binds multiple descriptor sets to a compute pipeline starting from the specified set index.
    /// </summary>
    public void BindComputeDescriptorSets(VulkanComputePipeline pipeline, uint firstSet, ReadOnlySpan<DescriptorSet> descriptorSets)
    {
        fixed (DescriptorSet* pSets = descriptorSets)
        {
            _context.Vk.CmdBindDescriptorSets(_commandBuffer, PipelineBindPoint.Compute, pipeline.Layout, firstSet, (uint)descriptorSets.Length, pSets, 0, null);
        }
    }

    /// <summary>
    /// Inserts a pipeline barrier for a storage buffer to synchronize memory access between compute and graphics stages.
    /// </summary>
    public void BufferBarrier(VulkanStorageBuffer buffer, PipelineStageFlags2 srcStage, AccessFlags2 srcAccess, PipelineStageFlags2 dstStage, AccessFlags2 dstAccess)
    {
        BufferMemoryBarrier2 barrier = new()
        {
            SType = StructureType.BufferMemoryBarrier2,
            SrcStageMask = srcStage,
            SrcAccessMask = srcAccess,
            DstStageMask = dstStage,
            DstAccessMask = dstAccess,
            Buffer = buffer.Buffer,
            Offset = 0,
            Size = buffer.Size
        };

        DependencyInfo depInfo = new()
        {
            SType = StructureType.DependencyInfo,
            BufferMemoryBarrierCount = 1,
            PBufferMemoryBarriers = &barrier
        };

        _context.Vk.CmdPipelineBarrier2(_commandBuffer, in depInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsurePassActive()
    {
        if (!_passActive)
            throw new InvalidOperationException("Cannot issue draw commands outside an active render pass. Call BeginPass() or BeginSwapchainPass() first.");
    }

    /// <summary>
    /// Issues an indirect indexed draw call sourced from a GPU storage buffer.
    /// </summary>
    public void DrawIndexedIndirect(VulkanStorageBuffer buffer, ulong offset = 0, uint drawCount = 1, uint stride = 20)
    {
        EnsurePassActive();
        _context.Vk.CmdDrawIndexedIndirect(_commandBuffer, buffer.Buffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Issues an indirect non-indexed draw call sourced from a GPU storage buffer.
    /// </summary>
    public void DrawIndirect(VulkanStorageBuffer buffer, ulong offset = 0, uint drawCount = 1, uint stride = 16)
    {
        EnsurePassActive();
        _context.Vk.CmdDrawIndirect(_commandBuffer, buffer.Buffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Binds a vertex buffer to binding 0 at the specified byte offset, skipping redundant bindings.
    /// </summary>
    public void BindVertexBuffer(VulkanBuffer buffer, ulong offset = 0)
    {
        if (offset == 0 && _boundVertexBuffer.Handle == buffer.Buffer.Handle)
            return;

        var buf = buffer.Buffer;
        _context.Vk.CmdBindVertexBuffers(_commandBuffer, 0, 1, in buf, in offset);
        _boundVertexBuffer = offset == 0 ? buf : default;
    }

    /// <summary>
    /// Binds an index buffer with the specified index format at the given byte offset, skipping redundant bindings.
    /// </summary>
    public void BindIndexBuffer(VulkanBuffer buffer, ulong offset = 0, IndexType indexType = IndexType.Uint32)
    {
        if (offset == 0 && _boundIndexBuffer.Handle == buffer.Buffer.Handle)
            return;

        _context.Vk.CmdBindIndexBuffer(_commandBuffer, buffer.Buffer, offset, indexType);
        _boundIndexBuffer = offset == 0 ? buffer.Buffer : default;
    }

    /// <summary>
    /// Binds an index buffer with index type and byte offset, skipping redundant bindings.
    /// </summary>
    public void BindIndexBuffer(VulkanBuffer buffer, IndexType indexType, ulong offset = 0)
    {
        BindIndexBuffer(buffer, offset, indexType);
    }

    /// <summary>
    /// Issues an indexed draw command for the currently bound vertex and index buffers.
    /// </summary>
    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
    {
        EnsurePassActive();
        _context.Vk.CmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    /// <summary>
    /// Issues a non-indexed primitive draw command for the currently bound vertex buffer.
    /// </summary>
    public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        EnsurePassActive();
        _context.Vk.CmdDraw(_commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
    }

    /// <summary>
    /// Issues a non-indexed 3-vertex draw command for a fullscreen triangle without vertex buffers.
    /// </summary>
    public void DrawFullscreenTriangle()
    {
        Draw(3, 1, 0, 0);
    }

    /// <summary>
    /// Sets dynamic viewport dimensions for the current command buffer.
    /// </summary>
    public void SetViewport(float x, float y, float width, float height, float minDepth = 0.0f, float maxDepth = 1.0f)
    {
        Viewport viewport = new(x, y, width, height, minDepth, maxDepth);
        _context.Vk.CmdSetViewport(_commandBuffer, 0, 1, in viewport);
    }

    /// <summary>
    /// Sets dynamic scissor rectangle bounds for the current command buffer.
    /// </summary>
    public void SetScissor(int x, int y, uint width, uint height)
    {
        Rect2D scissor = new(new Offset2D(x, y), new Extent2D(width, height));
        _context.Vk.CmdSetScissor(_commandBuffer, 0, 1, in scissor);
    }

    /// <summary>
    /// Executes a self-contained fullscreen pass by beginning a render pass, binding pipeline, uploading push constants, drawing a fullscreen triangle, and ending the pass.
    /// </summary>
    public void RunFullscreenPass<TPush>(VulkanRenderTarget? target, VulkanPipeline pipeline, in TPush pushConstants) where TPush : unmanaged
    {
        if (target != null)
            BeginPass(target, Vector4.Zero);
        else
            BeginSwapchainPass(Vector4.Zero);

        BindPipeline(pipeline);
        PushConstants(pipeline, in pushConstants);
        Draw(3, 1, 0, 0);
        EndPass();
    }

    /// <summary>
    /// Dispatches a 1D compute grid calculating ceiling workgroup counts for the specified total thread count.
    /// </summary>
    public void DispatchGroups1D(uint totalThreadsX, uint groupSizeX = 64)
    {
        uint groupCountX = (totalThreadsX + groupSizeX - 1) / groupSizeX;
        Dispatch(Math.Max(1u, groupCountX), 1, 1);
    }

    /// <summary>
    /// Dispatches a 2D compute grid calculating ceiling workgroup counts for the specified 2D resolution.
    /// </summary>
    public void DispatchGroups2D(uint width, uint height, uint groupSizeX = 8, uint groupSizeY = 8)
    {
        uint groupCountX = (width + groupSizeX - 1) / groupSizeX;
        uint groupCountY = (height + groupSizeY - 1) / groupSizeY;
        Dispatch(Math.Max(1u, groupCountX), Math.Max(1u, groupCountY), 1);
    }

    /// <summary>
    /// Inserts a pipeline barrier synchronizing compute shader buffer writes with vertex shader and vertex input attribute reads.
    /// </summary>
    public void BarrierComputeToVertexRead()
    {
        MemoryBarrier2 barrier = new()
        {
            SType = StructureType.MemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ComputeShaderBit,
            SrcAccessMask = AccessFlags2.ShaderWriteBit,
            DstStageMask = PipelineStageFlags2.VertexShaderBit | PipelineStageFlags2.VertexAttributeInputBit,
            DstAccessMask = AccessFlags2.ShaderReadBit | AccessFlags2.VertexAttributeReadBit
        };

        DependencyInfo dep = new()
        {
            SType = StructureType.DependencyInfo,
            MemoryBarrierCount = 1,
            PMemoryBarriers = &barrier
        };

        _context.Vk.CmdPipelineBarrier2(_commandBuffer, in dep);
    }

    /// <summary>
    /// Inserts a pipeline barrier synchronizing compute shader writes to indirect command buffers with indirect draw execution.
    /// </summary>
    public void BarrierComputeToIndirectDraw()
    {
        MemoryBarrier2 barrier = new()
        {
            SType = StructureType.MemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ComputeShaderBit,
            SrcAccessMask = AccessFlags2.ShaderWriteBit,
            DstStageMask = PipelineStageFlags2.DrawIndirectBit,
            DstAccessMask = AccessFlags2.IndirectCommandReadBit
        };

        DependencyInfo dep = new()
        {
            SType = StructureType.DependencyInfo,
            MemoryBarrierCount = 1,
            PMemoryBarriers = &barrier
        };

        _context.Vk.CmdPipelineBarrier2(_commandBuffer, in dep);
    }

    /// <summary>
    /// Inserts a pipeline barrier synchronizing compute shader image or buffer writes with fragment or compute shader sampled reads.
    /// </summary>
    public void BarrierComputeToShaderRead()
    {
        MemoryBarrier2 barrier = new()
        {
            SType = StructureType.MemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ComputeShaderBit,
            SrcAccessMask = AccessFlags2.ShaderWriteBit,
            DstStageMask = PipelineStageFlags2.FragmentShaderBit | PipelineStageFlags2.ComputeShaderBit,
            DstAccessMask = AccessFlags2.ShaderReadBit
        };

        DependencyInfo dep = new()
        {
            SType = StructureType.DependencyInfo,
            MemoryBarrierCount = 1,
            PMemoryBarriers = &barrier
        };

        _context.Vk.CmdPipelineBarrier2(_commandBuffer, in dep);
    }
}
