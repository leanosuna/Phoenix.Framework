using Phoenix.Framework.Rendering.Shaders;
using Phoenix.Framework.Rendering.Vulkan;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Gizmos;

/// <summary>
/// Vulkan rendering pipeline for drawing debug wireframes and gizmo line lists with depth testing.
/// </summary>
public sealed unsafe class GizmoRenderer : IDisposable
{
    private readonly Graphics _graphics;
    private readonly VulkanPipeline _pipeline;
    private readonly VulkanBuffer?[] _lineBuffers = new VulkanBuffer[VulkanSwapchain.MaxFramesInFlight];
    private readonly ulong[] _vertexCapacities = new ulong[VulkanSwapchain.MaxFramesInFlight];
    private bool _disposed;

    private const string VertexShaderSource = @"#version 450 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec4 aColor;

layout(set = 0, binding = 0) uniform CommonData {
    mat4 uView;
    mat4 uProjection;
    vec3 uCamPos;
    float uTime;
    float uDeltaTime;
};

layout(location = 0) out vec4 vColor;

void main() {
    vColor = aColor;
    gl_Position = uProjection * uView * vec4(aPos, 1.0);
}
";

    private const string FragmentShaderSource = @"#version 450 core
layout(location = 0) in vec4 vColor;
layout(location = 0) out vec4 fColor;

void main() {
    fColor = vColor;
}
";

    public GizmoRenderer(Graphics graphics)
    {
        _graphics = graphics;

        byte[] vertSpv = ShaderCompiler.CompileGlsl(VertexShaderSource, ShaderKind.VertexShader, "gizmos.vert");
        byte[] fragSpv = ShaderCompiler.CompileGlsl(FragmentShaderSource, ShaderKind.FragmentShader, "gizmos.frag");

        VulkanPipelineDescription desc = new()
        {
            VertexShaderSpv = vertSpv,
            FragmentShaderSpv = fragSpv,
            VertexBindings = [VertexPositionColor.GetBindingDescription(0)],
            VertexAttributes = VertexPositionColor.GetAttributeDescriptions(0),
            Topology = PrimitiveTopology.LineList,
            CullMode = CullModeFlags.None,
            FrontFace = FrontFace.CounterClockwise,
            PolygonMode = PolygonMode.Fill,
            DepthTestEnable = true,
            DepthWriteEnable = false,
            DepthCompareOp = CompareOp.LessOrEqual,
            BlendEnable = true,
            DescriptorSetLayouts = [_graphics.CommonUBOLayout, _graphics.BindlessLayout],
            PushConstantsSize = 0,
            ColorFormat = _graphics.Swapchain.ImageFormat,
            DepthFormat = _graphics.Swapchain.DepthFormat
        };

        _pipeline = new VulkanPipeline(_graphics.Context, desc);
    }

    /// <summary>
    /// Binds vertex buffer and pipeline to submit line primitives to the active command buffer.
    /// </summary>
    public void Render(RenderContext ctx, ReadOnlySpan<VertexPositionColor> lines)
    {
        if (lines.IsEmpty)
            return;

        int frame = ctx.CurrentFrame;
        ulong requiredCount = (ulong)lines.Length;

        EnsureBuffer(frame, requiredCount);

        var buffer = _lineBuffers[frame]!;
        buffer.SetData(lines);

        ctx.BindPipeline(_pipeline);
        ctx.BindVertexBuffer(buffer);
        _graphics.Context.Vk.CmdDraw(ctx.CommandBuffer, (uint)lines.Length, 1, 0, 0);
    }

    private void EnsureBuffer(int frame, ulong requiredCount)
    {
        if (_lineBuffers[frame] == null || _vertexCapacities[frame] < requiredCount)
        {
            _lineBuffers[frame]?.Dispose();
            ulong newCount = Math.Max(requiredCount + 2048, _vertexCapacities[frame] * 2);
            ulong newSize = newCount * (ulong)sizeof(VertexPositionColor);

            _lineBuffers[frame] = new VulkanBuffer(_graphics.Context, newSize, BufferUsageFlags.VertexBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
            _vertexCapacities[frame] = newCount;
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
            _lineBuffers[i]?.Dispose();
            _lineBuffers[i] = null;
        }
    }
}
