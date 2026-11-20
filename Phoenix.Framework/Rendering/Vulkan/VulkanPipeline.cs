using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

public sealed unsafe class VulkanPipeline : IDisposable
{
    private readonly VulkanContext _context;
    private Pipeline _pipeline;
    private PipelineLayout _layout;
    private bool _disposed;

    public Pipeline Pipeline => _pipeline;
    public PipelineLayout Layout => _layout;
    public PipelineBindPoint BindPoint => PipelineBindPoint.Graphics;
    public uint DescriptorSetCount { get; private set; }
    public CullModeFlags DefaultCullMode { get; }
    public FrontFace DefaultFrontFace { get; }
    public bool DefaultDepthTestEnable { get; }
    public bool DefaultDepthWriteEnable { get; }
    public CompareOp DefaultDepthCompareOp { get; }
    public bool DefaultDepthBiasEnable { get; }
    public float DefaultDepthBiasConstant { get; }
    public float DefaultDepthBiasSlope { get; }
    public float DefaultDepthBiasClamp { get; }
    public ShaderStageFlags PushConstantStages { get; }

    /// <summary>
    /// Compiles shader modules and builds a Vulkan graphics pipeline with dynamic rendering and configurable pipeline state.
    /// </summary>
    public VulkanPipeline(VulkanContext context, VulkanPipelineDescription desc)
    {
        _context = context;
        DefaultCullMode = desc.CullMode;
        DefaultFrontFace = desc.FrontFace;
        DefaultDepthTestEnable = desc.DepthTestEnable;
        DefaultDepthWriteEnable = desc.DepthWriteEnable;
        DefaultDepthCompareOp = desc.DepthCompareOp;
        DefaultDepthBiasEnable = desc.DepthBiasEnable;
        DefaultDepthBiasConstant = desc.DepthBiasConstantFactor;
        DefaultDepthBiasSlope = desc.DepthBiasSlopeFactor;
        DefaultDepthBiasClamp = desc.DepthBiasClamp;
        PushConstantStages = desc.PushConstantStages;

        CreatePipelineLayout(desc);
        CreateGraphicsPipeline(desc);
    }

    /// <summary>
    /// Creates the pipeline layout configuring descriptor set layouts and push constant ranges.
    /// </summary>
    private void CreatePipelineLayout(VulkanPipelineDescription desc)
    {
        DescriptorSetCount = (uint)desc.DescriptorSetLayouts.Length;

        PushConstantRange pushRange = new()
        {
            StageFlags = desc.PushConstantStages,
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
        bool hasFragment = desc.FragmentShaderSpv != null && desc.FragmentShaderSpv.Length > 0;
        var vertModule = CreateShaderModule(desc.VertexShaderSpv);
        var fragModule = hasFragment ? CreateShaderModule(desc.FragmentShaderSpv!) : default;

        byte* mainName = (byte*)SilkMarshal.StringToPtr("main");

        PipelineShaderStageCreateInfo vertStage = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertModule,
            PName = mainName
        };

        PipelineShaderStageCreateInfo fragStage = default;
        if (hasFragment)
        {
            fragStage = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragModule,
                PName = mainName
            };
        }

        uint stageCount = hasFragment ? 2u : 1u;
        PipelineShaderStageCreateInfo* stages = stackalloc PipelineShaderStageCreateInfo[(int)stageCount];
        stages[0] = vertStage;
        if (hasFragment)
        {
            stages[1] = fragStage;
        }

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
                DepthBiasEnable = desc.DepthBiasEnable,
                DepthBiasConstantFactor = desc.DepthBiasConstantFactor,
                DepthBiasSlopeFactor = desc.DepthBiasSlopeFactor,
                DepthBiasClamp = desc.DepthBiasClamp
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

            Format[] colorFormats;
            if (desc.ColorFormats.Length > 0)
            {
                colorFormats = desc.ColorFormats;
            }
            else if (desc.ColorFormat != Format.Undefined)
            {
                colorFormats = [desc.ColorFormat];
            }
            else
            {
                colorFormats = [];
            }

