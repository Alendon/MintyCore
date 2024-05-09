using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Managers.Implementations;
using MintyCore.Graphics.Utils;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using static MintyCore.Graphics.Utils.TextureHelper;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
///     Represents a vulkan image
/// </summary>
[PublicAPI]
public unsafe class Texture : VulkanObject
{
    private Vk Vk => VulkanEngine.Vk;

    /// <summary>
    ///     The vulkan image
    /// </summary>
    public readonly Image Image;

    /// <summary>
    ///     The memory where the image data is stored
    /// </summary>
    public readonly MemoryBlock MemoryBlock;

    /// <summary>
    ///     Staging buffer for transferring data to the gpu
    /// </summary>
    public readonly Buffer StagingBuffer;

    /// <summary>
    ///     The format of the texture
    /// </summary>
    public readonly Format Format;

    /// <summary>
    ///     The width of the texture
    /// </summary>
    public readonly uint Width;

    /// <summary>
    ///     The height of the texture
    /// </summary>
    public readonly uint Height;

    /// <summary>
    ///     The depth of the texture
    /// </summary>
    public readonly uint Depth;

    /// <summary>
    ///     Number of mip levels present in the texture
    /// </summary>
    public readonly uint MipLevels;

    /// <summary>
    ///     Number of array layers present in the texture
    /// </summary>
    public readonly uint ArrayLayers;

    /// <summary>
    ///     Usage of the texture
    /// </summary>
    public readonly TextureUsage Usage;

    /// <summary>
    ///     The type of the image
    /// </summary>
    public readonly ImageType Type;

    /// <summary>
    ///     Sample count of the image
    /// </summary>
    public readonly SampleCountFlags SampleCount;

    /// <summary>
    ///     Layouts of the image
    /// </summary>
    public readonly ImageLayout[] ImageLayouts;

    public readonly ImageUsageFlags ImageUsageFlags;


    /// <summary>
    ///     Whether or not this is a swapchain texture
    /// </summary>
    public bool IsSwapchainTexture { get; }

    private IMemoryManager MemoryManager { get; }

    /// <summary/>
    public Texture(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler, IMemoryManager memoryManager,
        Image image, MemoryBlock memoryBlock, Buffer stagingBuffer, Format format, uint width, uint height,
        uint depth, uint mipLevels, uint arrayLayers, TextureUsage usage, ImageType type, SampleCountFlags sampleCount,
        ImageLayout[] imageLayouts, bool isSwapchainTexture, ImageUsageFlags imageUsageFlags) : base(vulkanEngine,
        allocationHandler)
    {
        MemoryManager = memoryManager;

        Image = image;
        MemoryBlock = memoryBlock;
        StagingBuffer = stagingBuffer;
        Format = format;
        Width = width;
        Height = height;
        Depth = depth;
        MipLevels = mipLevels;
        ArrayLayers = arrayLayers;
        Usage = usage;
        Type = type;
        SampleCount = sampleCount;
        ImageLayouts = imageLayouts;
        IsSwapchainTexture = isSwapchainTexture;
        ImageUsageFlags = imageUsageFlags;
    }

    /// <summary/>
    public Texture(IVulkanEngine vulkanEngine, IMemoryManager memoryManager,
        Image image, MemoryBlock memoryBlock, Buffer stagingBuffer, Format format, uint width, uint height,
        uint depth, uint mipLevels, uint arrayLayers, TextureUsage usage, ImageType type, SampleCountFlags sampleCount,
        ImageLayout[] imageLayouts, bool isSwapchainTexture, ImageUsageFlags imageUsageFlags) : base(vulkanEngine)
    {
        MemoryManager = memoryManager;

        Image = image;
        MemoryBlock = memoryBlock;
        StagingBuffer = stagingBuffer;
        Format = format;
        Width = width;
        Height = height;
        Depth = depth;
        MipLevels = mipLevels;
        ArrayLayers = arrayLayers;
        Usage = usage;
        Type = type;
        SampleCount = sampleCount;
        ImageLayouts = imageLayouts;
        IsSwapchainTexture = isSwapchainTexture;
        ImageUsageFlags = imageUsageFlags;
    }


