using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Image = SixLabors.ImageSharp.Image;

namespace MintyCore.Render;

/// <summary>
///     Class to handle <see cref="Texture" />. Including <see cref="ImageView" />, <see cref="Sampler" /> and Texture
///     <see cref="DescriptorSet" />
/// </summary>
public static class TextureHandler
{
    private static readonly Dictionary<Identification, Texture> _textures = new();
    private static readonly Dictionary<Identification, ImageView> _textureViews = new();
    private static readonly Dictionary<Identification, Sampler> _samplers = new();
    private static readonly Dictionary<Identification, DescriptorSet> _textureBindDescriptorSets = new();

    /// <summary>
    ///     Get a Texture
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    public static Texture GetTexture(Identification textureId)
    {
        return _textures[textureId];
    }

    /// <summary>
    ///     Get a TextureView
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    public static ImageView GetTextureView(Identification textureId)
    {
        return _textureViews[textureId];
    }

    /// <summary>
    ///     Get a Sampler
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    public static Sampler GetSampler(Identification textureId)
    {
        return _samplers[textureId];
    }


    /// <summary>
    ///     Get TextureResourceSet
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static DescriptorSet GetTextureBindResourceSet(Identification texture)
    {
        return _textureBindDescriptorSets[texture];
    }

    /// <summary>
    ///     Copy a <see cref="Image{TPixel}" /> array to a <see cref="Texture" />
    /// </summary>
    /// <param name="images">Images to copy; Must be same length as mip map count</param>
    /// <param name="targetTexture">Texture to copy to</param>
    /// <param name="flipY">Whether or not to flip the y axis</param>
    /// <typeparam name="TPixel"></typeparam>
    public static unsafe void CopyImageToTexture<TPixel>(Span<Image<TPixel>> images, Texture targetTexture, bool flipY)
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
            : Texture.Create(ref textureDescription);


        var mapped = MemoryManager.Map(stagingTexture.MemoryBlock);

        for (var i = 0; i < images.Length; i++)
        {
            var currentImage = images[i];
            var layout = stagingTexture.GetSubresourceLayout((uint) i);
            var sourceBasePointer = mapped + (int) layout.Offset;
            var rowPitch = layout.RowPitch;

            currentImage.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var sourceRow = !flipY ? y : accessor.Height - y - 1;
                    var sourceSpan = accessor.GetRowSpan(sourceRow);
                    var destinationSpan = new Span<TPixel>(
                        (sourceBasePointer + (int) (rowPitch * (ulong) y)).ToPointer(),
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
                (uint) images[(int) i].Width, (uint) images[(int) i].Height, 1, 1);

        VulkanEngine.ExecuteSingleTimeCommandBuffer(buffer);

        stagingTexture.Dispose();
    }

    internal static unsafe void AddTexture(Identification textureId, bool mipMapping, IResampler resampler, bool flipY)
    {
        var image = Image.Load<Rgba32>(RegistryManager.GetResourceFileName(textureId));

        var images = mipMapping ? MipmapHelper.GenerateMipmaps(image, resampler) : new[] {image};

        var description = TextureDescription.Texture2D((uint) image.Width, (uint) image.Height,
            (uint) images.Length, 1, Format.R8G8B8A8Unorm, TextureUsage.Sampled);
        var texture = Texture.Create(ref description);

        CopyImageToTexture(images.AsSpan(), texture, flipY);

        foreach (var image1 in images) image1.Dispose();

        ImageViewCreateInfo imageViewCreateInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Format = texture.Format,
            Image = texture.Image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ImageAspectColorBit,
                LayerCount = 1,
                BaseArrayLayer = 0,
                LevelCount = texture.MipLevels,
                BaseMipLevel = 0
            },
            ViewType = ImageViewType.ImageViewType2D
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateImageView(VulkanEngine.Device, in imageViewCreateInfo,
            VulkanEngine.AllocationCallback, out var imageView));

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
            VulkanEngine.AllocationCallback, out var sampler));

        var descriptorSet = DescriptorSetHandler.AllocateDescriptorSet(DescriptorSetIDs.SampledTexture);

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


    internal static unsafe void Clear()
    {
        foreach (var textureView in _textureViews.Values)
            VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, textureView, VulkanEngine.AllocationCallback);

        foreach (var texture in _textures.Values) texture.Dispose();

        foreach (var sampler in _samplers.Values)
            VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, sampler, VulkanEngine.AllocationCallback);

        foreach (var descriptorSet in _textureBindDescriptorSets.Values)
            DescriptorSetHandler.FreeDescriptorSet(descriptorSet);

        _textureViews.Clear();
        _textures.Clear();
        _samplers.Clear();
        _textureBindDescriptorSets.Clear();
    }

    internal static unsafe void RemoveTexture(Identification objectId)
    {
        if (_textureViews.Remove(objectId, out var textureView))
            VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, textureView, VulkanEngine.AllocationCallback);

        if (_textures.Remove(objectId, out var texture)) texture.Dispose();

        if (_samplers.Remove(objectId, out var sampler))
            VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, sampler, VulkanEngine.AllocationCallback);

        if (_textureBindDescriptorSets.Remove(objectId, out var descriptorSet))
            DescriptorSetHandler.FreeDescriptorSet(descriptorSet);
    }
}