using Silk.NET.Vulkan;
using VkImage = Silk.NET.Vulkan.Image;
using piko.VulkanMemoryAllocator;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Encapsulates offscreen color and depth render targets with bindless sampling and dynamic resizing.
/// Supports single-target framebuffers or multi-render-target (MRT) G-buffers.
/// </summary>
public sealed unsafe class VulkanRenderTarget : IDisposable
{
    private readonly VulkanContext _context;
    private readonly BindlessManager _bindlessManager;
    private uint _width;
    private uint _height;
    private readonly Format[] _colorFormats;
    private readonly bool _hasDepth;
    private Format _depthFormat;
    private readonly SamplerDescription? _samplerDesc;
    private VulkanTexture[] _colorTextures = [];
    private VkImage _depthImage;
    private DeviceMemory _depthMemory;
    private Allocation _depthAllocation;
    private ImageView _depthImageView;
    private uint _depthTextureId;
    private Sampler _depthSampler;
    private bool _disposed;

    internal float ViewportScale { get; set; } = 1.0f;

    /// <summary>
    /// Gets width in pixels.
    /// </summary>
    public uint Width => _width;

    /// <summary>
    /// Gets height in pixels.
    /// </summary>
    public uint Height => _height;

    /// <summary>
    /// Gets 2D extent matching width and height.
    /// </summary>
    public Extent2D Extent => new(_width, _height);

    /// <summary>
    /// Gets number of color attachments.
    /// </summary>
    public int ColorAttachmentCount => _colorTextures.Length;

    /// <summary>
    /// Gets the primary color texture format, or Format.Undefined if depth-only.
    /// </summary>
    public Format ColorFormat => _colorFormats.Length > 0 ? _colorFormats[0] : Format.Undefined;

    /// <summary>
    /// Gets the primary color texture (attachment 0), or null if depth-only.
    /// </summary>
    public VulkanTexture? ColorTexture => _colorTextures.Length > 0 ? _colorTextures[0] : null;

    /// <summary>
    /// Gets bindless texture slot ID of the primary color texture, or depth texture if depth-only.
    /// </summary>
    public uint TextureId => _colorTextures.Length > 0 ? _colorTextures[0].TextureId : _depthTextureId;

    /// <summary>
    /// Gets all color attachment textures.
    /// </summary>
    public IReadOnlyList<VulkanTexture> ColorTextures => _colorTextures;

    /// <summary>
    /// Gets whether this target includes a depth buffer.
    /// </summary>
    public bool HasDepth => _hasDepth;

    /// <summary>
    /// Gets depth buffer image handle.
    /// </summary>
    public VkImage DepthImage => _depthImage;

    /// <summary>
    /// Gets depth buffer image view.
    /// </summary>
    public ImageView DepthImageView => _depthImageView;

    /// <summary>
    /// Gets depth buffer pixel format.
    /// </summary>
    public Format DepthFormat => _depthFormat;

    /// <summary>
    /// Gets bindless texture slot ID for the depth buffer.
    /// </summary>
    public uint DepthTextureId => _depthTextureId;

    /// <summary>
    /// Gets or sets the current tracked layout of the depth attachment.
    /// </summary>
    public ImageLayout DepthLayout { get; internal set; } = ImageLayout.Undefined;

    /// <summary>
    /// Creates a depth-only render target without color attachments.
    /// </summary>
    public static VulkanRenderTarget CreateDepthOnly(VulkanContext context, BindlessManager bindlessManager, uint width, uint height, Format? depthFormat = null, SamplerDescription? samplerDesc = null) =>
        new(context, bindlessManager, width, height, Array.Empty<Format>(), hasDepth: true, samplerDesc: samplerDesc, depthFormat: depthFormat);

    /// <summary>
    /// Initializes a single color attachment offscreen render target with optional depth buffer.
    /// </summary>
    public VulkanRenderTarget(VulkanContext context, BindlessManager bindless, uint width, uint height,
        Format colorFormat = Format.R8G8B8A8Unorm, bool hasDepth = true, Format? depthFormat = null,
        SamplerDescription? samplerDesc = null)
        : this(context, bindless, width, height, [colorFormat], hasDepth, depthFormat, samplerDesc)
    {
    }

    /// <summary>
    /// Initializes a multi-render-target (MRT) offscreen framebuffer with optional depth buffer.
    /// </summary>
    public VulkanRenderTarget(VulkanContext context, BindlessManager bindless, uint width, uint height,
        ReadOnlySpan<Format> colorFormats, bool hasDepth = true, Format? depthFormat = null,
        SamplerDescription? samplerDesc = null)
    {
        if (width == 0 || height == 0)
            throw new ArgumentException("Render target dimensions must be greater than zero.");
        if (colorFormats.IsEmpty && !hasDepth)
            throw new ArgumentException("Render target must have at least one color attachment or depth attachment.");

        _context = context;
        _bindlessManager = bindless;
        _width = width;
        _height = height;
        _colorFormats = colorFormats.ToArray();
        _hasDepth = hasDepth;
        _samplerDesc = samplerDesc;

        _depthFormat = depthFormat ?? _context.FindSupportedFormat(
            [Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint],
            ImageTiling.Optimal,
            FormatFeatureFlags.DepthStencilAttachmentBit);

        CreateResources();
    }

