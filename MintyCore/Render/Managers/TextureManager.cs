using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.Utils;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using SixLaborsImage = SixLabors.ImageSharp.Image;
using static MintyCore.Render.Utils.TextureHelper;

namespace MintyCore.Render.Managers;

/// <summary>
///     Class to handle <see cref="Texture" />. Including <see cref="ImageView" />, <see cref="Sampler" /> and Texture
///     <see cref="DescriptorSet" />
/// </summary>
public class TextureManager : ITextureManager
{
    private readonly Dictionary<Identification, Texture> _textures = new();
    private readonly Dictionary<Identification, ImageView> _textureViews = new();
    private readonly Dictionary<Identification, Sampler> _samplers = new();
    private readonly Dictionary<Identification, DescriptorSet> _textureBindDescriptorSets = new();

    public required IModManager ModManager { init; private get; }
    public required IDescriptorSetManager DescriptorSetManager { init; private get; }
    public required IVulkanEngine VulkanEngine { set; private get; }
    public required IMemoryManager MemoryManager { init; private get; }
    public required IAllocationTracker AllocationTracker { init; private get; }
    
    private Vk Vk => VulkanEngine.Vk;

    /// <summary>
    ///     Get a Texture
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    public Texture GetTexture(Identification textureId)
    {
        return _textures[textureId];
    }

    /// <summary>
    ///     Get a TextureView
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    public ImageView GetTextureView(Identification textureId)
    {
        return _textureViews[textureId];
    }

    /// <summary>
    ///     Get a Sampler
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    public Sampler GetSampler(Identification textureId)
    {
        return _samplers[textureId];
    }


    /// <summary>
    ///     Get TextureResourceSet
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    public DescriptorSet GetTextureBindResourceSet(Identification texture)
    {
        return _textureBindDescriptorSets[texture];
    }