    internal void ClearIfRenderTarget()
    {
        // If the image is going to be used as a render target, we need to clear the data before its first use.
        if ((Usage & TextureUsage.RenderTarget) != 0)
            VulkanEngine.ClearColorTexture(this, new ClearColorValue(0, 0, 0, 0));
        else if ((Usage & TextureUsage.DepthStencil) != 0)
            VulkanEngine.ClearDepthTexture(this, new ClearDepthStencilValue(0, 0));
    }

    internal void TransitionIfSampled()
    {
        if ((Usage & TextureUsage.Sampled) != 0)
            VulkanEngine.TransitionImageLayout(this, ImageLayout.ShaderReadOnlyOptimal);
    }

    /// <summary>
    ///     Get the layout for the given subresource
    /// </summary>
    /// <param name="subresource">Subresource to get layout from</param>
    /// <returns>Layout</returns>
    public SubresourceLayout GetSubresourceLayout(uint subresource)
    {
        var staging = StagingBuffer.Handle != 0;
        GetMipLevelAndArrayLayer(this, subresource, out var mipLevel, out var arrayLayer);
        if (!staging)
        {
            var aspect = (Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil
                ? ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit
                : ImageAspectFlags.ColorBit;
            var imageSubresource = new ImageSubresource
            {
                ArrayLayer = arrayLayer,
                MipLevel = mipLevel,
                AspectMask = aspect
            };

            Vk.GetImageSubresourceLayout(VulkanEngine.Device, Image, imageSubresource,
                out var layout);
            return layout;
        }
        else
        {
            GetMipDimensions(Width, Height, Depth, mipLevel, out var mipWidth, out var mipHeight, out _);
            var rowPitch = FormatHelpers.GetRowPitch(mipWidth, Format);
            var depthPitch = FormatHelpers.GetDepthPitch(rowPitch, mipHeight, Format);

            SubresourceLayout layout = new()
            {
                RowPitch = rowPitch,
                DepthPitch = depthPitch,
                ArrayPitch = depthPitch,
                Size = depthPitch,
                Offset = ComputeSubresourceOffset(this, mipLevel, arrayLayer)
            };

            return layout;
        }
    }

    /// <summary>
    ///     Transition the image to a new layout
    /// </summary>
    /// <param name="cb">Command buffer to issue transition</param>
    /// <param name="baseMipLevel">Starting mip level to transition image</param>
    /// <param name="levelCount">Mip level count for image transition</param>
    /// <param name="baseArrayLayer">Starting array layer to transition image</param>
    /// <param name="layerCount">Array layer count for image transition</param>
    /// <param name="newLayout">New layout for the image</param>
    /// <exception cref="MintyCoreException"></exception>
    public void TransitionImageLayout(
        ManagedCommandBuffer cb,
        uint baseMipLevel,
        uint levelCount,
        uint baseArrayLayer,
        uint layerCount,
        ImageLayout newLayout)
    {
        if (StagingBuffer.Handle != 0) return;

        var oldLayout = ImageLayouts[CalculateSubresource(baseMipLevel, baseArrayLayer)];

        for (uint level = 0; level < levelCount; level++)
        for (uint layer = 0; layer < layerCount; layer++)
            if (ImageLayouts[CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] != oldLayout)
            {
                throw new MintyCoreException("Unexpected image layout.");
            }

        if (oldLayout == newLayout) return;
        {
            ImageAspectFlags aspectMask;
            if ((Usage & TextureUsage.DepthStencil) != 0)
                aspectMask = FormatHelpers.IsStencilFormat(Format)
                    ? ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit
                    : ImageAspectFlags.DepthBit;
            else
                aspectMask = ImageAspectFlags.ColorBit;

            VulkanEngine.TransitionImageLayout(
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

    /// <summary>
    ///     Transition the image to a new layout non matching
    /// </summary>
    /// <param name="cb">Command buffer to issue transition</param>
    /// <param name="baseMipLevel">Starting mip level to transition image</param>
    /// <param name="levelCount">Mip level count for image transition</param>
    /// <param name="baseArrayLayer">Starting array layer to transition image</param>
    /// <param name="layerCount">Array layer count for image transition</param>
    /// <param name="newLayout">New layout for the image</param>
    /// <exception cref="MintyCoreException"></exception>
    public void TransitionImageLayoutNonMatching(
        ManagedCommandBuffer cb,
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

            if (oldLayout == newLayout) continue;
            ImageAspectFlags aspectMask;
            if ((Usage & TextureUsage.DepthStencil) != 0)
                aspectMask = FormatHelpers.IsStencilFormat(Format)
                    ? ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit
                    : ImageAspectFlags.DepthBit;
            else
                aspectMask = ImageAspectFlags.ColorBit;

            VulkanEngine.TransitionImageLayout(
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

    /// <summary>
    ///     Get the layout of the image
    /// </summary>
    /// <param name="mipLevel">Mip level of the layout</param>
    /// <param name="arrayLayer">Array level of the layout</param>
    /// <returns></returns>
    public ImageLayout GetImageLayout(uint mipLevel, uint arrayLayer)
    {
        return ImageLayouts[(int)CalculateSubresource(mipLevel, arrayLayer)];
    }

    /// <summary>
    ///     Calculates the subresource index, given a mipmap level and array layer.
    /// </summary>
    /// <param name="mipLevel">The mip level. This should be less than <see cref="MipLevels" />.</param>
    /// <param name="arrayLayer">The array layer. This should be less than <see cref="ArrayLayers" />.</param>
    /// <returns>The subresource index.</returns>
    public uint CalculateSubresource(uint mipLevel, uint arrayLayer)
    {
        return arrayLayer * MipLevels + mipLevel;
    }


    /// <summary>
    ///     Copy a texture on to another
    /// </summary>
    /// <param name="buffer">Command buffer to issue copy command</param>
    /// <param name="src">Information of the texture source</param>
    /// <param name="dst">Information of the texture destination</param>
    /// <param name="width">The width to copy</param>
    /// <param name="height">The height to copy</param>
    /// <param name="depth">The depth to copy</param>
    /// <param name="layerCount">The number of layers to copy</param>
    public static void CopyTo(ManagedCommandBuffer buffer,
        (Texture Texture, uint X, uint Y, uint Z, uint MipLevel, uint BaseArrayLayer) src,
        (Texture Texture, uint X, uint Y, uint Z, uint MipLevel, uint BaseArrayLayer) dst,
        uint width, uint height, uint depth, uint layerCount)
    {
        var vk = src.Texture.Vk;

        var sourceIsStaging = (src.Texture.Usage & TextureUsage.Staging) == TextureUsage.Staging;
        var destIsStaging = (dst.Texture.Usage & TextureUsage.Staging) == TextureUsage.Staging;

        switch (sourceIsStaging)
        {
            case false when !destIsStaging:
            {
                ImageSubresourceLayers srcSubresource = new()
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    LayerCount = layerCount,
                    MipLevel = src.MipLevel,
                    BaseArrayLayer = src.BaseArrayLayer
                };

                ImageSubresourceLayers dstSubresource = new()
                {
                    AspectMask = ImageAspectFlags.ColorBit,
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

                vk.CmdCopyImage(
                    buffer.InternalCommandBuffer,
                    src.Texture.Image,
                    ImageLayout.TransferSrcOptimal,
                    dst.Texture.Image,
                    ImageLayout.TransferDstOptimal,
                    1,
                    in region);

                if ((src.Texture.Usage & TextureUsage.Sampled) != 0)
                    src.Texture.TransitionImageLayout(
                        buffer,
                        src.MipLevel,
                        1,
                        src.BaseArrayLayer,
                        layerCount,
                        ImageLayout.ShaderReadOnlyOptimal);

                if ((dst.Texture.Usage & TextureUsage.Sampled) != 0)
                    dst.Texture.TransitionImageLayout(
                        buffer,
                        dst.MipLevel,
                        1,
                        dst.BaseArrayLayer,
                        layerCount,
                        ImageLayout.ShaderReadOnlyOptimal);
                break;
            }
            case true when !destIsStaging:
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
                    AspectMask = ImageAspectFlags.ColorBit,
                    LayerCount = layerCount,
                    MipLevel = dst.MipLevel,
                    BaseArrayLayer = dst.BaseArrayLayer
                };

                GetMipDimensions(src.Texture.Width, src.Texture.Height, src.Texture.Depth, src.MipLevel,
                    out var mipWidth,
                    out var mipHeight, out _);
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
                var copyHeight = Math.Min(height, mipHeight);

                BufferImageCopy regions = new()
                {
                    BufferOffset = srcLayout.Offset
                                   + src.Z * depthPitch
                                   + compressedY * rowPitch
                                   + compressedX * blockSizeInBytes,
                    BufferRowLength = bufferRowLength,
                    BufferImageHeight = bufferImageHeight,
                    ImageExtent = new Extent3D { Width = copyWidth, Height = copyHeight, Depth = depth },
                    ImageOffset = new Offset3D { X = (int)dst.X, Y = (int)dst.Y, Z = (int)dst.Z },
                    ImageSubresource = dstSubresource
                };

                vk.CmdCopyBufferToImage(buffer.InternalCommandBuffer, srcBuffer, dstImage,
                    ImageLayout.TransferDstOptimal, 1, in regions);

                if ((dst.Texture.Usage & TextureUsage.Sampled) != 0)
                    dst.Texture.TransitionImageLayout(
                        buffer,
                        dst.MipLevel,
                        1,
                        dst.BaseArrayLayer,
                        layerCount,
                        ImageLayout.ShaderReadOnlyOptimal);
                break;
            }
            case false:
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

                var aspect = (src.Texture.Usage & TextureUsage.DepthStencil) != 0
                    ? ImageAspectFlags.DepthBit
                    : ImageAspectFlags.ColorBit;
                ImageSubresourceLayers srcSubresource = new()
                {
                    AspectMask = aspect,
                    LayerCount = layerCount,
                    MipLevel = src.MipLevel,
                    BaseArrayLayer = src.BaseArrayLayer
                };

                GetMipDimensions(dst.Texture.Width, dst.Texture.Height, dst.Texture.Depth, dst.MipLevel,
                    out var mipWidth,
                    out var mipHeight, out _);
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

                vk.CmdCopyImageToBuffer(buffer.InternalCommandBuffer, srcImage, ImageLayout.TransferSrcOptimal,
                    dstBuffer, 1, in region);

                if ((src.Texture.Usage & TextureUsage.Sampled) != 0)
                    src.Texture.TransitionImageLayout(
                        buffer,
                        src.MipLevel,
                        1,
                        src.BaseArrayLayer,
                        layerCount,
                        ImageLayout.ShaderReadOnlyOptimal);
                break;
            }
            default:
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
                            Size = width * pixelSize
                        };

                        vk.CmdCopyBuffer(buffer.InternalCommandBuffer, srcBuffer, dstBuffer, 1, in region);
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
                            Size = denseRowSize
                        };

                        vk.CmdCopyBuffer(buffer.InternalCommandBuffer, srcBuffer, dstBuffer, 1, in region);
                    }
                }

                break;
            }
        }
    }


    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources()
    {
        //The textures of the swapchain is externally managed
        if (IsSwapchainTexture) return;

        var isStaging = (Usage & TextureUsage.Staging) == TextureUsage.Staging;
        if (isStaging)
            Vk.DestroyBuffer(VulkanEngine.Device, StagingBuffer, null);
        else
            Vk.DestroyImage(VulkanEngine.Device, Image, null);

        if (MemoryBlock.DeviceMemory.Handle != 0) MemoryManager.Free(MemoryBlock);
    }

    public bool HasUniformImageLayout()
    {
        if (ImageLayouts.Length == 0) return true;

        var layout = ImageLayouts[0];
        return ImageLayouts.All(x => x == layout);
    }
}