            BlendMode effectiveBlend = desc.BlendMode ?? (desc.BlendEnable ? BlendMode.AlphaBlend : BlendMode.Opaque);
            PipelineColorBlendAttachmentState[] blendAttachments = new PipelineColorBlendAttachmentState[colorFormats.Length];
            for (int i = 0; i < colorFormats.Length; i++)
            {
                PipelineColorBlendAttachmentState attachment = new()
                {
                    ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit
                };

                switch (effectiveBlend)
                {
                    case BlendMode.Opaque:
                        attachment.BlendEnable = false;
                        break;
                    case BlendMode.AlphaBlend:
                        attachment.BlendEnable = true;
                        attachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
                        attachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
                        attachment.ColorBlendOp = BlendOp.Add;
                        attachment.SrcAlphaBlendFactor = BlendFactor.One;
                        attachment.DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha;
                        attachment.AlphaBlendOp = BlendOp.Add;
                        break;
                    case BlendMode.Additive:
                        attachment.BlendEnable = true;
                        attachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
                        attachment.DstColorBlendFactor = BlendFactor.One;
                        attachment.ColorBlendOp = BlendOp.Add;
                        attachment.SrcAlphaBlendFactor = BlendFactor.One;
                        attachment.DstAlphaBlendFactor = BlendFactor.One;
                        attachment.AlphaBlendOp = BlendOp.Add;
                        break;
                    case BlendMode.Premultiplied:
                        attachment.BlendEnable = true;
                        attachment.SrcColorBlendFactor = BlendFactor.One;
                        attachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
                        attachment.ColorBlendOp = BlendOp.Add;
                        attachment.SrcAlphaBlendFactor = BlendFactor.One;
                        attachment.DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha;
                        attachment.AlphaBlendOp = BlendOp.Add;
                        break;
                }

                blendAttachments[i] = attachment;
            }

            fixed (PipelineColorBlendAttachmentState* pBlendAttachments = blendAttachments)
            fixed (Format* pColorFormats = colorFormats)
            {
                PipelineColorBlendStateCreateInfo colorBlending = new()
                {
                    SType = StructureType.PipelineColorBlendStateCreateInfo,
                    LogicOpEnable = false,
                    AttachmentCount = (uint)colorFormats.Length,
                    PAttachments = colorFormats.Length > 0 ? pBlendAttachments : null
                };

                DynamicState* dynamicStates = stackalloc DynamicState[]
                {
                    DynamicState.Viewport,
                    DynamicState.Scissor,
                    DynamicState.CullMode,
                    DynamicState.FrontFace,
                    DynamicState.DepthTestEnable,
                    DynamicState.DepthWriteEnable,
                    DynamicState.DepthCompareOp,
                    DynamicState.DepthBias
                };

                PipelineDynamicStateCreateInfo dynamicStateInfo = new()
                {
                    SType = StructureType.PipelineDynamicStateCreateInfo,
                    DynamicStateCount = 8,
                    PDynamicStates = dynamicStates
                };

                PipelineRenderingCreateInfo renderingInfo = new()
                {
                    SType = StructureType.PipelineRenderingCreateInfo,
                    ColorAttachmentCount = (uint)colorFormats.Length,
                    PColorAttachmentFormats = colorFormats.Length > 0 ? pColorFormats : null,
                    DepthAttachmentFormat = desc.DepthFormat
                };

                GraphicsPipelineCreateInfo pipelineInfo = new()
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    PNext = &renderingInfo,
                    StageCount = stageCount,
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

                VulkanHelper.Check(_context.Vk.CreateGraphicsPipelines(_context.Device, _context.PipelineCache, 1, in pipelineInfo, null, out _pipeline),
                    "Failed to create Vulkan graphics pipeline.");

                SilkMarshal.Free((nint)mainName);
                _context.Vk.DestroyShaderModule(_context.Device, vertModule, null);
                if (hasFragment)
                {
                    _context.Vk.DestroyShaderModule(_context.Device, fragModule, null);
                }
            }
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
