using System;
using System.Diagnostics;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;
using Silk.NET.Vulkan;
using static MintyCore.Render.VulkanUtils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Render;

public readonly unsafe struct Texture : IDisposable
{
    private static Vk Vk => VulkanEngine.Vk;

    public readonly Image Image;
    public readonly MemoryBlock MemoryBlock;
    public readonly Buffer StagingBuffer;
    public readonly Format Format;
    private readonly uint _actualImageArrayLayers;

    public readonly uint Width;
    public readonly uint Height;
    public readonly uint Depth;

    public readonly uint MipLevels;
    public readonly uint ArrayLayers;
    public readonly TextureUsage Usage;
    public readonly ImageType Type;
    public readonly SampleCountFlags SampleCount;
    public readonly UnmanagedArray<ImageLayout> ImageLayouts;

    private readonly byte _isSwapchainTexture;
    public bool IsSwapchainTexture => _isSwapchainTexture != 0;

    public Texture(ref TextureDescription description) : this()
    {
        Width = description.Width;
        Height = description.Height;
        Depth = description.Depth;
        MipLevels = description.MipLevels;
        ArrayLayers = description.ArrayLayers;
        var isCubemap = (description.Usage & TextureUsage.CUBEMAP) == TextureUsage.CUBEMAP;
        _actualImageArrayLayers = isCubemap
            ? 6 * ArrayLayers
            : ArrayLayers;
        Format = description.Format;
        Usage = description.Usage;
        Type = description.Type;
        SampleCount = description.SampleCount;

        var isStaging = (Usage & TextureUsage.STAGING) == TextureUsage.STAGING;

        if (!isStaging)
        {
            ImageCreateInfo imageCi = new()
            {
                SType = StructureType.ImageCreateInfo,
                MipLevels = MipLevels,
                ArrayLayers = _actualImageArrayLayers,
                ImageType = Type,
                Extent =
                {
                    Width = Width,
                    Height = Height,
                    Depth = Depth,
                },
                InitialLayout = ImageLayout.Preinitialized,
                Usage = VdToVkTextureUsage(Usage),
                Tiling = isStaging ? ImageTiling.Linear : ImageTiling.Optimal,
                Format = Format,
                Flags = ImageCreateFlags.ImageCreateMutableFormatBit,
                Samples = SampleCount
            };

            if (isCubemap) imageCi.Flags |= ImageCreateFlags.ImageCreateCubeCompatibleBit;

            var subresourceCount = MipLevels * _actualImageArrayLayers * Depth;
            Assert(Vk.CreateImage(VulkanEngine.Device, imageCi, VulkanEngine.AllocationCallback, out Image));

            ImageMemoryRequirementsInfo2 memReqsInfo2 = new()
            {
                SType = StructureType.ImageMemoryRequirementsInfo2Khr,
                Image = Image
            };
            MemoryRequirements memReqs2;
            MemoryDedicatedRequirementsKHR dedicatedReqs = new()
            {
                SType = StructureType.MemoryDedicatedRequirementsKhr
            };
            //memReqs2.PNext = &dedicatedReqs;
            Vk.GetImageMemoryRequirements(VulkanEngine.Device, Image, &memReqs2);
            // var memoryRequirements = memReqs2.MemoryRequirements;

            var memoryToken = MemoryManager.Allocate(
                memReqs2.MemoryTypeBits,
                MemoryPropertyFlags.MemoryPropertyDeviceLocalBit,
                false,
                memReqs2.Size,
                memReqs2.Alignment,
                true,
                Image);
            MemoryBlock = memoryToken;
            Assert(Vk.BindImageMemory(VulkanEngine.Device, Image, MemoryBlock.DeviceMemory, MemoryBlock.Offset));

            ImageLayouts = new UnmanagedArray<ImageLayout>((int)subresourceCount);
            for (var i = 0; i < ImageLayouts.Length; i++) ImageLayouts[i] = ImageLayout.Preinitialized;
        }
        else // isStaging
        {
            var depthPitch = FormatHelpers.GetDepthPitch(
                FormatHelpers.GetRowPitch(Width, Format),
                Height,
                Format);
            var stagingSize = depthPitch * Depth;
            for (uint level = 1; level < MipLevels; level++)
            {
                GetMipDimensions(this, level, out var mipWidth, out var mipHeight, out var mipDepth);

                depthPitch = FormatHelpers.GetDepthPitch(
                    FormatHelpers.GetRowPitch(mipWidth, Format),
                    mipHeight,
                    Format);

                stagingSize += depthPitch * mipDepth;
            }

            stagingSize *= ArrayLayers;

            BufferCreateInfo bufferCi = new()
            {
                SType = StructureType.BufferCreateInfo,
                Usage = BufferUsageFlags.BufferUsageTransferSrcBit |
                        BufferUsageFlags.BufferUsageTransferDstBit,
                Size = stagingSize
            };
            Assert(Vk.CreateBuffer(VulkanEngine.Device, bufferCi, VulkanEngine.AllocationCallback,
                out StagingBuffer));
                
            Vk.GetBufferMemoryRequirements(VulkanEngine.Device, StagingBuffer, out var memReqs);

            // Use "host cached" memory when available, for better performance of GPU -> CPU transfers
            var propertyFlags = MemoryPropertyFlags.MemoryPropertyHostVisibleBit |
                                MemoryPropertyFlags.MemoryPropertyHostCoherentBit |
                                MemoryPropertyFlags.MemoryPropertyHostCachedBit;
            if (!FindMemoryType(memReqs.MemoryTypeBits, propertyFlags, out _))
                propertyFlags ^= MemoryPropertyFlags.MemoryPropertyHostCachedBit;

            MemoryBlock = MemoryManager.Allocate(
                memReqs.MemoryTypeBits,
                propertyFlags,
                true,
                memReqs.Size,
                memReqs.Alignment,
                true,
                default,
                StagingBuffer);

            Assert(Vk.BindBufferMemory(VulkanEngine.Device, StagingBuffer, MemoryBlock.DeviceMemory,
                MemoryBlock.Offset));
        }

        ClearIfRenderTarget();
        TransitionIfSampled();
    }

    // Used to construct Swapchain textures.
    public Texture(
        uint width,
        uint height,
        uint mipLevels,
        uint arrayLayers,
        Format vkFormat,
        TextureUsage usage,
        SampleCountFlags sampleCount,
        Image existingImage) : this()
    {
        Debug.Assert(width > 0 && height > 0);
        MipLevels = mipLevels;
        Width = width;
        Height = height;
        Depth = 1;
        Format = vkFormat;
        ArrayLayers = arrayLayers;
        Usage = usage;
        Type = ImageType.ImageType2D;
        SampleCount = sampleCount;
        Image = existingImage;
        ImageLayouts = new UnmanagedArray<ImageLayout>(1)
        {
            [0] = ImageLayout.Undefined
        };

        _isSwapchainTexture = 1;

        ClearIfRenderTarget();
    }

    private void ClearIfRenderTarget()
    {
        // If the image is going to be used as a render target, we need to clear the data before its first use.
        if ((Usage & TextureUsage.RENDER_TARGET) != 0)
            VulkanEngine.ClearColorTexture(this, new ClearColorValue(0, 0, 0, 0));
        else if ((Usage & TextureUsage.DEPTH_STENCIL) != 0)
            VulkanEngine.ClearDepthTexture(this, new ClearDepthStencilValue(0, 0));
    }

    private void TransitionIfSampled()
    {
        if ((Usage & TextureUsage.SAMPLED) != 0)
            VulkanEngine.TransitionImageLayout(this, ImageLayout.ShaderReadOnlyOptimal);
    }

    public SubresourceLayout GetSubresourceLayout(uint subresource)
    {
        var staging = StagingBuffer.Handle != 0;
        GetMipLevelAndArrayLayer(this, subresource, out var mipLevel, out var arrayLayer);
        if (!staging)
        {
            var aspect = (Usage & TextureUsage.DEPTH_STENCIL) == TextureUsage.DEPTH_STENCIL
                ? ImageAspectFlags.ImageAspectDepthBit | ImageAspectFlags.ImageAspectStencilBit
                : ImageAspectFlags.ImageAspectColorBit;
            var imageSubresource = new ImageSubresource
            {
                ArrayLayer = arrayLayer,
                MipLevel = mipLevel,
                AspectMask = aspect,
            };

            Vk.GetImageSubresourceLayout(VulkanEngine.Device, Image, imageSubresource,
                out var layout);
            return layout;
        }
        else
        {
            GetMipDimensions(this, mipLevel, out var mipWidth, out var mipHeight, out _);
            var rowPitch = FormatHelpers.GetRowPitch(mipWidth, Format);
            var depthPitch = FormatHelpers.GetDepthPitch(rowPitch, mipHeight, Format);

            SubresourceLayout layout = new()
            {
                RowPitch = rowPitch,
                DepthPitch = depthPitch,
                ArrayPitch = depthPitch,
                Size = depthPitch,
            };
            layout.Offset = ComputeSubresourceOffset(this, mipLevel, arrayLayer);

            return layout;
        }
    }

    public void TransitionImageLayout(
        CommandBuffer cb,
        uint baseMipLevel,
        uint levelCount,
        uint baseArrayLayer,
        uint layerCount,
        ImageLayout newLayout)
    {
        if (StagingBuffer.Handle != 0) return;

        var oldLayout = ImageLayouts[CalculateSubresource(baseMipLevel, baseArrayLayer)];
#if DEBUG
        for (uint level = 0; level < levelCount; level++)
        for (uint layer = 0; layer < layerCount; layer++)
            if (ImageLayouts[CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] != oldLayout)
                throw new MintyCoreException("Unexpected image layout.");
#endif
        if (oldLayout != newLayout)
        {
            ImageAspectFlags aspectMask;
            if ((Usage & TextureUsage.DEPTH_STENCIL) != 0)
                aspectMask = FormatHelpers.IsStencilFormat(Format)
                    ? ImageAspectFlags.ImageAspectDepthBit | ImageAspectFlags.ImageAspectStencilBit
                    : ImageAspectFlags.ImageAspectDepthBit;
            else
                aspectMask = ImageAspectFlags.ImageAspectColorBit;

            VulkanUtils.TransitionImageLayout(
                cb,
                Image,
                baseMipLevel,
                levelCount,
                baseArrayLayer,
                layerCount,
                aspectMask,
                ImageLayouts[CalculateSubresource(baseMipLevel, baseArrayLayer)],
                newLayout);

            for (uint level = 0; level < levelCount; level++)
            for (uint layer = 0; layer < layerCount; layer++)
                ImageLayouts[CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] = newLayout;
        }
    }

    public void TransitionImageLayoutNonmatching(
        CommandBuffer cb,
        uint baseMipLevel,
        uint levelCount,
        uint baseArrayLayer,
        uint layerCount,
        ImageLayout newLayout)
    {
        if (StagingBuffer.Handle != 0) return;

        for (var level = baseMipLevel; level < baseMipLevel + levelCount; level++)
        for (var layer = baseArrayLayer; layer < baseArrayLayer + layerCount; layer++)
        {
            var subresource = CalculateSubresource(level, layer);
            var oldLayout = ImageLayouts[subresource];

            if (oldLayout != newLayout)
            {
                ImageAspectFlags aspectMask;
                if ((Usage & TextureUsage.DEPTH_STENCIL) != 0)
                    aspectMask = FormatHelpers.IsStencilFormat(Format)
                        ? ImageAspectFlags.ImageAspectDepthBit | ImageAspectFlags.ImageAspectStencilBit
                        : ImageAspectFlags.ImageAspectDepthBit;
                else
                    aspectMask = ImageAspectFlags.ImageAspectColorBit;

                VulkanUtils.TransitionImageLayout(
                    cb,
                    Image,
                    level,
                    1,
                    layer,
                    1,
                    aspectMask,
                    oldLayout,
                    newLayout);

                ImageLayouts[subresource] = newLayout;
            }
        }
    }

    public ImageLayout GetImageLayout(uint mipLevel, uint arrayLayer)
    {
        return ImageLayouts[(int)CalculateSubresource(mipLevel, arrayLayer)];
    }

    /// <summary>
    /// Calculates the subresource index, given a mipmap level and array layer.
    /// </summary>
    /// <param name="mipLevel">The mip level. This should be less than <see cref="MipLevels"/>.</param>
    /// <param name="arrayLayer">The array layer. This should be less than <see cref="ArrayLayers"/>.</param>
    /// <returns>The subresource index.</returns>
    public uint CalculateSubresource(uint mipLevel, uint arrayLayer)
    {
        return arrayLayer * MipLevels + mipLevel;
    }

    private static ImageUsageFlags VdToVkTextureUsage(TextureUsage vdUsage)
    {
        ImageUsageFlags vkUsage = 0;

        vkUsage = ImageUsageFlags.ImageUsageTransferDstBit | ImageUsageFlags.ImageUsageTransferSrcBit;
        var isDepthStencil = (vdUsage & TextureUsage.DEPTH_STENCIL) == TextureUsage.DEPTH_STENCIL;
        if ((vdUsage & TextureUsage.SAMPLED) == TextureUsage.SAMPLED)
            vkUsage |= ImageUsageFlags.ImageUsageSampledBit;

        if (isDepthStencil) vkUsage |= ImageUsageFlags.ImageUsageDepthStencilAttachmentBit;

        if ((vdUsage & TextureUsage.RENDER_TARGET) == TextureUsage.RENDER_TARGET)
            vkUsage |= ImageUsageFlags.ImageUsageColorAttachmentBit;

        if ((vdUsage & TextureUsage.STORAGE) == TextureUsage.STORAGE)
            vkUsage |= ImageUsageFlags.ImageUsageStorageBit;

        return vkUsage;
    }

    public static void CopyTo(CommandBuffer buffer,
        (Texture Texture, uint X, uint Y, uint Z, uint MipLevel, uint BaseArrayLayer) src,
        (Texture Texture, uint X, uint Y, uint Z, uint MipLevel, uint BaseArrayLayer) dst,
        uint width, uint height, uint depth, uint layerCount)
    {
        var sourceIsStaging = (src.Texture.Usage & TextureUsage.STAGING) == TextureUsage.STAGING;
        var destIsStaging = (dst.Texture.Usage & TextureUsage.STAGING) == TextureUsage.STAGING;

        if (!sourceIsStaging && !destIsStaging)
        {
            ImageSubresourceLayers srcSubresource = new()
            {
                AspectMask = ImageAspectFlags.ImageAspectColorBit,
                LayerCount = layerCount,
                MipLevel = src.MipLevel,
                BaseArrayLayer = src.BaseArrayLayer
            };

            ImageSubresourceLayers dstSubresource = new()
            {
                AspectMask = ImageAspectFlags.ImageAspectColorBit,
                LayerCount = layerCount,
                MipLevel = dst.MipLevel,
                BaseArrayLayer = dst.BaseArrayLayer
            };

            ImageCopy region = new()
            {
                SrcOffset = new Offset3D { X = (int)src.X, Y = (int)src.Y, Z = (int)src.Z },
                DstOffset = new Offset3D { X = (int)dst.X, Y = (int)dst.Y, Z = (int)dst.Z },
                SrcSubresource = srcSubresource,
                DstSubresource = dstSubresource,
                Extent = new Extent3D { Width = width, Height = height, Depth = depth }
            };

            src.Texture.TransitionImageLayout(
                buffer,
                src.MipLevel,
                1,
                src.BaseArrayLayer,
                layerCount,
                ImageLayout.TransferSrcOptimal);

            dst.Texture.TransitionImageLayout(
                buffer,
                dst.MipLevel,
                1,
                dst.BaseArrayLayer,
                layerCount,
                ImageLayout.TransferDstOptimal);

            Vk.CmdCopyImage(
                buffer,
                src.Texture.Image,
                ImageLayout.TransferSrcOptimal,
                dst.Texture.Image,
                ImageLayout.TransferDstOptimal,
                1,
                in region);

            if ((src.Texture.Usage & TextureUsage.SAMPLED) != 0)
                src.Texture.TransitionImageLayout(
                    buffer,
                    src.MipLevel,
                    1,
                    src.BaseArrayLayer,
                    layerCount,
                    ImageLayout.ShaderReadOnlyOptimal);

            if ((dst.Texture.Usage & TextureUsage.SAMPLED) != 0)
                dst.Texture.TransitionImageLayout(
                    buffer,
                    dst.MipLevel,
                    1,
                    dst.BaseArrayLayer,
                    layerCount,
                    ImageLayout.ShaderReadOnlyOptimal);
        }
        else if (sourceIsStaging && !destIsStaging)
        {
            var srcBuffer = src.Texture.StagingBuffer;
            var srcLayout = src.Texture.GetSubresourceLayout(
                src.Texture.CalculateSubresource(src.MipLevel, src.BaseArrayLayer));
            var dstImage = dst.Texture.Image;
            dst.Texture.TransitionImageLayout(
                buffer,
                dst.MipLevel,
                1,
                dst.BaseArrayLayer,
                layerCount,
                ImageLayout.TransferDstOptimal);

            ImageSubresourceLayers dstSubresource = new()
            {
                AspectMask = ImageAspectFlags.ImageAspectColorBit,
                LayerCount = layerCount,
                MipLevel = dst.MipLevel,
                BaseArrayLayer = dst.BaseArrayLayer
            };

            GetMipDimensions(src.Texture, src.MipLevel, out var mipWidth, out var mipHeight, out var mipDepth);
            var blockSize = FormatHelpers.IsCompressedFormat(src.Texture.Format) ? 4u : 1u;
            var bufferRowLength = Math.Max(mipWidth, blockSize);
            var bufferImageHeight = Math.Max(mipHeight, blockSize);
            var compressedX = src.X / blockSize;
            var compressedY = src.Y / blockSize;
            var blockSizeInBytes = blockSize == 1
                ? FormatHelpers.GetSizeInBytes(src.Texture.Format)
                : FormatHelpers.GetBlockSizeInBytes(src.Texture.Format);
            var rowPitch = FormatHelpers.GetRowPitch(bufferRowLength, src.Texture.Format);
            var depthPitch = FormatHelpers.GetDepthPitch(rowPitch, bufferImageHeight, src.Texture.Format);

            var copyWidth = Math.Min(width, mipWidth);
            var copyheight = Math.Min(height, mipHeight);

            BufferImageCopy regions = new()
            {
                BufferOffset = srcLayout.Offset
                               + src.Z * depthPitch
                               + compressedY * rowPitch
                               + compressedX * blockSizeInBytes,
                BufferRowLength = bufferRowLength,
                BufferImageHeight = bufferImageHeight,
                ImageExtent = new Extent3D { Width = copyWidth, Height = copyheight, Depth = depth },
                ImageOffset = new Offset3D { X = (int)dst.X, Y = (int)dst.Y, Z = (int)dst.Z },
                ImageSubresource = dstSubresource
            };

            Vk.CmdCopyBufferToImage(buffer, srcBuffer, dstImage, ImageLayout.TransferDstOptimal, 1, in regions);

            if ((dst.Texture.Usage & TextureUsage.SAMPLED) != 0)
                dst.Texture.TransitionImageLayout(
                    buffer,
                    dst.MipLevel,
                    1,
                    dst.BaseArrayLayer,
                    layerCount,
                    ImageLayout.ShaderReadOnlyOptimal);
        }
        else if (!sourceIsStaging && destIsStaging)
        {
            var srcImage = src.Texture.Image;
            src.Texture.TransitionImageLayout(
                buffer,
                src.MipLevel,
                1,
                src.BaseArrayLayer,
                layerCount,
                ImageLayout.TransferSrcOptimal);

            var dstBuffer = dst.Texture.StagingBuffer;
            var dstLayout = dst.Texture.GetSubresourceLayout(
                dst.Texture.CalculateSubresource(dst.MipLevel, dst.BaseArrayLayer));

            var aspect = (src.Texture.Usage & TextureUsage.DEPTH_STENCIL) != 0
                ? ImageAspectFlags.ImageAspectDepthBit
                : ImageAspectFlags.ImageAspectColorBit;
            ImageSubresourceLayers srcSubresource = new()
            {
                AspectMask = aspect,
                LayerCount = layerCount,
                MipLevel = src.MipLevel,
                BaseArrayLayer = src.BaseArrayLayer
            };

            GetMipDimensions(dst.Texture, dst.MipLevel, out var mipWidth, out var mipHeight, out var mipDepth);
            var blockSize = FormatHelpers.IsCompressedFormat(src.Texture.Format) ? 4u : 1u;
            var bufferRowLength = Math.Max(mipWidth, blockSize);
            var bufferImageHeight = Math.Max(mipHeight, blockSize);
            var compressedDstX = dst.X / blockSize;
            var compressedDstY = dst.Y / blockSize;
            var blockSizeInBytes = blockSize == 1
                ? FormatHelpers.GetSizeInBytes(dst.Texture.Format)
                : FormatHelpers.GetBlockSizeInBytes(dst.Texture.Format);
            var rowPitch = FormatHelpers.GetRowPitch(bufferRowLength, dst.Texture.Format);
            var depthPitch = FormatHelpers.GetDepthPitch(rowPitch, bufferImageHeight, dst.Texture.Format);

            BufferImageCopy region = new()
            {
                BufferRowLength = mipWidth,
                BufferImageHeight = mipHeight,
                BufferOffset = dstLayout.Offset
                               + dst.Z * depthPitch
                               + compressedDstY * rowPitch
                               + compressedDstX * blockSizeInBytes,
                ImageExtent = new Extent3D { Width = width, Height = height, Depth = depth },
                ImageOffset = new Offset3D { X = (int)src.X, Y = (int)src.Y, Z = (int)src.Z },
                ImageSubresource = srcSubresource
            };

            Vk.CmdCopyImageToBuffer(buffer, srcImage, ImageLayout.TransferSrcOptimal, dstBuffer, 1, in region);

            if ((src.Texture.Usage & TextureUsage.SAMPLED) != 0)
                src.Texture.TransitionImageLayout(
                    buffer,
                    src.MipLevel,
                    1,
                    src.BaseArrayLayer,
                    layerCount,
                    ImageLayout.ShaderReadOnlyOptimal);
        }
        else
        {
            var srcBuffer = src.Texture.StagingBuffer;
            var srcLayout = src.Texture.GetSubresourceLayout(
                src.Texture.CalculateSubresource(src.MipLevel, src.BaseArrayLayer));
            var dstBuffer = dst.Texture.StagingBuffer;
            var dstLayout = dst.Texture.GetSubresourceLayout(
                dst.Texture.CalculateSubresource(dst.MipLevel, dst.BaseArrayLayer));

            var zLimit = Math.Max(depth, layerCount);
            if (!FormatHelpers.IsCompressedFormat(src.Texture.Format))
            {
                var pixelSize = FormatHelpers.GetSizeInBytes(src.Texture.Format);
                for (uint zz = 0; zz < zLimit; zz++)
                for (uint yy = 0; yy < height; yy++)
                {
                    BufferCopy region = new()
                    {
                        SrcOffset = srcLayout.Offset
                                    + srcLayout.DepthPitch * (zz + src.Z)
                                    + srcLayout.RowPitch * (yy + src.Y)
                                    + pixelSize * src.X,
                        DstOffset = dstLayout.Offset
                                    + dstLayout.DepthPitch * (zz + dst.Z)
                                    + dstLayout.RowPitch * (yy + dst.Y)
                                    + pixelSize * dst.X,
                        Size = width * pixelSize,
                    };

                    Vk.CmdCopyBuffer(buffer, srcBuffer, dstBuffer, 1, in region);
                }
            }
            else // IsCompressedFormat
            {
                var denseRowSize = FormatHelpers.GetRowPitch(width, src.Texture.Format);
                var numRows = FormatHelpers.GetNumRows(height, src.Texture.Format);
                var compressedSrcX = src.X / 4;
                var compressedSrcY = src.Y / 4;
                var compressedDstX = dst.X / 4;
                var compressedDstY = dst.Y / 4;
                var blockSizeInBytes = FormatHelpers.GetBlockSizeInBytes(src.Texture.Format);

                for (uint zz = 0; zz < zLimit; zz++)
                for (uint row = 0; row < numRows; row++)
                {
                    BufferCopy region = new()
                    {
                        SrcOffset = srcLayout.Offset
                                    + srcLayout.DepthPitch * (zz + src.Z)
                                    + srcLayout.RowPitch * (row + compressedSrcY)
                                    + blockSizeInBytes * compressedSrcX,
                        DstOffset = dstLayout.Offset
                                    + dstLayout.DepthPitch * (zz + dst.Z)
                                    + dstLayout.RowPitch * (row + compressedDstY)
                                    + blockSizeInBytes * compressedDstX,
                        Size = denseRowSize,
                    };

                    Vk.CmdCopyBuffer(buffer, srcBuffer, dstBuffer, 1, in region);
                }
            }
        }
    }

    public void Dispose()
    {
        var isStaging = (Usage & TextureUsage.STAGING) == TextureUsage.STAGING;
        if (isStaging)
            Vk.DestroyBuffer(VulkanEngine.Device, StagingBuffer, null);
        else
            Vk.DestroyImage(VulkanEngine.Device, Image, null);

        if (MemoryBlock.DeviceMemory.Handle != 0) MemoryManager.Free(MemoryBlock);

        while (!ImageLayouts.DecreaseRefCount())
        {
        }
    }
}