    /// <summary>
    ///     Create a new texture
    /// </summary>
    /// <param name="description">Description of texture</param>
    /// <returns>Created texture</returns>
    public unsafe Texture Create(ref TextureDescription description)
    {
        var width = description.Width;
        var height = description.Height;
        var depth = description.Depth;
        var mipLevels = description.MipLevels;
        var arrayLayers = description.ArrayLayers;
        var isCubemap = (description.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap;
        var actualImageArrayLayers = isCubemap
            ? 6 * arrayLayers
            : arrayLayers;
        var format = description.Format;
        var usage = description.Usage;
        var type = description.Type;
        var sampleCount = description.SampleCount;

        var isStaging = (usage & TextureUsage.Staging) == TextureUsage.Staging;

        Image image = default;
        var imageLayouts = Array.Empty<ImageLayout>();
        MemoryBlock memoryBlock;
        Buffer stagingBuffer = default;

        if (!isStaging)
        {
            ImageCreateInfo imageCi = new()
            {
                SType = StructureType.ImageCreateInfo,
                MipLevels = mipLevels,
                ArrayLayers = actualImageArrayLayers,
                ImageType = type,
                Extent =
                {
                    Width = width,
                    Height = height,
                    Depth = depth
                },
                InitialLayout = ImageLayout.Preinitialized,
                Usage = VdToVkTextureUsage(usage) | description.AdditionalUsageFlags,
                Tiling = ImageTiling.Optimal,
                Format = format,
                Flags = ImageCreateFlags.CreateMutableFormatBit,
                Samples = sampleCount
            };

            if (isCubemap) imageCi.Flags |= ImageCreateFlags.CreateCubeCompatibleBit;

            var subresourceCount = mipLevels * actualImageArrayLayers * depth;
            VulkanUtils.Assert(Vk.CreateImage(VulkanEngine.Device, imageCi, null, out image));

            Vk.GetImageMemoryRequirements(VulkanEngine.Device, image, out var memReqs2);

            var memoryToken = MemoryManager.Allocate(
                memReqs2.MemoryTypeBits,
                MemoryPropertyFlags.DeviceLocalBit,
                false,
                memReqs2.Size,
                memReqs2.Alignment,
                true,
                image);
            memoryBlock = memoryToken;
            VulkanUtils.Assert(Vk.BindImageMemory(VulkanEngine.Device, image, memoryBlock.DeviceMemory, memoryBlock.Offset));

            imageLayouts = new ImageLayout[(int)subresourceCount];
            for (var i = 0; i < imageLayouts.Length; i++) imageLayouts[i] = ImageLayout.Preinitialized;
        }
        else // isStaging
        {
            var depthPitch = FormatHelpers.GetDepthPitch(
                FormatHelpers.GetRowPitch(width, format),
                height,
                format);
            var stagingSize = depthPitch * depth;
            for (uint level = 1; level < mipLevels; level++)
            {
                GetMipDimensions(width, height, depth, level, out var mipWidth, out var mipHeight, out var mipDepth);

                depthPitch = FormatHelpers.GetDepthPitch(
                    FormatHelpers.GetRowPitch(mipWidth, format),
                    mipHeight,
                    format);

                stagingSize += depthPitch * mipDepth;
            }

            stagingSize *= arrayLayers;

            BufferCreateInfo bufferCi = new()
            {
                SType = StructureType.BufferCreateInfo,
                Usage = BufferUsageFlags.TransferSrcBit |
                        BufferUsageFlags.TransferDstBit,
                Size = stagingSize
            };
            VulkanUtils.Assert(Vk.CreateBuffer(VulkanEngine.Device, bufferCi, null,
                out stagingBuffer));

            Vk.GetBufferMemoryRequirements(VulkanEngine.Device, stagingBuffer, out var memReqs);

            // Use "host cached" memory when available, for better performance of GPU -> CPU transfers
            var propertyFlags = MemoryPropertyFlags.HostVisibleBit |
                                MemoryPropertyFlags.HostCoherentBit |
                                MemoryPropertyFlags.HostCachedBit;
            if (!VulkanEngine.FindMemoryType(memReqs.MemoryTypeBits, propertyFlags, out _))
                propertyFlags ^= MemoryPropertyFlags.HostCachedBit;

            memoryBlock = MemoryManager.Allocate(
                memReqs.MemoryTypeBits,
                propertyFlags,
                true,
                memReqs.Size,
                memReqs.Alignment,
                true,
                default,
                stagingBuffer);

            VulkanUtils.Assert(Vk.BindBufferMemory(VulkanEngine.Device, stagingBuffer, memoryBlock.DeviceMemory,
                memoryBlock.Offset));
        }

        var texture = new Texture(VulkanEngine, AllocationTracker, MemoryManager,
            image, memoryBlock, stagingBuffer, format, width, height, depth, mipLevels,
            arrayLayers, usage, type, sampleCount, imageLayouts, 0);

        texture.ClearIfRenderTarget();
        texture.TransitionIfSampled();
        return texture;
    }

    private static ImageUsageFlags VdToVkTextureUsage(TextureUsage vdUsage)
    {
        var vkUsage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit;
        var isDepthStencil = (vdUsage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;
        if ((vdUsage & TextureUsage.Sampled) == TextureUsage.Sampled)
            vkUsage |= ImageUsageFlags.SampledBit;

        if (isDepthStencil) vkUsage |= ImageUsageFlags.DepthStencilAttachmentBit;

        if ((vdUsage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget)
            vkUsage |= ImageUsageFlags.ColorAttachmentBit;

        if ((vdUsage & TextureUsage.Storage) == TextureUsage.Storage)
            vkUsage |= ImageUsageFlags.StorageBit;

        return vkUsage;
    }

    /// <summary>
    ///     Copy a <see cref="Image{TPixel}" /> array to a <see cref="Texture" />
    /// </summary>
    /// <param name="images">Images to copy; Must be same length as mip map count</param>
    /// <param name="targetTexture">Texture to copy to</param>
    /// <param name="flipY">Whether or not to flip the y axis</param>
    /// <typeparam name="TPixel"></typeparam>
    public unsafe void CopyImageToTexture<TPixel>(Span<Image<TPixel>> images, Texture targetTexture, bool flipY)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        Logger.AssertAndThrow(images.Length == targetTexture.MipLevels, "Image layout doesn't match (mip level count)",
            "Render");
        Logger.AssertAndThrow(images[0].Width == targetTexture.Width && images[0].Height == targetTexture.Height,
            "Image layout doesn't match (size)", "Render");

        var textureDescription = TextureDescription.Texture2D(targetTexture.Width, targetTexture.Height,
            targetTexture.MipLevels, targetTexture.ArrayLayers, targetTexture.Format, TextureUsage.Staging);

        var stagingTexture = targetTexture.Usage == TextureUsage.Staging
            ? targetTexture
            : Create(ref textureDescription);


        var mapped = MemoryManager.Map(stagingTexture.MemoryBlock);

        for (var i = 0; i < images.Length; i++)
        {
            var currentImage = images[i];
            var layout = stagingTexture.GetSubresourceLayout((uint)i);
            var sourceBasePointer = mapped + (int)layout.Offset;
            var rowPitch = layout.RowPitch;

            currentImage.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var sourceRow = !flipY ? y : accessor.Height - y - 1;
                    var sourceSpan = accessor.GetRowSpan(sourceRow);
                    var destinationSpan = new Span<TPixel>(
                        (sourceBasePointer + (int)(rowPitch * (ulong)y)).ToPointer(),
                        accessor.Width);
                    sourceSpan.CopyTo(destinationSpan);
                }
            });
        }

