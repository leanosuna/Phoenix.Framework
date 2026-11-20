using Phoenix.Framework.Rendering.Geometry.Model;
using Phoenix.Framework.Rendering.Geometry.Vertices;
using Silk.NET.Vulkan;
using System.Linq;
using System.Reflection;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Fluent builder for configuring and instantiating Vulkan graphics pipeline state objects.
/// </summary>
public sealed class PipelineBuilder
{
    private readonly Graphics _graphics;
    private byte[] _vertSpv = [];
    private byte[]? _fragSpv;
    private VertexInputBindingDescription[] _vertexBindings = [];
    private VertexInputAttributeDescription[] _vertexAttributes = [];
    private PrimitiveTopology _topology = PrimitiveTopology.TriangleList;
    private CullModeFlags _cullMode = CullModeFlags.BackBit;
    private FrontFace _frontFace = FrontFace.CounterClockwise;
    private PolygonMode _polygonMode = PolygonMode.Fill;
    private bool _depthTest = true;
    private bool _depthWrite = true;
    private CompareOp _depthCompareOp = CompareOp.Less;
    private bool _depthBiasEnable;
    private float _depthBiasConstant;
    private float _depthBiasSlope;
    private float _depthBiasClamp;
    private BlendMode _blendMode = BlendMode.Opaque;
    private uint _pushConstantsSize = 80;
    private ShaderStageFlags _pushConstantStages = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit;
    private Format[] _colorFormats = [];
    private Format _colorFormat = Format.Undefined;
    private Format _depthFormat = Format.Undefined;
    private readonly List<DescriptorSetLayout> _userLayouts = new();

    public PipelineBuilder(Graphics graphics)
    {
        _graphics = graphics;
    }

    public PipelineBuilder WithShaders(byte[] vertSpv, byte[]? fragSpv = null)
    {
        _vertSpv = vertSpv;
        _fragSpv = fragSpv;
        return this;
    }

    public PipelineBuilder WithShadersFromResources(string vertResource, string? fragResource = null, Assembly? assembly = null)
    {
        _vertSpv = LoadResourceBytes(vertResource, assembly);
        _fragSpv = fragResource != null ? LoadResourceBytes(fragResource, assembly) : null;
        return this;
    }

    public PipelineBuilder WithVertexShader(byte[] vertSpv)
    {
        _vertSpv = vertSpv;
        return this;
    }

    public PipelineBuilder WithVertexShaderFromResource(string vertResource, Assembly? assembly = null)
    {
        _vertSpv = LoadResourceBytes(vertResource, assembly);
        return this;
    }

    public PipelineBuilder WithVertexLayout<TVertex>(uint binding = 0) where TVertex : unmanaged, IVertexLayout
    {
        _vertexBindings = [TVertex.GetBindingDescription(binding)];
        _vertexAttributes = TVertex.GetAttributeDescriptions(binding);
        return this;
    }

    public PipelineBuilder WithVertexLayout(VertexInputBindingDescription[] bindings, VertexInputAttributeDescription[] attributes)
    {
        _vertexBindings = bindings;
        _vertexAttributes = attributes;
        return this;
    }

    public PipelineBuilder WithModelVertex(uint binding = 0)
    {
        return WithVertexLayout<ModelVertex>(binding);
    }

    public PipelineBuilder WithNoVertexInput()
    {
        _vertexBindings = [];
        _vertexAttributes = [];
        return this;
    }

    public PipelineBuilder WithTopology(PrimitiveTopology topology)
    {
        _topology = topology;
        return this;
    }

    public PipelineBuilder WithCullMode(CullModeFlags cullMode, FrontFace frontFace = FrontFace.CounterClockwise)
    {
        _cullMode = cullMode;
        _frontFace = frontFace;
        return this;
    }

    public PipelineBuilder WithPolygonMode(PolygonMode mode)
    {
        _polygonMode = mode;
        return this;
    }

    public PipelineBuilder WithDepth(bool test = true, bool write = true, CompareOp compareOp = CompareOp.Less)
    {
        _depthTest = test;
        _depthWrite = write;
        _depthCompareOp = compareOp;
        return this;
    }

    public PipelineBuilder WithDepthBias(float constantFactor, float slopeFactor, float clamp = 0f)
    {
        _depthBiasEnable = true;
        _depthBiasConstant = constantFactor;
        _depthBiasSlope = slopeFactor;
        _depthBiasClamp = clamp;
        return this;
    }

    public PipelineBuilder WithBlend(BlendMode mode)
    {
        _blendMode = mode;
        return this;
    }