    /// <summary>
    /// Gets color texture at specified attachment index.
    /// </summary>
    public VulkanTexture GetColorTexture(int index) => _colorTextures[index];

    /// <summary>
    /// Gets bindless texture slot ID of specified color attachment, or depth texture if depth-only.
    /// </summary>
    public uint GetTextureId(int index) => _colorTextures.Length > 0 ? _colorTextures[index].TextureId : _depthTextureId;

    /// <summary>
    /// Reallocates GPU color and depth images at new dimensions while preserving bindless texture slot IDs.
    /// </summary>
    public void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0)
            return;
        if (_width == width && _height == height)
            return;

        _width = width;
        _height = height;

        for (int i = 0; i < _colorTextures.Length; i++)
        {
            _colorTextures[i].Resize(width, height);
        }

        if (_hasDepth)
        {
            var oldDepthView = _depthImageView;
            var oldDepthImage = _depthImage;
            var oldDepthAlloc = _depthAllocation;

            _depthImageView = default;
            _depthImage = default;
            _depthAllocation = default;
            _depthMemory = default;

            _context.DeletionQueue.Enqueue(_context.CurrentFrame, () =>
            {
                if (oldDepthView.Handle != 0)
                    _context.Vk.DestroyImageView(_context.Device, oldDepthView, null);

                if (oldDepthImage.Handle != 0)
                    VMA.DestroyImage(_context.Allocator, oldDepthImage, oldDepthAlloc);
            });

            CreateDepthResources();
        }
    }

    private void CreateResources()
    {
        const ImageUsageFlags colorUsage = ImageUsageFlags.ColorAttachmentBit |
                                           ImageUsageFlags.SampledBit |
                                           ImageUsageFlags.TransferSrcBit |
                                           ImageUsageFlags.TransferDstBit;

        _colorTextures = new VulkanTexture[_colorFormats.Length];
        for (int i = 0; i < _colorFormats.Length; i++)
        {
            _colorTextures[i] = new VulkanTexture(
                _context,
                _bindlessManager,
                _width,
                _height,
                _colorFormats[i],
                colorUsage,
                _samplerDesc);
        }

        if (_hasDepth)
        {
            CreateDepthResources();
        }
    }

    private void CreateDepthResources()
    {
        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(_width, _height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Format = _depthFormat,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = ImageUsageFlags.DepthStencilAttachmentBit | ImageUsageFlags.SampledBit,
            SharingMode = SharingMode.Exclusive,
            Samples = SampleCountFlags.Count1Bit
        };

        AllocationCreateInfo allocCreateInfo = new()
        {
            Usage = MemoryUsage.AutoPreferDevice
        };

        Silk.NET.Vulkan.Image img;
        Allocation alloc;
        AllocationInfo allocDetails;
        VulkanHelper.Check(
            VMA.CreateImage(_context.Allocator, &imageInfo, &allocCreateInfo, &img, &alloc, &allocDetails),
            "Failed to create depth image for render target via VMA.");
        _depthImage = img;
        _depthAllocation = alloc;
        _depthMemory = allocDetails.DeviceMemory;

        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _depthImage,
            ViewType = ImageViewType.Type2D,
            Format = _depthFormat,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.DepthBit, 0, 1, 0, 1)
        };

        VulkanHelper.Check(_context.Vk.CreateImageView(_context.Device, in viewInfo, null, out _depthImageView),
            "Failed to create depth image view for render target.");

        if (_depthSampler.Handle == 0)
        {
            _depthSampler = _bindlessManager.SamplerManager.GetOrCreateSampler(_samplerDesc ?? SamplerDescription.LinearClamp);
        }

        if (_depthTextureId == 0)
        {
            _depthTextureId = _bindlessManager.RegisterTexture(_depthImageView, _depthSampler);
        }
        else
        {
            _bindlessManager.UpdateDescriptor(_depthTextureId, _depthImageView, _depthSampler);
        }

        DepthLayout = ImageLayout.Undefined;
    }

    private void CreateDepthBuffer() => CreateDepthResources();

    private void DestroyDepthResources()
    {
        if (_depthTextureId != 0)
        {
            _bindlessManager.UnregisterTexture(_depthTextureId);
            _depthTextureId = 0;
        }

        if (_depthImageView.Handle != 0)
        {
            _context.Vk.DestroyImageView(_context.Device, _depthImageView, null);
            _depthImageView = default;
        }

        if (_depthImage.Handle != 0)
        {
            VMA.DestroyImage(_context.Allocator, _depthImage, _depthAllocation);
            _depthImage = default;
            _depthAllocation = default;
            _depthMemory = default;
        }
    }

    private void DestroyDepthBuffer() => DestroyDepthResources();

    private void DestroyResources()
    {
        for (int i = 0; i < _colorTextures.Length; i++)
        {
            _colorTextures[i]?.Dispose();
        }
        _colorTextures = [];

        DestroyDepthResources();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        DestroyResources();
        _disposed = true;
    }
}
