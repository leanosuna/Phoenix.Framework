using Silk.NET.Vulkan;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Caches and manages unique Vulkan sampler handles based on structural sampler descriptions.
/// </summary>
public sealed unsafe class SamplerManager : IDisposable
{
    private readonly VulkanContext _context;
    private readonly Dictionary<SamplerDescription, Sampler> _cache = new();
    private readonly object _lock = new();
    private bool _disposed;

    public SamplerManager(VulkanContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtains an existing sampler matching the description or creates and caches a new one.
    /// </summary>
    public Sampler GetOrCreateSampler(in SamplerDescription desc)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(desc, out var existing))
                return existing;

            SamplerCreateInfo samplerInfo = new()
            {
                SType = StructureType.SamplerCreateInfo,
                MagFilter = desc.MagFilter,
                MinFilter = desc.MinFilter,
                MipmapMode = desc.MipmapMode,
                AddressModeU = desc.AddressModeU,
                AddressModeV = desc.AddressModeV,
                AddressModeW = desc.AddressModeW,
                MipLodBias = 0.0f,
                AnisotropyEnable = desc.AnisotropyEnable,
                MaxAnisotropy = desc.MaxAnisotropy,
                CompareEnable = false,
                CompareOp = CompareOp.Always,
                MinLod = 0.0f,
                MaxLod = 1000.0f,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false
            };

            VulkanHelper.Check(_context.Vk.CreateSampler(_context.Device, in samplerInfo, null, out var sampler),
                "Failed to create cached Vulkan sampler.");

            _cache[desc] = sampler;
            return sampler;
        }
    }

    /// <summary>
    /// Destroys all allocated Vulkan samplers.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            foreach (var sampler in _cache.Values)
            {
                if (sampler.Handle != 0)
                {
                    _context.Vk.DestroySampler(_context.Device, sampler, null);
                }
            }

            _cache.Clear();
            _disposed = true;
        }
    }
}
