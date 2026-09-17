using Silk.NET.Vulkan;
using VkImage = Silk.NET.Vulkan.Image;

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
    private ImageView _depthImageView;
    private bool _disposed;

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
    /// Gets the primary color texture (attachment 0).
    /// </summary>
    public VulkanTexture ColorTexture => _colorTextures[0];

    /// <summary>
    /// Gets bindless texture slot ID of the primary color texture.
    /// </summary>
    public uint TextureId => _colorTextures[0].TextureId;

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
        if (colorFormats.Length == 0 && !hasDepth)
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
    /// Gets bindless texture slot ID of specified color attachment.
    /// </summary>
    public uint GetTextureId(int index) => _colorTextures[index].TextureId;

    /// <summary>
    /// Reallocates GPU color and depth images at new dimensions.
    /// </summary>
    public void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0)
            return;
        if (_width == width && _height == height)
            return;

        _width = width;
        _height = height;

        DestroyResources();
        CreateResources();
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
            CreateDepthBuffer();
        }
    }

    private void CreateDepthBuffer()
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

        VulkanHelper.Check(_context.Vk.CreateImage(_context.Device, in imageInfo, null, out _depthImage),
            "Failed to create depth image for render target.");

        _context.Vk.GetImageMemoryRequirements(_context.Device, _depthImage, out var memReqs);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = _context.FindMemoryType(memReqs.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };

        VulkanHelper.Check(_context.Vk.AllocateMemory(_context.Device, in allocInfo, null, out _depthMemory),
            "Failed to allocate depth image memory for render target.");

        VulkanHelper.Check(_context.Vk.BindImageMemory(_context.Device, _depthImage, _depthMemory, 0),
            "Failed to bind depth image memory for render target.");

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
    }

    private void DestroyResources()
    {
        for (int i = 0; i < _colorTextures.Length; i++)
        {
            _colorTextures[i]?.Dispose();
        }
        _colorTextures = [];

        if (_depthImageView.Handle != 0)
        {
            _context.Vk.DestroyImageView(_context.Device, _depthImageView, null);
            _depthImageView = default;
        }

        if (_depthImage.Handle != 0)
        {
            _context.Vk.DestroyImage(_context.Device, _depthImage, null);
            _depthImage = default;
        }

        if (_depthMemory.Handle != 0)
        {
            _context.Vk.FreeMemory(_context.Device, _depthMemory, null);
            _depthMemory = default;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        DestroyResources();
        _disposed = true;
    }
}
