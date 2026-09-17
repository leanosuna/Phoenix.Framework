using Phoenix.Framework.AssetImport;
using Phoenix.Framework.AssetImport.Processing;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using VkImage = Silk.NET.Vulkan.Image;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Encapsulates a GPU-resident 2D texture, staging upload pipeline, optional GPU mipmap generation,
/// image view, and bindless descriptor registration with custom or default sampling states.
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
    private readonly uint _mipLevels;
    private readonly Format _format;
    private readonly uint _textureId;
    private readonly bool _ownsSlot;
    private readonly Sampler _sampler;
    private bool _disposed;

    public VkImage Image => _image;
    public DeviceMemory Memory => _memory;
    public ImageView ImageView => _imageView;
    public Sampler Sampler => _sampler;
    public uint Width => _width;
    public uint Height => _height;
    public uint MipLevels => _mipLevels;
    public Format Format => _format;
    public uint TextureId => _textureId;
    public TextureLoadOptions Options { get; }

    /// <summary>
    /// Creates a GPU image, uploads raw pixel data via a staging buffer with optional GPU mipmap generation,
    /// creates an image view, and registers the texture in the global bindless manager.
    /// </summary>
    public VulkanTexture(VulkanContext context, BindlessManager bindless, int width, int height,
        ReadOnlySpan<byte> pixelData, TextureLoadOptions? options = null, Format format = Format.R8G8B8A8Unorm,
        bool isDefaultSlot0 = false, uint? preallocatedSlot = null)
    {
        _context = context;
        _bindlessManager = bindless;
        _width = (uint)width;
        _height = (uint)height;
        _format = format;
        Options = options ?? new TextureLoadOptions();
        _mipLevels = Options.GenerateMipmaps ? (uint)Math.Floor(Math.Log2(Math.Max(width, height))) + 1 : 1;
        _ownsSlot = !isDefaultSlot0 && !preallocatedSlot.HasValue;

        CreateImage(width, height, format, _mipLevels);
        UploadPixels(width, height, pixelData);
        CreateImageView(format, _mipLevels);

        if (isDefaultSlot0)
        {
            _textureId = 0;
            _sampler = _bindlessManager.DefaultSampler;
            _bindlessManager.UpdateDescriptor(0, _imageView, _sampler);
        }
        else if (preallocatedSlot.HasValue)
        {
            _textureId = preallocatedSlot.Value;
            var samplerDesc = SamplerDescription.FromTextureOptions(Options);
            _sampler = _bindlessManager.SamplerManager.GetOrCreateSampler(samplerDesc);
            _bindlessManager.UpdateDescriptor(_textureId, _imageView, _sampler);
        }
        else
        {
            var samplerDesc = SamplerDescription.FromTextureOptions(Options);
            _sampler = _bindlessManager.SamplerManager.GetOrCreateSampler(samplerDesc);
            _textureId = _bindlessManager.RegisterTexture(_imageView, in samplerDesc);
        }
    }

    /// <summary>
    /// Creates a GPU image from pre-compressed BCn mipmap chain data, uploads via a staging buffer,
    /// creates an image view, and registers into the bindless manager (or updates a preallocated slot).
    /// </summary>
    public VulkanTexture(VulkanContext context, BindlessManager bindless, CompressedTextureData data,
        TextureLoadOptions? options = null, uint? preallocatedSlot = null)
    {
        _context = context;
        _bindlessManager = bindless;
        _width = (uint)data.Width;
        _height = (uint)data.Height;
        Options = options ?? new TextureLoadOptions();
        _mipLevels = (uint)data.Mips.Count;
        _format = MapFormat(data.Format, data.IsSRgb);
        _ownsSlot = !preallocatedSlot.HasValue;

        CreateImage(data.Width, data.Height, _format, _mipLevels);
        UploadCompressedMips(data.Mips);
        CreateImageView(_format, _mipLevels);

        var samplerDesc = SamplerDescription.FromTextureOptions(Options);
        _sampler = _bindlessManager.SamplerManager.GetOrCreateSampler(samplerDesc);

        if (preallocatedSlot.HasValue)
        {
            _textureId = preallocatedSlot.Value;
            _bindlessManager.UpdateDescriptor(_textureId, _imageView, _sampler);
        }
        else
        {
            _textureId = _bindlessManager.RegisterTexture(_imageView, _sampler);
        }
    }

    /// <summary>
    /// Creates a GPU-resident offscreen render target texture without initial pixel staging upload.
    /// Allocates optimal device-local memory and registers into the global bindless manager.
    /// </summary>
    public VulkanTexture(VulkanContext context, BindlessManager bindless, uint width, uint height,
        Format format, ImageUsageFlags usageFlags, SamplerDescription? samplerDesc = null)
    {
        _context = context;
        _bindlessManager = bindless;
        _width = width;
        _height = height;
        _format = format;
        _mipLevels = 1;
        Options = new TextureLoadOptions { GenerateMipmaps = false };
        _ownsSlot = true;

        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(width, height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Format = format,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = usageFlags,
            SharingMode = SharingMode.Exclusive,
            Samples = SampleCountFlags.Count1Bit
        };

        VulkanHelper.Check(_context.Vk.CreateImage(_context.Device, in imageInfo, null, out _image),
            "Failed to create Vulkan render target image.");

        _context.Vk.GetImageMemoryRequirements(_context.Device, _image, out var memReqs);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = _context.FindMemoryType(memReqs.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };

        VulkanHelper.Check(_context.Vk.AllocateMemory(_context.Device, in allocInfo, null, out _memory),
            "Failed to allocate Vulkan render target image memory.");

        VulkanHelper.Check(_context.Vk.BindImageMemory(_context.Device, _image, _memory, 0),
            "Failed to bind Vulkan render target image memory.");

        CreateImageView(format, 1);

        var sDesc = samplerDesc ?? SamplerDescription.LinearClamp;
        _sampler = _bindlessManager.SamplerManager.GetOrCreateSampler(sDesc);
        _textureId = _bindlessManager.RegisterTexture(_imageView, in sDesc);

        var cmd = _context.BeginSingleTimeCommands();
        ImageMemoryBarrier2 barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.TopOfPipeBit,
            SrcAccessMask = AccessFlags2.None,
            DstStageMask = PipelineStageFlags2.FragmentShaderBit | PipelineStageFlags2.ComputeShaderBit,
            DstAccessMask = AccessFlags2.ShaderReadBit,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.ShaderReadOnlyOptimal,
            Image = _image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        DependencyInfo dep = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrier
        };

        _context.Vk.CmdPipelineBarrier2(cmd, in dep);
        _context.EndSingleTimeCommands(cmd);
    }


    /// <summary>
    /// Allocates optimal GPU device-local memory and creates the VkImage handle.
    /// </summary>
    private void CreateImage(int width, int height, Format format, uint mipLevels)
    {
        ImageUsageFlags usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit;
        if (mipLevels > 1)
            usage |= ImageUsageFlags.TransferSrcBit;

        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D((uint)width, (uint)height, 1),
            MipLevels = mipLevels,
            ArrayLayers = 1,
            Format = format,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
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

        if (_mipLevels > 1)
        {
            GenerateMipmaps(cmd, width, height, _mipLevels);
        }
        else
        {
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
        }

        _context.EndSingleTimeCommands(cmd);
    }

    /// <summary>
    /// Uploads all pre-compressed mipmap levels from a single contiguous staging buffer and transitions to ShaderReadOnlyOptimal.
    /// </summary>
    private void UploadCompressedMips(List<CompressedMipLevel> mips)
    {
        ulong totalBytes = 0;
        for (int i = 0; i < mips.Count; i++)
        {
            totalBytes += (ulong)mips[i].Data.Length;
        }

        using var stagingBuffer = new VulkanBuffer(_context, totalBytes,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        ulong currentOffset = 0;
        BufferImageCopy[] copyRegions = new BufferImageCopy[mips.Count];

        for (int i = 0; i < mips.Count; i++)
        {
            var mip = mips[i];
            stagingBuffer.SetData<byte>(mip.Data.AsSpan(), currentOffset);

            copyRegions[i] = new BufferImageCopy
            {
                BufferOffset = currentOffset,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, (uint)i, 0, 1),
                ImageOffset = new Offset3D(0, 0, 0),
                ImageExtent = new Extent3D((uint)mip.Width, (uint)mip.Height, 1)
            };

            currentOffset += (ulong)mip.Data.Length;
        }

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
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, _mipLevels, 0, 1)
        };

        DependencyInfo depToDst = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrierToDst
        };

        _context.Vk.CmdPipelineBarrier2(cmd, in depToDst);

        fixed (BufferImageCopy* pRegions = copyRegions)
        {
            _context.Vk.CmdCopyBufferToImage(cmd, stagingBuffer.Buffer, _image, ImageLayout.TransferDstOptimal, (uint)copyRegions.Length, pRegions);
        }

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
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, _mipLevels, 0, 1)
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
    /// Maps framework compression format and color space flag to the corresponding Vulkan format enum.
    /// </summary>
    public static Format MapFormat(TextureCompressionFormat format, bool isSRgb) => format switch
    {
        TextureCompressionFormat.BC1 => isSRgb ? Format.BC1RgbaSrgbBlock : Format.BC1RgbaUnormBlock,
        TextureCompressionFormat.BC3 => isSRgb ? Format.BC3SrgbBlock : Format.BC3UnormBlock,
        TextureCompressionFormat.BC4 => Format.BC4UnormBlock,
        TextureCompressionFormat.BC5 => Format.BC5UnormBlock,
        TextureCompressionFormat.BC7 => isSRgb ? Format.BC7SrgbBlock : Format.BC7UnormBlock,
        _ => isSRgb ? Format.R8G8B8A8Srgb : Format.R8G8B8A8Unorm
    };

    /// <summary>
    /// Generates successive mipmap levels on the GPU using hardware-accelerated linear blit operations.
    /// </summary>
    private void GenerateMipmaps(CommandBuffer cmd, int width, int height, uint mipLevels)
    {
        int mipWidth = width;
        int mipHeight = height;
        ImageMemoryBarrier2* barriers = stackalloc ImageMemoryBarrier2[2];

        for (uint i = 1; i < mipLevels; i++)
        {
            ImageMemoryBarrier2 barrierSrc = new()
            {
                SType = StructureType.ImageMemoryBarrier2,
                SrcStageMask = PipelineStageFlags2.TransferBit,
                SrcAccessMask = AccessFlags2.TransferWriteBit,
                DstStageMask = PipelineStageFlags2.TransferBit,
                DstAccessMask = AccessFlags2.TransferReadBit,
                OldLayout = ImageLayout.TransferDstOptimal,
                NewLayout = ImageLayout.TransferSrcOptimal,
                Image = _image,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, i - 1, 1, 0, 1)
            };

            ImageMemoryBarrier2 barrierDst = new()
            {
                SType = StructureType.ImageMemoryBarrier2,
                SrcStageMask = PipelineStageFlags2.TopOfPipeBit,
                SrcAccessMask = AccessFlags2.None,
                DstStageMask = PipelineStageFlags2.TransferBit,
                DstAccessMask = AccessFlags2.TransferWriteBit,
                OldLayout = ImageLayout.Undefined,
                NewLayout = ImageLayout.TransferDstOptimal,
                Image = _image,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, i, 1, 0, 1)
            };

            barriers[0] = barrierSrc;
            barriers[1] = barrierDst;

            DependencyInfo dep = new()
            {
                SType = StructureType.DependencyInfo,
                ImageMemoryBarrierCount = 2,
                PImageMemoryBarriers = barriers
            };

            _context.Vk.CmdPipelineBarrier2(cmd, in dep);

            int nextWidth = mipWidth > 1 ? mipWidth / 2 : 1;
            int nextHeight = mipHeight > 1 ? mipHeight / 2 : 1;

            ImageBlit blit = new()
            {
                SrcOffsets = { [0] = new Offset3D(0, 0, 0), [1] = new Offset3D(mipWidth, mipHeight, 1) },
                SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, i - 1, 0, 1),
                DstOffsets = { [0] = new Offset3D(0, 0, 0), [1] = new Offset3D(nextWidth, nextHeight, 1) },
                DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, i, 0, 1)
            };

            _context.Vk.CmdBlitImage(cmd, _image, ImageLayout.TransferSrcOptimal, _image, ImageLayout.TransferDstOptimal, 1, in blit, Filter.Linear);

            ImageMemoryBarrier2 barrierRead = new()
            {
                SType = StructureType.ImageMemoryBarrier2,
                SrcStageMask = PipelineStageFlags2.TransferBit,
                SrcAccessMask = AccessFlags2.TransferReadBit,
                DstStageMask = PipelineStageFlags2.FragmentShaderBit,
                DstAccessMask = AccessFlags2.ShaderReadBit,
                OldLayout = ImageLayout.TransferSrcOptimal,
                NewLayout = ImageLayout.ShaderReadOnlyOptimal,
                Image = _image,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, i - 1, 1, 0, 1)
            };

            DependencyInfo depRead = new()
            {
                SType = StructureType.DependencyInfo,
                ImageMemoryBarrierCount = 1,
                PImageMemoryBarriers = &barrierRead
            };

            _context.Vk.CmdPipelineBarrier2(cmd, in depRead);

            mipWidth = nextWidth;
            mipHeight = nextHeight;
        }

        ImageMemoryBarrier2 barrierLast = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.TransferBit,
            SrcAccessMask = AccessFlags2.TransferWriteBit,
            DstStageMask = PipelineStageFlags2.FragmentShaderBit,
            DstAccessMask = AccessFlags2.ShaderReadBit,
            OldLayout = ImageLayout.TransferDstOptimal,
            NewLayout = ImageLayout.ShaderReadOnlyOptimal,
            Image = _image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, mipLevels - 1, 1, 0, 1)
        };

        DependencyInfo depLast = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrierLast
        };

        _context.Vk.CmdPipelineBarrier2(cmd, in depLast);
    }

    /// <summary>
    /// Creates a 2D color image view for sampling the texture across all mip levels.
    /// </summary>
    private void CreateImageView(Format format, uint mipLevels)
    {
        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, mipLevels, 0, 1)
        };

        VulkanHelper.Check(_context.Vk.CreateImageView(_context.Device, in viewInfo, null, out _imageView),
            "Failed to create Vulkan texture image view.");
    }

    /// <summary>
    /// Decodes an image from file and uploads it to the GPU as a bindless texture.
    /// </summary>
    public static VulkanTexture FromFile(VulkanContext context, BindlessManager bindless, string filePath,
        TextureLoadOptions? options = null, Format format = Format.R8G8B8A8Srgb)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);
        ApplySizeLimit(image, options);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        return new VulkanTexture(context, bindless, image.Width, image.Height, pixels, options, format);
    }

    /// <summary>
    /// Decodes an image from stream and uploads it to the GPU as a bindless texture.
    /// </summary>
    public static VulkanTexture FromStream(VulkanContext context, BindlessManager bindless, Stream stream,
        TextureLoadOptions? options = null, Format format = Format.R8G8B8A8Srgb)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
        ApplySizeLimit(image, options);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        return new VulkanTexture(context, bindless, image.Width, image.Height, pixels, options, format);
    }

    private static void ApplySizeLimit(SixLabors.ImageSharp.Image<Rgba32> image, TextureLoadOptions? options)
    {
        if (options == null || !options.LimitSize)
            return;

        int maxDim = options.MaxSize > 0 ? options.MaxSize : 1024;
        if (image.Width <= maxDim && image.Height <= maxDim)
            return;

        int targetW, targetH;
        if (image.Width >= image.Height)
        {
            targetW = maxDim;
            targetH = Math.Max(1, (int)Math.Round((double)image.Height * maxDim / image.Width));
        }
        else
        {
            targetH = maxDim;
            targetW = Math.Max(1, (int)Math.Round((double)image.Width * maxDim / image.Height));
        }

        image.Mutate(x => x.Resize(targetW, targetH));
    }

    /// <summary>
    /// Releases the bindless slot allocation, image view, image, and allocated GPU memory.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_ownsSlot && _textureId != 0)
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