public struct TextureDescription : IEquatable<TextureDescription>
{
    /// <summary>
    /// The total width, in texels.
    /// </summary>
    public uint Width;

    /// <summary>
    /// The total height, in texels.
    /// </summary>
    public uint Height;

    /// <summary>
    /// The total depth, in texels.
    /// </summary>
    public uint Depth;

    /// <summary>
    /// The number of mipmap levels.
    /// </summary>
    public uint MipLevels;

    /// <summary>
    /// The number of array layers.
    /// </summary>
    public uint ArrayLayers;

    /// <summary>
    /// The format of individual texture elements.
    /// </summary>
    public Format Format;

    /// <summary>
    /// Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader, then
    /// <see cref="TextureUsage.SAMPLED"/> must be included. If the Texture will be used as a depth target in a
    /// <see cref="Framebuffer"/>, then <see cref="TextureUsage.DEPTH_STENCIL"/> must be included. If the Texture will be used
    /// as a color target in a <see cref="Framebuffer"/>, then <see cref="TextureUsage.RENDER_TARGET"/> must be included.
    /// If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.CUBEMAP"/> must be included.
    /// </summary>
    public TextureUsage Usage;

    /// <summary>
    /// The type of Texture to create.
    /// </summary>
    public ImageType Type;

    /// <summary>
    /// The number of samples. If equal to <see cref="SampleCountFlags.SampleCount1Bit"/>, this instance does not describe a
    /// multisample <see cref="Texture"/>.
    /// </summary>
    public SampleCountFlags SampleCount;

