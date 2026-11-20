using Silk.NET.Vulkan;
using System.Reflection;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Fluent builder for configuring and instantiating Vulkan compute pipeline state objects.
/// </summary>
public sealed class ComputePipelineBuilder
{
    private readonly Graphics _graphics;
    private byte[] _compSpv = [];
    private readonly List<DescriptorSetLayout> _layouts = new();
    private uint _pushConstantsSize = 80;

    public ComputePipelineBuilder(Graphics graphics)
    {
        _graphics = graphics;
    }

    public ComputePipelineBuilder WithShader(byte[] compSpv)
    {
        _compSpv = compSpv;
        return this;
    }

    public ComputePipelineBuilder WithShaderFromResource(string compResource, Assembly? assembly = null)
    {
        _compSpv = LoadResourceBytes(compResource, assembly);
        return this;
    }

    public ComputePipelineBuilder WithDescriptorLayouts(params DescriptorSetLayout[] layouts)
    {
        _layouts.AddRange(layouts);
        return this;
    }

    public ComputePipelineBuilder WithDescriptorSetLayout(DescriptorSetLayout layout)
    {
        _layouts.Add(layout);
        return this;
    }

    public unsafe ComputePipelineBuilder WithPushConstants<TPush>() where TPush : unmanaged
    {
        _pushConstantsSize = (uint)sizeof(TPush);
        return this;
    }

    public ComputePipelineBuilder WithPushConstants(uint sizeInBytes)
    {
        _pushConstantsSize = sizeInBytes;
        return this;
    }

    public VulkanComputePipeline Build()
    {
        if (_compSpv.Length == 0)
            throw new InvalidOperationException("Compute pipeline must have a compute shader configured.");

        return new VulkanComputePipeline(_graphics.Context, _compSpv, _layouts.ToArray(), _pushConstantsSize);
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
