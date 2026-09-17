using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Configuration description for creating a graphics pipeline state object.
/// </summary>
public sealed record VulkanPipelineDescription
{
    public byte[] VertexShaderSpv { get; init; } = [];
    public byte[] FragmentShaderSpv { get; init; } = [];
    public VertexInputBindingDescription[] VertexBindings { get; init; } = [];
    public VertexInputAttributeDescription[] VertexAttributes { get; init; } = [];
    public PrimitiveTopology Topology { get; init; } = PrimitiveTopology.TriangleList;
    public CullModeFlags CullMode { get; init; } = CullModeFlags.BackBit;
    public FrontFace FrontFace { get; init; } = FrontFace.CounterClockwise;
    public PolygonMode PolygonMode { get; init; } = PolygonMode.Fill;
    public bool DepthTestEnable { get; init; } = true;
    public bool DepthWriteEnable { get; init; } = true;
    public CompareOp DepthCompareOp { get; init; } = CompareOp.LessOrEqual;
    public bool BlendEnable { get; init; } = false;
    public DescriptorSetLayout[] DescriptorSetLayouts { get; init; } = [];
    public uint PushConstantsSize { get; init; } = 80;
    public Format ColorFormat { get; init; }
    public Format[] ColorFormats { get; init; } = [];
    public Format DepthFormat { get; init; }
}