    /// <summary>
    /// Contsructs a new TextureDescription describing a non-multisampled <see cref="Texture"/>.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="depth">The total depth, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    /// then <see cref="TextureUsage.SAMPLED"/> must be included. If the Texture will be used as a depth target in a
    /// <see cref="Framebuffer"/>, then <see cref="TextureUsage.DEPTH_STENCIL"/> must be included. If the Texture will be used
    /// as a color target in a <see cref="Framebuffer"/>, then <see cref="TextureUsage.RENDER_TARGET"/> must be included.
    /// If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.CUBEMAP"/> must be included.</param>
    /// <param name="type">The type of Texture to create.</param>
    public TextureDescription(
        uint width,
        uint height,
        uint depth,
        uint mipLevels,
        uint arrayLayers,
        Format format,
        TextureUsage usage,
        ImageType type)
    {
        Width = width;
        Height = height;
        Depth = depth;
        MipLevels = mipLevels;
        ArrayLayers = arrayLayers;
        Format = format;
        Usage = usage;
        SampleCount = SampleCountFlags.SampleCount1Bit;
        Type = type;
    }

    /// <summary>
    /// Contsructs a new TextureDescription.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="depth">The total depth, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    /// then <see cref="TextureUsage.SAMPLED"/> must be included. If the Texture will be used as a depth target in a
    /// <see cref="Framebuffer"/>, then <see cref="TextureUsage.DEPTH_STENCIL"/> must be included. If the Texture will be used
    /// as a color target in a <see cref="Framebuffer"/>, then <see cref="TextureUsage.RENDER_TARGET"/> must be included.
    /// If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.CUBEMAP"/> must be included.</param>
    /// <param name="type">The type of Texture to create.</param>
    /// <param name="sampleCount">The number of samples. If any other value than <see cref="TextureSampleCount.Count1"/> is
    /// provided, then this describes a multisample texture.</param>
    public TextureDescription(
        uint width,
        uint height,
        uint depth,
        uint mipLevels,
        uint arrayLayers,
        Format format,
        TextureUsage usage,
        ImageType type,
        SampleCountFlags sampleCount)
    {
        Width = width;
        Height = height;
        Depth = depth;
        MipLevels = mipLevels;
        ArrayLayers = arrayLayers;
        Format = format;
        Usage = usage;
        Type = type;
        SampleCount = sampleCount;
    }

