using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Encapsulates a Vulkan compute pipeline, compute shader module, and pipeline layout.
/// </summary>
public sealed unsafe class VulkanComputePipeline : IDisposable
{
    private readonly VulkanContext _context;
    private Pipeline _pipeline;
    private PipelineLayout _layout;
    private bool _disposed;

    public Pipeline Pipeline => _pipeline;
    public PipelineLayout Layout => _layout;
    public PipelineBindPoint BindPoint => PipelineBindPoint.Compute;

    /// <summary>
    /// Compiles a compute shader module and builds a Vulkan compute pipeline.
    /// </summary>
    public VulkanComputePipeline(VulkanContext context, byte[] computeShaderSpv,
        DescriptorSetLayout[]? descriptorSetLayouts = null, uint pushConstantsSize = 80)
    {
        _context = context;

        var layouts = descriptorSetLayouts ?? [];
        CreatePipelineLayout(layouts, pushConstantsSize);
        CreateComputePipeline(computeShaderSpv);
    }

    private void CreatePipelineLayout(DescriptorSetLayout[] descriptorSetLayouts, uint pushConstantsSize)
    {
        PushConstantRange pushRange = new()
        {
            StageFlags = ShaderStageFlags.ComputeBit,
            Offset = 0,
            Size = pushConstantsSize
        };

        fixed (DescriptorSetLayout* pSetLayouts = descriptorSetLayouts)
        {
            PipelineLayoutCreateInfo layoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint)descriptorSetLayouts.Length,
                PSetLayouts = pSetLayouts,
                PushConstantRangeCount = pushConstantsSize > 0 ? 1u : 0u,
                PPushConstantRanges = pushConstantsSize > 0 ? &pushRange : null
            };

            VulkanHelper.Check(_context.Vk.CreatePipelineLayout(_context.Device, in layoutInfo, null, out _layout),
                "Failed to create compute pipeline layout.");
        }
    }

    private void CreateComputePipeline(byte[] computeShaderSpv)
    {
        ShaderModule shaderModule = CreateShaderModule(computeShaderSpv);
        byte* mainName = (byte*)SilkMarshal.StringToPtr("main");

        try
        {
            PipelineShaderStageCreateInfo stageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.ComputeBit,
                Module = shaderModule,
                PName = mainName
            };

            ComputePipelineCreateInfo pipelineInfo = new()
            {
                SType = StructureType.ComputePipelineCreateInfo,
                Stage = stageInfo,
                Layout = _layout
            };

            VulkanHelper.Check(_context.Vk.CreateComputePipelines(_context.Device, default, 1, in pipelineInfo, null, out _pipeline),
                "Failed to create compute pipeline.");
        }
        finally
        {
            SilkMarshal.Free((nint)mainName);
            _context.Vk.DestroyShaderModule(_context.Device, shaderModule, null);
        }
    }

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
                "Failed to create compute shader module.");
            return module;
        }
    }

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
