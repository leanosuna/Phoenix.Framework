using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Indirect;

/// <summary>
/// Submits multi-draw indirect commands from an IndirectDrawBatch to the Vulkan command buffer.
/// Binds pipeline, Set 2 submesh metadata descriptor set, packed geometry buffers,
/// and dispatches vkCmdDrawIndexedIndirect.
/// </summary>
public sealed class IndirectMeshRenderer
{
    private readonly Graphics _graphics;

    public IndirectMeshRenderer(Graphics graphics)
    {
        _graphics = graphics;
    }

    /// <summary>
    /// Records an indirect draw for all submeshes within the batch.
    /// </summary>
    public void Render(RenderContext rc, VulkanPipeline pipeline, IndirectDrawBatch batch)
    {
        Render(rc, pipeline, batch, 0, batch.DrawCount);
    }

    /// <summary>
    /// Records an indirect draw for a subrange of submeshes within the batch.
    /// </summary>
    public void Render(RenderContext rc, VulkanPipeline pipeline, IndirectDrawBatch batch, uint firstDraw, uint drawCount)
    {
        if (batch.DrawCount == 0 || drawCount == 0)
            return;

        if (firstDraw + drawCount > batch.DrawCount)
            throw new ArgumentOutOfRangeException(nameof(drawCount), "Draw range exceeds total batch draw count.");

        uint stride = (uint)Marshal.SizeOf<DrawIndexedIndirectCommand>();
        ulong offset = (ulong)firstDraw * stride;

        rc.BindPipeline(pipeline);
        rc.BindDescriptorSet(pipeline, 2, batch.MetadataDescriptorSet);
        rc.BindVertexBuffer(batch.PackedVertexBuffer);
        rc.BindIndexBuffer(batch.PackedIndexBuffer);
        rc.DrawIndexedIndirect(batch.IndirectCommandBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Records an indirect draw for all submeshes within the batch with push constants data.
    /// </summary>
    public void Render<T>(RenderContext rc, VulkanPipeline pipeline, IndirectDrawBatch batch, in T pushConstants) where T : unmanaged
    {
        Render(rc, pipeline, batch, 0, batch.DrawCount, in pushConstants);
    }

    /// <summary>
    /// Records an indirect draw for a subrange of submeshes within the batch with push constants data.
    /// </summary>
    public void Render<T>(RenderContext rc, VulkanPipeline pipeline, IndirectDrawBatch batch, uint firstDraw, uint drawCount, in T pushConstants) where T : unmanaged
    {
        if (batch.DrawCount == 0 || drawCount == 0)
            return;

        if (firstDraw + drawCount > batch.DrawCount)
            throw new ArgumentOutOfRangeException(nameof(drawCount), "Draw range exceeds total batch draw count.");

        uint stride = (uint)Marshal.SizeOf<DrawIndexedIndirectCommand>();
        ulong offset = (ulong)firstDraw * stride;

        rc.BindPipeline(pipeline);
        rc.PushConstants(pipeline, in pushConstants);
        rc.BindDescriptorSet(pipeline, 2, batch.MetadataDescriptorSet);
        rc.BindVertexBuffer(batch.PackedVertexBuffer);
        rc.BindIndexBuffer(batch.PackedIndexBuffer);
        rc.DrawIndexedIndirect(batch.IndirectCommandBuffer, offset, drawCount, stride);
    }
}