    /// <summary>
    /// Creates a description for a non-multisampled 1D Texture.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    /// then <see cref="TextureUsage.SAMPLED"/> must be included. If the Texture will be used as a depth target in a
    /// <see cref="Framebuffer"/>, then <see cref="TextureUsage.DEPTH_STENCIL"/> must be included. If the Texture will be used
    /// as a color target in a <see cref="Framebuffer"/>, then <see cref="TextureUsage.RENDER_TARGET"/> must be included.
    /// </param>
    /// <returns>A new TextureDescription for a non-multisampled 1D Texture.</returns>
    public static TextureDescription Texture1D(
        uint width,
        uint mipLevels,
        uint arrayLayers,
        Format format,
        TextureUsage usage)
    {
        return new TextureDescription(
            width,
            1,
            1,
            mipLevels,
            arrayLayers,
            format,
            usage,
            ImageType.ImageType1D,
            SampleCountFlags.SampleCount1Bit);
    }

    /// <summary>
    /// Creates a description for a non-multisampled 2D Texture.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    /// then <see cref="TextureUsage.SAMPLED"/> must be included. If the Texture will be used as a depth target in a
    /// <see cref="Framebuffer"/>, then <see cref="TextureUsage.DEPTH_STENCIL"/> must be included. If the Texture will be used
    /// as a color target in a <see cref="Framebuffer"/>, then <see cref="TextureUsage.RENDER_TARGET"/> must be included.
    /// If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.CUBEMAP"/> must be included.</param>
    /// <returns>A new TextureDescription for a non-multisampled 2D Texture.</returns>
    public static TextureDescription Texture2D(
        uint width,
        uint height,
        uint mipLevels,
        uint arrayLayers,
        Format format,
        TextureUsage usage)
    {
        return new TextureDescription(
            width,
            height,
            1,
            mipLevels,
            arrayLayers,
            format,
            usage,
            ImageType.ImageType2D,
            SampleCountFlags.SampleCount1Bit);
    }