        MemoryManager.UnMap(stagingTexture.MemoryBlock);

        if (targetTexture.Usage == TextureUsage.Staging) return;

        var buffer = VulkanEngine.GetSingleTimeCommandBuffer();
        for (uint i = 0; i < images.Length; i++)
            Texture.CopyTo(buffer, (stagingTexture, 0, 0, 0, i, 0), (targetTexture, 0, 0, 0, i, 0),
                (uint)images[(int)i].Width, (uint)images[(int)i].Height, 1, 1);

        VulkanEngine.ExecuteSingleTimeCommandBuffer(buffer);

        stagingTexture.Dispose();
    }

    public unsafe void AddTexture(Identification textureId, bool mipMapping, IResampler resampler, bool flipY)
    {
        var image = SixLaborsImage.Load<Rgba32>(ModManager.GetResourceFileStream(textureId));

        var images = mipMapping ? TextureHelper.GenerateMipmaps(image, resampler) : new[] { image };

        var description = TextureDescription.Texture2D((uint)image.Width, (uint)image.Height,
            (uint)images.Length, 1, Format.R8G8B8A8Unorm, TextureUsage.Sampled);
        var texture = Create(ref description);

        CopyImageToTexture(images.AsSpan(), texture, flipY);

        foreach (var image1 in images) image1.Dispose();

        ImageViewCreateInfo imageViewCreateInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Format = texture.Format,
            Image = texture.Image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LayerCount = 1,
                BaseArrayLayer = 0,
                LevelCount = texture.MipLevels,
                BaseMipLevel = 0
            },
            ViewType = ImageViewType.Type2D
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateImageView(VulkanEngine.Device, in imageViewCreateInfo,
            null, out var imageView));

        SamplerCreateInfo samplerCreateInfo = new()
        {
            AnisotropyEnable = Vk.True,
            MaxAnisotropy = 4,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            BorderColor = BorderColor.FloatOpaqueBlack,
            SType = StructureType.SamplerCreateInfo,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            CompareOp = CompareOp.Never,
            CompareEnable = Vk.True,
            MinLod = 0,
            MaxLod = texture.MipLevels
        };
        VulkanUtils.Assert(VulkanEngine.Vk.CreateSampler(VulkanEngine.Device, in samplerCreateInfo,
            null, out var sampler));

        var descriptorSet = DescriptorSetManager.AllocateDescriptorSet(DescriptorSetIDs.SampledTexture);

        DescriptorImageInfo descriptorImageInfo = new()
        {
            Sampler = sampler,
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = imageView
        };

        WriteDescriptorSet writeDescriptorSet = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DstBinding = 0,
            DstSet = descriptorSet,
            PImageInfo = &descriptorImageInfo
        };

        VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, in writeDescriptorSet, 0, null);

        _textures.Add(textureId, texture);
        _textureViews.Add(textureId, imageView);
        _samplers.Add(textureId, sampler);
        _textureBindDescriptorSets.Add(textureId, descriptorSet);
    }


    public unsafe void Clear()
    {
        foreach (var textureView in _textureViews.Values)
            VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, textureView, null);

        foreach (var texture in _textures.Values) texture.Dispose();

        foreach (var sampler in _samplers.Values)
            VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, sampler, null);

        foreach (var descriptorSet in _textureBindDescriptorSets.Values)
            DescriptorSetManager.FreeDescriptorSet(descriptorSet);

        _textureViews.Clear();
        _textures.Clear();
        _samplers.Clear();
        _textureBindDescriptorSets.Clear();
    }

    public unsafe void RemoveTexture(Identification objectId)
    {
        if (_textureViews.Remove(objectId, out var textureView))
            VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, textureView, null);

        if (_textures.Remove(objectId, out var texture)) texture.Dispose();

        if (_samplers.Remove(objectId, out var sampler))
            VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, sampler, null);

        if (_textureBindDescriptorSets.Remove(objectId, out var descriptorSet))
            DescriptorSetManager.FreeDescriptorSet(descriptorSet);
    }
}