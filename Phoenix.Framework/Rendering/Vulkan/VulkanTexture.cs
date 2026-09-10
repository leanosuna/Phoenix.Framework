using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;
using VkImage = Silk.NET.Vulkan.Image;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Encapsulates a GPU-resident 2D texture, staging upload pipeline, image view, and bindless descriptor registration.
/// </summary>
public sealed unsafe class VulkanTexture : IDisposable
{
    private readonly VulkanContext _context;
    private readonly BindlessManager _bindlessManager;
    private VkImage _image;
    private DeviceMemory _memory;
    private ImageView _imageView;
    private readonly uint _width;
    private readonly uint _height;
    private readonly Format _format;
    private readonly uint _textureId;
    private bool _disposed;

    public VkImage Image => _image;
    public DeviceMemory Memory => _memory;
    public ImageView ImageView => _imageView;
    public uint Width => _width;
    public uint Height => _height;
    public Format Format => _format;
    public uint TextureId => _textureId;

    /// <summary>
    /// Creates a GPU image, uploads raw pixel data via a staging buffer with synchronization2 pipeline barriers,
    /// creates an image view, and registers the texture in the global bindless manager.
    /// </summary>
    public VulkanTexture(VulkanContext context, BindlessManager bindless, int width, int height,
        ReadOnlySpan<byte> pixelData, Format format = Format.R8G8B8A8Unorm, bool isDefaultSlot0 = false)
    {
        _context = context;
        _bindlessManager = bindless;
        _width = (uint)width;
        _height = (uint)height;
        _format = format;

        CreateImage(width, height, format);
        UploadPixels(width, height, pixelData);
        CreateImageView(format);

        if (isDefaultSlot0)
        {
            _textureId = 0;
            _bindlessManager.UpdateDescriptor(0, _imageView, _bindlessManager.DefaultSampler);
        }
        else
        {
            _textureId = _bindlessManager.RegisterTexture(_imageView);
        }
    }

    /// <summary>
    /// Allocates optimal GPU device-local memory and creates the VkImage handle.
    /// </summary>
    private void CreateImage(int width, int height, Format format)
    {
        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D((uint)width, (uint)height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Format = format,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            SharingMode = SharingMode.Exclusive,
            Samples = SampleCountFlags.Count1Bit
        };

        VulkanHelper.Check(_context.Vk.CreateImage(_context.Device, in imageInfo, null, out _image),
            "Failed to create Vulkan texture image.");

        _context.Vk.GetImageMemoryRequirements(_context.Device, _image, out var memReqs);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = _context.FindMemoryType(memReqs.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };

        VulkanHelper.Check(_context.Vk.AllocateMemory(_context.Device, in allocInfo, null, out _memory),
            "Failed to allocate Vulkan texture image memory.");

        VulkanHelper.Check(_context.Vk.BindImageMemory(_context.Device, _image, _memory, 0),
            "Failed to bind Vulkan texture image memory.");
    }

    /// <summary>
    /// Uploads pixel data using a staging buffer and transitions the image to ShaderReadOnlyOptimal layout.
    /// </summary>
    private void UploadPixels(int width, int height, ReadOnlySpan<byte> pixelData)
    {
        using var stagingBuffer = new VulkanBuffer(_context, (ulong)pixelData.Length,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        stagingBuffer.SetData(pixelData);

        var cmd = _context.BeginSingleTimeCommands();

        ImageMemoryBarrier2 barrierToDst = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.TopOfPipeBit,
            SrcAccessMask = AccessFlags2.None,
            DstStageMask = PipelineStageFlags2.TransferBit,
            DstAccessMask = AccessFlags2.TransferWriteBit,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.TransferDstOptimal,
            Image = _image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        DependencyInfo depToDst = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrierToDst
        };

        _context.Vk.CmdPipelineBarrier2(cmd, in depToDst);

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = new Extent3D((uint)width, (uint)height, 1)
        };

        _context.Vk.CmdCopyBufferToImage(cmd, stagingBuffer.Buffer, _image, ImageLayout.TransferDstOptimal, 1, in region);

        ImageMemoryBarrier2 barrierToShader = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.TransferBit,
            SrcAccessMask = AccessFlags2.TransferWriteBit,
            DstStageMask = PipelineStageFlags2.FragmentShaderBit,
            DstAccessMask = AccessFlags2.ShaderReadBit,
            OldLayout = ImageLayout.TransferDstOptimal,
            NewLayout = ImageLayout.ShaderReadOnlyOptimal,
            Image = _image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        DependencyInfo depToShader = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrierToShader
        };

        _context.Vk.CmdPipelineBarrier2(cmd, in depToShader);

        _context.EndSingleTimeCommands(cmd);
    }

    /// <summary>
    /// Creates a 2D color image view for sampling the texture.
    /// </summary>
    private void CreateImageView(Format format)
    {
        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        VulkanHelper.Check(_context.Vk.CreateImageView(_context.Device, in viewInfo, null, out _imageView),
            "Failed to create Vulkan texture image view.");
    }

    /// <summary>
    /// Decodes an image from file and uploads it to the GPU as a bindless texture.
    /// </summary>
    public static VulkanTexture FromFile(VulkanContext context, BindlessManager bindless, string filePath, Format format = Format.R8G8B8A8Srgb)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        return new VulkanTexture(context, bindless, image.Width, image.Height, pixels, format);
    }

    /// <summary>
    /// Decodes an image from stream and uploads it to the GPU as a bindless texture.
    /// </summary>
    public static VulkanTexture FromStream(VulkanContext context, BindlessManager bindless, Stream stream, Format format = Format.R8G8B8A8Srgb)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        return new VulkanTexture(context, bindless, image.Width, image.Height, pixels, format);
    }

    /// <summary>
    /// Releases the bindless slot allocation, image view, image, and allocated GPU memory.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_textureId != 0)
        {
            _bindlessManager.UnregisterTexture(_textureId);
        }

        if (_imageView.Handle != 0)
        {
            _context.Vk.DestroyImageView(_context.Device, _imageView, null);
            _imageView = default;
        }

        if (_image.Handle != 0)
        {
            _context.Vk.DestroyImage(_context.Device, _image, null);
            _image = default;
        }

        if (_memory.Handle != 0)
        {
            _context.Vk.FreeMemory(_context.Device, _memory, null);
            _memory = default;
        }

        _disposed = true;
    }
}
