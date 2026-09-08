using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

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
    public Format DepthFormat { get; init; }
}

public sealed unsafe class VulkanPipeline : IDisposable
{
    private readonly VulkanContext _context;
    private Pipeline _pipeline;
    private PipelineLayout _layout;
    private bool _disposed;

    public Pipeline Pipeline => _pipeline;
    public PipelineLayout Layout => _layout;
    public PipelineBindPoint BindPoint => PipelineBindPoint.Graphics;

    /// <summary>
    /// Compiles shader modules and builds a Vulkan graphics pipeline with dynamic rendering and configurable pipeline state.
    /// </summary>
    public VulkanPipeline(VulkanContext context, VulkanPipelineDescription desc)
    {
        _context = context;

        CreatePipelineLayout(desc);
        CreateGraphicsPipeline(desc);
    }

    /// <summary>
    /// Creates the pipeline layout configuring descriptor set layouts and push constant ranges.
    /// </summary>
    private void CreatePipelineLayout(VulkanPipelineDescription desc)
    {
        PushConstantRange pushRange = new()
        {
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            Offset = 0,
            Size = desc.PushConstantsSize
        };

        fixed (DescriptorSetLayout* pSetLayouts = desc.DescriptorSetLayouts)
        {
            PipelineLayoutCreateInfo layoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint)desc.DescriptorSetLayouts.Length,
                PSetLayouts = pSetLayouts,
                PushConstantRangeCount = desc.PushConstantsSize > 0 ? 1u : 0u,
                PPushConstantRanges = desc.PushConstantsSize > 0 ? &pushRange : null
            };

            VulkanHelper.Check(_context.Vk.CreatePipelineLayout(_context.Device, in layoutInfo, null, out _layout),
                "Failed to create Vulkan pipeline layout.");
        }
    }

    /// <summary>
    /// Compiles shader modules, configures dynamic states and attachments, and creates the VkPipeline handle.
    /// </summary>
    private void CreateGraphicsPipeline(VulkanPipelineDescription desc)
    {
        var vertModule = CreateShaderModule(desc.VertexShaderSpv);
        var fragModule = CreateShaderModule(desc.FragmentShaderSpv);

        byte* mainName = (byte*)SilkMarshal.StringToPtr("main");

        PipelineShaderStageCreateInfo vertStage = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertModule,
            PName = mainName
        };

        PipelineShaderStageCreateInfo fragStage = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragModule,
            PName = mainName
        };

        PipelineShaderStageCreateInfo* stages = stackalloc PipelineShaderStageCreateInfo[] { vertStage, fragStage };

        fixed (VertexInputBindingDescription* pBindings = desc.VertexBindings)
        fixed (VertexInputAttributeDescription* pAttributes = desc.VertexAttributes)
        {
            PipelineVertexInputStateCreateInfo vertexInputInfo = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = (uint)desc.VertexBindings.Length,
                PVertexBindingDescriptions = pBindings,
                VertexAttributeDescriptionCount = (uint)desc.VertexAttributes.Length,
                PVertexAttributeDescriptions = pAttributes
            };

            PipelineInputAssemblyStateCreateInfo inputAssembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = desc.Topology,
                PrimitiveRestartEnable = false
            };

            PipelineViewportStateCreateInfo viewportState = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                ScissorCount = 1
            };

            PipelineRasterizationStateCreateInfo rasterizer = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = desc.PolygonMode,
                LineWidth = 1.0f,
                CullMode = desc.CullMode,
                FrontFace = desc.FrontFace,
                DepthBiasEnable = false
            };

            PipelineMultisampleStateCreateInfo multisampling = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.Count1Bit
            };

            PipelineDepthStencilStateCreateInfo depthStencil = new()
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = desc.DepthTestEnable,
                DepthWriteEnable = desc.DepthWriteEnable,
                DepthCompareOp = desc.DepthCompareOp,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false
            };

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                BlendEnable = desc.BlendEnable,
                SrcColorBlendFactor = BlendFactor.SrcAlpha,
                DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOp = BlendOp.Add
            };

            PipelineColorBlendStateCreateInfo colorBlending = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment
            };

            DynamicState* dynamicStates = stackalloc DynamicState[]
            {
                DynamicState.Viewport,
                DynamicState.Scissor
            };

            PipelineDynamicStateCreateInfo dynamicStateInfo = new()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                PDynamicStates = dynamicStates
            };


            Format colorFormat = desc.ColorFormat;
            PipelineRenderingCreateInfo renderingInfo = new()
            {
                SType = StructureType.PipelineRenderingCreateInfo,
                ColorAttachmentCount = 1,
                PColorAttachmentFormats = &colorFormat,
                DepthAttachmentFormat = desc.DepthFormat
            };

            GraphicsPipelineCreateInfo pipelineInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                PNext = &renderingInfo,
                StageCount = 2,
                PStages = stages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                PDynamicState = &dynamicStateInfo,
                Layout = _layout,
                RenderPass = default,
                Subpass = 0
            };

            VulkanHelper.Check(_context.Vk.CreateGraphicsPipelines(_context.Device, default, 1, in pipelineInfo, null, out _pipeline),
                "Failed to create Vulkan graphics pipeline.");

            SilkMarshal.Free((nint)mainName);
            _context.Vk.DestroyShaderModule(_context.Device, vertModule, null);
            _context.Vk.DestroyShaderModule(_context.Device, fragModule, null);
        }
    }

    /// <summary>
    /// Creates a VkShaderModule handle from raw SPIR-V bytecode.
    /// </summary>
    private ShaderModule CreateShaderModule(byte[] bytecode)
    {
        fixed (byte* pBytecode = bytecode)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)bytecode.Length,
                PCode = (uint*)pBytecode
            };

            VulkanHelper.Check(_context.Vk.CreateShaderModule(_context.Device, in createInfo, null, out var module),
                "Failed to create shader module.");

            return module;
        }
    }

    /// <summary>
    /// Destroys the graphics pipeline and pipeline layout handles.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_pipeline.Handle != 0)
        {
            _context.Vk.DestroyPipeline(_context.Device, _pipeline, null);
            _pipeline = default;
        }

        if (_layout.Handle != 0)
        {
            _context.Vk.DestroyPipelineLayout(_context.Device, _layout, null);
            _layout = default;
        }

        _disposed = true;
    }
}