/// <summary>
///     Description to create a texture
/// </summary>
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public struct TextureDescription : IEquatable<TextureDescription>
{
    /// <summary>
    ///     The total width, in texels.
    /// </summary>
    public uint Width;

    /// <summary>
    ///     The total height, in texels.
    /// </summary>
    public uint Height;

    /// <summary>
    ///     The total depth, in texels.
    /// </summary>
    public uint Depth;

    /// <summary>
    ///     The number of mipmap levels.
    /// </summary>
    public uint MipLevels;

    /// <summary>
    ///     The number of array layers.
    /// </summary>
    public uint ArrayLayers;

    /// <summary>
    ///     The format of individual texture elements.
    /// </summary>
    public Format Format;

    /// <summary>
    ///     Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader, then
    ///     <see cref="TextureUsage.Sampled" /> must be included. If the Texture will be used as a depth target in a
    ///     <see cref="Framebuffer" />, then <see cref="TextureUsage.DepthStencil" /> must be included. If the Texture will be
    ///     used
    ///     as a color target in a <see cref="Framebuffer" />, then <see cref="TextureUsage.RenderTarget" /> must be included.
    ///     If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.Cubemap" /> must be included.
    /// </summary>
    public TextureUsage Usage;

    /// <summary>
    /// Optional usage flags
    /// Some cases are already handled through <see cref="Usage"/>
    /// </summary>
    public ImageUsageFlags AdditionalUsageFlags;

    /// <summary>
    ///     The type of Texture to create.
    /// </summary>
    public ImageType Type;

    /// <summary>
    ///     The number of samples. If equal to <see cref="SampleCountFlags.Count1Bit" />, this instance does not describe
    ///     a
    ///     multisample <see cref="Texture" />.
    /// </summary>
    public SampleCountFlags SampleCount;

    /// <summary>
    ///     Constructs a new TextureDescription describing a non-multisampled <see cref="Texture" />.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="depth">The total depth, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">
    ///     Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    ///     then <see cref="TextureUsage.Sampled" /> must be included. If the Texture will be used as a depth target in a
    ///     <see cref="Framebuffer" />, then <see cref="TextureUsage.DepthStencil" /> must be included. If the Texture will be
    ///     used
    ///     as a color target in a <see cref="Framebuffer" />, then <see cref="TextureUsage.RenderTarget" /> must be included.
    ///     If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.Cubemap" /> must be included.
    /// </param>
    /// <param name="type">The type of Texture to create.</param>
    /// <param name="optionalUsageFlags"></param>
    public TextureDescription(
        uint width,
        uint height,
        uint depth,
        uint mipLevels,
        uint arrayLayers,
        Format format,
        TextureUsage usage,
        ImageType type,
        ImageUsageFlags optionalUsageFlags = 0)
    {
        Width = width;
        Height = height;
        Depth = depth;
        MipLevels = mipLevels;
        ArrayLayers = arrayLayers;
        Format = format;
        Usage = usage;
        SampleCount = SampleCountFlags.Count1Bit;
        Type = type;
        AdditionalUsageFlags = optionalUsageFlags;
    }

    /// <summary>
    ///     Constructs a new TextureDescription.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="depth">The total depth, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">
    ///     Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    ///     then <see cref="TextureUsage.Sampled" /> must be included. If the Texture will be used as a depth target in a
    ///     <see cref="Framebuffer" />, then <see cref="TextureUsage.DepthStencil" /> must be included. If the Texture will be
    ///     used
    ///     as a color target in a <see cref="Framebuffer" />, then <see cref="TextureUsage.RenderTarget" /> must be included.
    ///     If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.Cubemap" /> must be included.
    /// </param>
    /// <param name="type">The type of Texture to create.</param>
    /// <param name="sampleCount">
    ///     The number of samples. If any other value than <see cref="SampleCountFlags.Count1Bit" /> is
    ///     provided, then this describes a multisample texture.
    /// </param>
    /// <param name="optionalUsageFlags"></param>
    public TextureDescription(
        uint width,
        uint height,
        uint depth,
        uint mipLevels,
        uint arrayLayers,
        Format format,
        TextureUsage usage,
        ImageType type,
        SampleCountFlags sampleCount,
        ImageUsageFlags optionalUsageFlags = 0)
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
        AdditionalUsageFlags = optionalUsageFlags;
    }

    /// <summary>
    ///     Creates a description for a non-multisampled 1D Texture.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">
    ///     Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    ///     then <see cref="TextureUsage.Sampled" /> must be included. If the Texture will be used as a depth target in a
    ///     <see cref="Framebuffer" />, then <see cref="TextureUsage.DepthStencil" /> must be included. If the Texture will be
    ///     used
    ///     as a color target in a <see cref="Framebuffer" />, then <see cref="TextureUsage.RenderTarget" /> must be included.
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
            ImageType.Type1D,
            SampleCountFlags.Count1Bit);
    }

    /// <summary>
    ///     Creates a description for a non-multisampled 2D Texture.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">
    ///     Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    ///     then <see cref="TextureUsage.Sampled" /> must be included. If the Texture will be used as a depth target in a
    ///     <see cref="Framebuffer" />, then <see cref="TextureUsage.DepthStencil" /> must be included. If the Texture will be
    ///     used
    ///     as a color target in a <see cref="Framebuffer" />, then <see cref="TextureUsage.RenderTarget" /> must be included.
    ///     If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.Cubemap" /> must be included.
    /// </param>
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
            ImageType.Type2D,
            SampleCountFlags.Count1Bit);
    }

    /// <summary>
    ///     Creates a description for a 2D Texture.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="arrayLayers">The number of array layers.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">
    ///     Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    ///     then <see cref="TextureUsage.Sampled" /> must be included. If the Texture will be used as a depth target in a
    ///     <see cref="Framebuffer" />, then <see cref="TextureUsage.DepthStencil" /> must be included. If the Texture will be
    ///     used
    ///     as a color target in a <see cref="Framebuffer" />, then <see cref="TextureUsage.RenderTarget" /> must be included.
    ///     If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.Cubemap" /> must be included.
    /// </param>
    /// <param name="sampleCount">
    ///     The number of samples. If any other value than <see cref="SampleCountFlags.Count1Bit" /> is
    ///     provided, then this describes a multisample texture.
    /// </param>
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
            ImageType.Type2D,
            sampleCount);
    }

    /// <summary>
    ///     Creates a description for a 3D Texture.
    /// </summary>
    /// <param name="width">The total width, in texels.</param>
    /// <param name="height">The total height, in texels.</param>
    /// <param name="depth">The total depth, in texels.</param>
    /// <param name="mipLevels">The number of mipmap levels.</param>
    /// <param name="format">The format of individual texture elements.</param>
    /// <param name="usage">
    ///     Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader,
    ///     then <see cref="TextureUsage.Sampled" /> must be included. If the Texture will be used as a depth target in a
    ///     <see cref="Framebuffer" />, then <see cref="TextureUsage.DepthStencil" /> must be included. If the Texture will be
    ///     used
    ///     as a color target in a <see cref="Framebuffer" />, then <see cref="TextureUsage.RenderTarget" /> must be included.
    /// </param>
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
            ImageType.Type3D,
            SampleCountFlags.Count1Bit);
    }

    /// <summary>
    ///     Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements are equal; false otherwise.</returns>
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
    ///     Returns the hash code for this instance.
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