    /// <summary>
    /// Creates a description for a 2D Texture.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    /// then <see cref="TextureUsage.SAMPLED"/> must be included. If the Texture will be used as a depth target in a
    /// <see cref="Framebuffer"/>, then <see cref="TextureUsage.DEPTH_STENCIL"/> must be included. If the Texture will be used
    /// as a color target in a <see cref="Framebuffer"/>, then <see cref="TextureUsage.RENDER_TARGET"/> must be included.
    /// If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.CUBEMAP"/> must be included.</param>
    /// <param name="sampleCount">The number of samples. If any other value than <see cref="TextureSampleCount.Count1"/> is
    /// provided, then this describes a multisample texture.</param>
    /// <returns>A new TextureDescription for a 2D Texture.</returns>
    public static TextureDescription Texture2D(
        uint width,
        uint height,
        uint mipLevels,
        uint arrayLayers,
        Format format,
        TextureUsage usage,
        SampleCountFlags sampleCount)
    {
        return new TextureDescription(
            width,
            height,
            1,
            mipLevels,
            arrayLayers,
            format,
            usage,
            ImageType.ImageType2D,
            sampleCount);
    }

    /// <summary>
    /// Creates a description for a 3D Texture.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="depth">The total depth, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    /// then <see cref="TextureUsage.SAMPLED"/> must be included. If the Texture will be used as a depth target in a
    /// <see cref="Framebuffer"/>, then <see cref="TextureUsage.DEPTH_STENCIL"/> must be included. If the Texture will be used
    /// as a color target in a <see cref="Framebuffer"/>, then <see cref="TextureUsage.RENDER_TARGET"/> must be included.</param>
    /// <returns>A new TextureDescription for a 3D Texture.</returns>
    public static TextureDescription Texture3D(
        uint width,
        uint height,
        uint depth,
        uint mipLevels,
        Format format,
        TextureUsage usage)
    {
        return new TextureDescription(
            width,
            height,
            depth,
            mipLevels,
            1,
            format,
            usage,
            ImageType.ImageType3D,
            SampleCountFlags.SampleCount1Bit);
    }

