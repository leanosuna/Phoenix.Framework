using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Vulkan;
using System.Numerics;

namespace Phoenix.Framework.Rendering;

public sealed unsafe class RenderContext
{
    private readonly VulkanContext _context;
    private readonly VulkanSwapchain _swapchain;
    private readonly CommonUBOManager _commonUbo;
    private CommandBuffer _commandBuffer;
    private uint _imageIndex;
    private int _currentFrame;
    private bool _passActive;

    public bool HasRenderedPass { get; private set; }
    public CommandBuffer CommandBuffer => _commandBuffer;
    public uint ImageIndex => _imageIndex;
    public int CurrentFrame => _currentFrame;

    /// <summary>
    /// Initializes the rendering context bound to the active Vulkan context, swapchain, and uniform manager.
    /// </summary>
    internal RenderContext(VulkanContext context, VulkanSwapchain swapchain, CommonUBOManager commonUbo)
    {
        _context = context;
        _swapchain = swapchain;
        _commonUbo = commonUbo;
    }

    /// <summary>
    /// Prepares the rendering context with the active command buffer and frame indices for the current frame.
    /// </summary>
    internal void Prepare(CommandBuffer commandBuffer, uint imageIndex, int currentFrame)
    {
        _commandBuffer = commandBuffer;
        _imageIndex = imageIndex;
        _currentFrame = currentFrame;
        _passActive = false;
        HasRenderedPass = false;
    }

    /// <summary>
    /// Transitions swapchain color and depth attachments and begins a scoped dynamic rendering pass.
    /// </summary>
    public ScopedPass BeginPass(Vector4 clearColor, float clearDepth = 1.0f)
    {
        if (_passActive)
            throw new InvalidOperationException("A render pass is already active on this context.");

        var vk = _context.Vk;
        var colorImage = _swapchain.GetImage(_imageIndex);
        var depthImage = _swapchain.DepthImage;

        ImageMemoryBarrier2 colorBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            SrcAccessMask = AccessFlags2.None,
            DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags2.ColorAttachmentWriteBit,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.ColorAttachmentOptimal,
            Image = colorImage,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        ImageMemoryBarrier2 depthBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.EarlyFragmentTestsBit | PipelineStageFlags2.LateFragmentTestsBit,
            SrcAccessMask = AccessFlags2.None,
            DstStageMask = PipelineStageFlags2.EarlyFragmentTestsBit | PipelineStageFlags2.LateFragmentTestsBit,
            DstAccessMask = AccessFlags2.DepthStencilAttachmentWriteBit | AccessFlags2.DepthStencilAttachmentReadBit,
            OldLayout = ImageLayout.Undefined,
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

        ClearColorValue colorValue = new(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        RenderingAttachmentInfo colorAttachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = _swapchain.GetImageView(_imageIndex),
            ImageLayout = ImageLayout.ColorAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new ClearValue { Color = colorValue }
        };

        ClearDepthStencilValue depthValue = new(clearDepth, 0);
        RenderingAttachmentInfo depthAttachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = _swapchain.DepthImageView,
            ImageLayout = ImageLayout.DepthAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
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

        _passActive = true;
        HasRenderedPass = true;

        return new ScopedPass(this);
    }

    /// <summary>
    /// Ends the dynamic rendering pass and transitions the color attachment for presentation.
    /// </summary>
    public void EndPass()
    {
        if (!_passActive)
            return;

        var vk = _context.Vk;
        vk.CmdEndRendering(_commandBuffer);

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

        _passActive = false;
    }

    /// <summary>
    /// Executes a minimal clear pass transitioning the swapchain image to present layout when no user pass was recorded.
    /// </summary>
    internal void FallbackClearPass(Vector4 clearColor)
    {
        using (BeginPass(clearColor))
        {
        }
    }

    /// <summary>
    /// Binds a graphics pipeline and automatically binds the CommonUBO descriptor set to Set 0.
    /// </summary>
    public void BindPipeline(VulkanPipeline pipeline)
    {
        _context.Vk.CmdBindPipeline(_commandBuffer, pipeline.BindPoint, pipeline.Pipeline);

        var uboSet = _commonUbo.GetDescriptorSet(_currentFrame);
        _context.Vk.CmdBindDescriptorSets(_commandBuffer, pipeline.BindPoint, pipeline.Layout, 0, 1, in uboSet, 0, null);
    }

    /// <summary>
    /// Uploads push constants data directly to the command buffer.
    /// </summary>
    public void PushConstants<T>(VulkanPipeline pipeline, in T data) where T : unmanaged
    {
        fixed (T* pData = &data)
        {
            _context.Vk.CmdPushConstants(
                _commandBuffer,
                pipeline.Layout,
                ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                0,
                (uint)sizeof(T),
                pData);
        }
    }

    /// <summary>
    /// Binds a vertex buffer to binding 0 at the specified byte offset.
    /// </summary>
    public void BindVertexBuffer(VulkanBuffer buffer, ulong offset = 0)
    {
        var buf = buffer.Buffer;
        _context.Vk.CmdBindVertexBuffers(_commandBuffer, 0, 1, in buf, in offset);
    }

    /// <summary>
    /// Binds an index buffer with the specified index format at the given byte offset.
    /// </summary>
    public void BindIndexBuffer(VulkanBuffer buffer, ulong offset = 0, IndexType indexType = IndexType.Uint32)
    {
        _context.Vk.CmdBindIndexBuffer(_commandBuffer, buffer.Buffer, offset, indexType);
    }


    /// <summary>
    /// Issues an indexed draw command for the currently bound vertex and index buffers.
    /// </summary>
    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
    {
        _context.Vk.CmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    /// <summary>
    /// Issues a non-indexed primitive draw command for the currently bound vertex buffer.
    /// </summary>
    public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        _context.Vk.CmdDraw(_commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
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
}

public readonly ref struct ScopedPass
{
    private readonly RenderContext _context;

    /// <summary>
    /// Initializes a scoped rendering pass wrapper.
    /// </summary>
    internal ScopedPass(RenderContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ends the dynamic rendering pass upon scope exit.
    /// </summary>
    public void Dispose()
    {
        _context.EndPass();
    }
}
