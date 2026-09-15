using ImGuiNET;
using Phoenix.Framework.Rendering.Shaders;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.GUI;

/// <summary>
/// Vulkan rendering backend for ImGui utilizing dynamic rendering and Set 1 bindless descriptors.
/// </summary>
public sealed unsafe class ImGuiRenderer : IDisposable
{
    private readonly Graphics _graphics;
    private readonly VulkanPipeline _pipeline;
    private readonly VulkanBuffer?[] _vertexBuffers = new VulkanBuffer[VulkanSwapchain.MaxFramesInFlight];
    private readonly VulkanBuffer?[] _indexBuffers = new VulkanBuffer[VulkanSwapchain.MaxFramesInFlight];
    private readonly ulong[] _vertexCounts = new ulong[VulkanSwapchain.MaxFramesInFlight];
    private readonly ulong[] _indexCounts = new ulong[VulkanSwapchain.MaxFramesInFlight];
    private bool _disposed;

    private const string VertexShaderSource = @"#version 450 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUV;
layout(location = 2) in vec4 aColor;

layout(push_constant) uniform PushConstants {
    vec2 uScale;
    vec2 uTranslate;
    uint uTextureId;
} pc;

layout(location = 0) out vec2 vUV;
layout(location = 1) out vec4 vColor;

void main() {
    vUV = aUV;
    vColor = aColor;
    gl_Position = vec4(aPos * pc.uScale + pc.uTranslate, 0.0, 1.0);
}
";

    private const string FragmentShaderSource = @"#version 450 core
#extension GL_EXT_nonuniform_qualifier : enable

layout(location = 0) in vec2 vUV;
layout(location = 1) in vec4 vColor;

layout(push_constant) uniform PushConstants {
    vec2 uScale;
    vec2 uTranslate;
    uint uTextureId;
} pc;

layout(set = 1, binding = 0) uniform sampler2D uTextures[];

layout(location = 0) out vec4 fColor;

void main() {
    fColor = vColor * texture(uTextures[nonuniformEXT(pc.uTextureId)], vUV);
}
";

    /// <summary>
    /// Initializes the Vulkan ImGui renderer pipeline, shader modules, and descriptor layouts.
    /// </summary>
    public ImGuiRenderer(Graphics graphics)
    {
        _graphics = graphics;

        byte[] vertSpv = ShaderCompiler.CompileGlsl(VertexShaderSource, ShaderKind.VertexShader, "imgui.vert");
        byte[] fragSpv = ShaderCompiler.CompileGlsl(FragmentShaderSource, ShaderKind.FragmentShader, "imgui.frag");

        VertexInputBindingDescription[] bindings =
        [
            new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = (uint)sizeof(ImDrawVert),
                InputRate = VertexInputRate.Vertex
            }
        ];