    /// <summary>
    /// Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements are equal; false otherswise.</returns>
    public bool Equals(TextureDescription other)
    {
        return Width.Equals(other.Width)
               && Height.Equals(other.Height)
               && Depth.Equals(other.Depth)
               && MipLevels.Equals(other.MipLevels)
               && ArrayLayers.Equals(other.ArrayLayers)
               && Format == other.Format
               && Usage == other.Usage
               && Type == other.Type
               && SampleCount == other.SampleCount;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(HashCode.Combine(
                Width.GetHashCode(),
                Height.GetHashCode(),
                Depth.GetHashCode(),
                MipLevels.GetHashCode(),
                ArrayLayers.GetHashCode(),
                (int)Format,
                (int)Usage,
                (int)Type),
            (int)SampleCount);
    }
}

[Flags]
public enum TextureUsage : byte
{
    /// <summary>
    /// The Texture can be used as the target of a read-only <see cref="ImageView"/>, and can be accessed from a shader.
    /// </summary>
    SAMPLED = 1 << 0,

    /// <summary>
    /// The Texture can be used as the target of a read-write <see cref="ImageView"/>, and can be accessed from a shader.
    /// </summary>
    STORAGE = 1 << 1,

    /// <summary>
    /// The Texture can be used as the color target of a <see cref="Framebuffer"/>.
    /// </summary>
    RENDER_TARGET = 1 << 2,

    /// <summary>
    /// The Texture can be used as the depth target of a <see cref="Framebuffer"/>.
    /// </summary>
    DEPTH_STENCIL = 1 << 3,

    /// <summary>
    /// The Texture is a two-dimensional cubemap.
    /// </summary>
    CUBEMAP = 1 << 4,

    /// <summary>
    /// The Texture is used as a read-write staging resource for uploading Texture data.
    /// With this flag, a Texture can be mapped using the <see cref="GraphicsDevice.Map(MappableResource, MapMode, uint)"/>
    /// method.
    /// </summary>
    STAGING = 1 << 5,

    /// <summary>
    /// The Texture supports automatic generation of mipmaps through <see cref="CommandList.GenerateMipmaps(Texture)"/>.
    /// </summary>
    GENERATE_MIPMAPS = 1 << 6,
}