    public unsafe PipelineBuilder WithPushConstants<TPush>(ShaderStageFlags stages = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit) where TPush : unmanaged
    {
        _pushConstantsSize = (uint)sizeof(TPush);
        _pushConstantStages = stages;
        return this;
    }

    public PipelineBuilder WithPushConstants(uint sizeInBytes, ShaderStageFlags stages = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit)
    {
        _pushConstantsSize = sizeInBytes;
        _pushConstantStages = stages;
        return this;
    }

    public PipelineBuilder WithTarget(VulkanRenderTarget target)
    {
        _colorFormats = target.ColorTextures.Select(t => t.Format).ToArray();
        _colorFormat = _colorFormats.Length > 0 ? _colorFormats[0] : target.ColorFormat;
        _depthFormat = target.DepthFormat;
        return this;
    }

    public PipelineBuilder ForTarget(VulkanRenderTarget target) => WithTarget(target);

    public PipelineBuilder ForSwapchain(bool withDepth = true)
    {
        _colorFormats = [_graphics.Swapchain.ImageFormat];
        _colorFormat = _graphics.Swapchain.ImageFormat;
        _depthFormat = withDepth ? _graphics.Swapchain.DepthFormat : Format.Undefined;
        return this;
    }

    public PipelineBuilder WithColorFormats(params Format[] formats)
    {
        _colorFormats = formats;
        _colorFormat = formats.Length > 0 ? formats[0] : Format.Undefined;
        return this;
    }

    public PipelineBuilder WithDepthOnly(Format? depthFormat = null)
    {
        _colorFormats = Array.Empty<Format>();
        _colorFormat = Format.Undefined;
        _depthFormat = depthFormat ?? _graphics.Swapchain.DepthFormat;
        return this;
    }

    public PipelineBuilder ForDepthOnly(Format? depthFormat = null) => WithDepthOnly(depthFormat);

    public PipelineBuilder WithBoneLayout()
    {
        return this;
    }

    public PipelineBuilder WithUserDescriptorLayouts(params DescriptorSetLayout[] layouts)
    {
        _userLayouts.AddRange(layouts);
        return this;
    }

    public VulkanPipeline Build()
    {
        if (_vertSpv.Length == 0)
            throw new InvalidOperationException("Pipeline must have a vertex shader configured.");

        Format colorFormat = _colorFormat;
        Format[] colorFormats = _colorFormats;
        Format depthFormat = _depthFormat;

        if (colorFormats.Length == 0 && colorFormat == Format.Undefined && depthFormat == Format.Undefined)
        {
            colorFormat = _graphics.Swapchain.ImageFormat;
            depthFormat = _graphics.Swapchain.DepthFormat;
        }
        else if (depthFormat == Format.Undefined && (_depthTest || _depthWrite))
        {
            depthFormat = _graphics.Swapchain.DepthFormat;
        }

        List<DescriptorSetLayout> layouts =
        [
            _graphics.FrameUniformsLayout,
            _graphics.BindlessLayout,
            _graphics.IndirectMetadataLayout,
            _graphics.BoneLayout
        ];

        if (_userLayouts.Count > 0)
        {
            layouts.AddRange(_userLayouts);
        }

        VulkanPipelineDescription desc = new()
        {
            VertexShaderSpv = _vertSpv,
            FragmentShaderSpv = _fragSpv ?? Array.Empty<byte>(),
            VertexBindings = _vertexBindings,
            VertexAttributes = _vertexAttributes,
            Topology = _topology,
            CullMode = _cullMode,
            FrontFace = _frontFace,
            PolygonMode = _polygonMode,
            DepthTestEnable = _depthTest,
            DepthWriteEnable = _depthWrite,
            DepthCompareOp = _depthCompareOp,
            BlendMode = _blendMode,
            BlendEnable = _blendMode != BlendMode.Opaque,
            DepthBiasEnable = _depthBiasEnable,
            DepthBiasConstantFactor = _depthBiasConstant,
            DepthBiasSlopeFactor = _depthBiasSlope,
            DepthBiasClamp = _depthBiasClamp,
            PushConstantsSize = _pushConstantsSize,
            PushConstantStages = _pushConstantStages,
            ColorFormat = colorFormat,
            ColorFormats = colorFormats,
            DepthFormat = depthFormat,
            DescriptorSetLayouts = layouts.ToArray()
        };

        return new VulkanPipeline(_graphics.Context, desc);
    }

    private static byte[] LoadResourceBytes(string resourceName, Assembly? assembly)
    {
        if (assembly == null)
            return EmbeddedHelper.ReadBytes(resourceName);

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found in assembly '{assembly.FullName}'.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