        VertexInputAttributeDescription[] attributes =
        [
            new VertexInputAttributeDescription
            {
                Location = 0,
                Binding = 0,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.pos))
            },
            new VertexInputAttributeDescription
            {
                Location = 1,
                Binding = 0,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.uv))
            },
            new VertexInputAttributeDescription
            {
                Location = 2,
                Binding = 0,
                Format = Format.R8G8B8A8Unorm,
                Offset = (uint)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.col))
            }
        ];

        VulkanPipelineDescription desc = new()
        {
            VertexShaderSpv = vertSpv,
            FragmentShaderSpv = fragSpv,
            VertexBindings = bindings,
            VertexAttributes = attributes,
            Topology = PrimitiveTopology.TriangleList,
            CullMode = CullModeFlags.None,
            FrontFace = FrontFace.CounterClockwise,
            PolygonMode = PolygonMode.Fill,
            DepthTestEnable = false,
            DepthWriteEnable = false,
            BlendEnable = true,
            DescriptorSetLayouts = [_graphics.CommonUBOLayout, _graphics.BindlessLayout],
            PushConstantsSize = (uint)sizeof(ImGuiPushConstants),
            ColorFormat = _graphics.Swapchain.ImageFormat,
            DepthFormat = Format.Undefined
        };

        _pipeline = new VulkanPipeline(_graphics.Context, desc);
    }

    /// <summary>
    /// Renders ImGui draw data into the active dynamic rendering UI pass.
    /// </summary>
    public void Render(RenderContext ctx, ImDrawDataPtr drawData, Extent2D swapchainExtent)
    {
        if (drawData.TotalVtxCount <= 0 || drawData.TotalIdxCount <= 0)
            return;

        int frame = ctx.CurrentFrame;
        var vk = _graphics.Context.Vk;
        var cmd = ctx.CommandBuffer;

        EnsureBuffers(frame, (ulong)drawData.TotalVtxCount, (ulong)drawData.TotalIdxCount);

        var vtxBuffer = _vertexBuffers[frame]!;
        var idxBuffer = _indexBuffers[frame]!;

        byte* pVtxDst = (byte*)vtxBuffer.MappedData;
        byte* pIdxDst = (byte*)idxBuffer.MappedData;
        ulong vtxOffsetBytes = 0;
        ulong idxOffsetBytes = 0;

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdLists[i];
            ulong vtxBytes = (ulong)(cmdList.VtxBuffer.Size * sizeof(ImDrawVert));
            ulong idxBytes = (ulong)(cmdList.IdxBuffer.Size * sizeof(ushort));

            System.Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, pVtxDst + vtxOffsetBytes, vtxBuffer.Size - vtxOffsetBytes, vtxBytes);
            System.Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, pIdxDst + idxOffsetBytes, idxBuffer.Size - idxOffsetBytes, idxBytes);

            vtxOffsetBytes += vtxBytes;
            idxOffsetBytes += idxBytes;
        }

        ctx.BindPipeline(_pipeline);

        Silk.NET.Vulkan.Buffer rawVtxBuffer = vtxBuffer.Buffer;
        ulong rawOffset = 0;
        vk.CmdBindVertexBuffers(cmd, 0, 1, in rawVtxBuffer, in rawOffset);
        vk.CmdBindIndexBuffer(cmd, idxBuffer.Buffer, 0, IndexType.Uint16);

        Viewport viewport = new(0, 0, drawData.DisplaySize.X, drawData.DisplaySize.Y, 0.0f, 1.0f);
        vk.CmdSetViewport(cmd, 0, 1, in viewport);

        Vector2 clipOff = drawData.DisplayPos;
        Vector2 clipScale = drawData.FramebufferScale;

        float scaleX = 2.0f / drawData.DisplaySize.X;
        float scaleY = 2.0f / drawData.DisplaySize.Y;

        uint globalVtxOffset = 0;
        uint globalIdxOffset = 0;

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdLists[i];
            for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
            {
                var pcmd = cmdList.CmdBuffer[cmdIndex];

                Vector4 clipRect = pcmd.ClipRect;
                int clipMinX = Math.Max(0, (int)((clipRect.X - clipOff.X) * clipScale.X));
                int clipMinY = Math.Max(0, (int)((clipRect.Y - clipOff.Y) * clipScale.Y));
                int clipMaxX = Math.Min((int)swapchainExtent.Width, (int)((clipRect.Z - clipOff.X) * clipScale.X));
                int clipMaxY = Math.Min((int)swapchainExtent.Height, (int)((clipRect.W - clipOff.Y) * clipScale.Y));

                if (clipMaxX <= clipMinX || clipMaxY <= clipMinY)
                    continue;

                Rect2D scissor = new(
                    new Offset2D(clipMinX, clipMinY),
                    new Extent2D((uint)(clipMaxX - clipMinX), (uint)(clipMaxY - clipMinY))
                );
                vk.CmdSetScissor(cmd, 0, 1, in scissor);

                ImGuiPushConstants pc = new()
                {
                    Scale = new Vector2(scaleX, scaleY),
                    Translate = new Vector2(-1.0f - clipOff.X * scaleX, -1.0f - clipOff.Y * scaleY),
                    TextureId = (uint)pcmd.TextureId
                };

                vk.CmdPushConstants(cmd, _pipeline.Layout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, (uint)sizeof(ImGuiPushConstants), &pc);
                vk.CmdDrawIndexed(cmd, pcmd.ElemCount, 1, pcmd.IdxOffset + globalIdxOffset, (int)(pcmd.VtxOffset + globalVtxOffset), 0);
            }

            globalVtxOffset += (uint)cmdList.VtxBuffer.Size;
            globalIdxOffset += (uint)cmdList.IdxBuffer.Size;
        }
    }

    private void EnsureBuffers(int frame, ulong neededVtx, ulong neededIdx)
    {
        if (_vertexBuffers[frame] == null || _vertexCounts[frame] < neededVtx)
        {
            _vertexBuffers[frame]?.Dispose();
            ulong newCount = Math.Max(neededVtx + 5000, _vertexCounts[frame] * 2);
            ulong newSize = newCount * (ulong)sizeof(ImDrawVert);
            _vertexBuffers[frame] = new VulkanBuffer(_graphics.Context, newSize, BufferUsageFlags.VertexBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
            _vertexCounts[frame] = newCount;
        }

        if (_indexBuffers[frame] == null || _indexCounts[frame] < neededIdx)
        {
            _indexBuffers[frame]?.Dispose();
            ulong newCount = Math.Max(neededIdx + 10000, _indexCounts[frame] * 2);
            ulong newSize = newCount * sizeof(ushort);
            _indexBuffers[frame] = new VulkanBuffer(_graphics.Context, newSize, BufferUsageFlags.IndexBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
            _indexCounts[frame] = newCount;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _pipeline.Dispose();

        for (int i = 0; i < VulkanSwapchain.MaxFramesInFlight; i++)
        {
            _vertexBuffers[i]?.Dispose();
            _vertexBuffers[i] = null;
            _indexBuffers[i]?.Dispose();
            _indexBuffers[i] = null;
        }
    }
}
