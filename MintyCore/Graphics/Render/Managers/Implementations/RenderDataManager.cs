using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IRenderDataManager>(SingletonContextFlags.NoHeadless)]
internal class RenderDataManager : IRenderDataManager
{
    public required ITextureManager TextureManager { private get; [UsedImplicitly] init; }
    public required IVulkanEngine VulkanEngine { private get; [UsedImplicitly] init; }
    public required IDescriptorSetManager DescriptorSetManager { private get; [UsedImplicitly] init; }

    private Dictionary<Identification, RenderTextureDescription> _renderTextureDescriptions = new();
    private Dictionary<Identification, Texture?[]> _renderTextures = new();

    private Dictionary<Identification, ImageView[]> _renderImageViews = new();
    private Dictionary<Identification, DescriptorSet[]> _sampledTextureDescriptorSets = new();
    private Dictionary<Identification, DescriptorSet[]> _storageTextureDescriptorSets = new();

    private Sampler _sampler;

    public void RegisterRenderTexture(Identification id, RenderTextureDescription textureData)
    {
        if ((textureData.usage & TextureUsage.DepthStencil) != 0 && textureData.usage != TextureUsage.DepthStencil)
            throw new InvalidOperationException("DepthStencil usage must be used exclusively");

        if ((textureData.usage & TextureUsage.Staging) != 0)
            throw new InvalidOperationException("Staging usage is not allowed for render textures");

        if ((textureData.usage & TextureUsage.Cubemap) != 0)
            throw new InvalidOperationException("Cubemap usage is not allowed for render textures");

        _renderTextureDescriptions.Add(id, textureData);
        _renderTextures.Add(id, new Texture?[VulkanEngine.SwapchainImageCount]);

        _renderImageViews.Add(id, new ImageView[VulkanEngine.SwapchainImageCount]);
        _sampledTextureDescriptorSets.Add(id, new DescriptorSet[VulkanEngine.SwapchainImageCount]);
        _storageTextureDescriptorSets.Add(id, new DescriptorSet[VulkanEngine.SwapchainImageCount]);
    }

    public RenderTextureDescription GetRenderTextureDescription(Identification id)
    {
        return _renderTextureDescriptions[id];
    }

    public Texture GetRenderTexture(Identification id)
    {
        CheckTextureSize(id);

        return _renderTextures[id][VulkanEngine.ImageIndex]!;
    }

    public ClearColorValue? GetClearColorValue(Identification id)
    {
        return _renderTextureDescriptions[id].clearColorValue;
    }

    public unsafe ImageView GetRenderImageView(Identification id)
    {
        CheckTextureSize(id);

        var imageView = _renderImageViews[id][VulkanEngine.ImageIndex];
        if (imageView.Handle != 0)
            return imageView;

        var textureDescription = _renderTextureDescriptions[id];
        var format = textureDescription.format.Match(
            format => format,
            _ => VulkanEngine.SwapchainImageFormat
        );

        var texture = _renderTextures[id][VulkanEngine.ImageIndex];

        var createInfo = new ImageViewCreateInfo()
        {
            SType = StructureType.ImageViewCreateInfo,
            Format = format,
            Components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B,
                ComponentSwizzle.A),
            SubresourceRange =
            {
                AspectMask = textureDescription.usage == TextureUsage.DepthStencil
                    ? ImageAspectFlags.DepthBit
                    : ImageAspectFlags.ColorBit,
                LayerCount = 1,
                BaseArrayLayer = 0,
                LevelCount = 1,
                BaseMipLevel = 0
            },
            ViewType = ImageViewType.Type2D,
            Image = texture!.Image
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateImageView(VulkanEngine.Device, createInfo, null, out imageView));

        _renderImageViews[id][VulkanEngine.ImageIndex] = imageView;

        return imageView;
    }

    public unsafe DescriptorSet GetSampledTextureDescriptorSet(Identification id)
    {
        CheckTextureSize(id);
        CreateSampler();

        var descriptorSet = _sampledTextureDescriptorSets[id][VulkanEngine.ImageIndex];
        if (descriptorSet.Handle != 0)
            return descriptorSet;

        descriptorSet = DescriptorSetManager.AllocateDescriptorSet(DescriptorSetIDs.SampledRenderTexture);
        var imageInfo = new DescriptorImageInfo
        {
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = GetRenderImageView(id),
            Sampler = _sampler
        };

        var writeDescriptorSet = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.SampledImage,
            DstBinding = 0,
            DstSet = descriptorSet,
            PImageInfo = &imageInfo
        };

        VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, &writeDescriptorSet, 0, null);
        _sampledTextureDescriptorSets[id][VulkanEngine.ImageIndex] = descriptorSet;

        return descriptorSet;
    }

    private unsafe void CreateSampler()
    {
        if (_sampler.Handle != 0)
            return;

        var samplerCreateInfo = new SamplerCreateInfo()
        {
            SType = StructureType.SamplerCreateInfo,
            AddressModeU = SamplerAddressMode.ClampToBorder,
            AddressModeV = SamplerAddressMode.ClampToBorder,
            AddressModeW = SamplerAddressMode.ClampToBorder,
            MinLod = 0,
            MaxLod = 1,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateSampler(VulkanEngine.Device, samplerCreateInfo, null, out _sampler));
    }

    public unsafe DescriptorSet GetStorageTextureDescriptorSet(Identification id)
    {
        CheckTextureSize(id);

        var descriptorSet = _storageTextureDescriptorSets[id][VulkanEngine.ImageIndex];
        if (descriptorSet.Handle != 0)
            return descriptorSet;

        descriptorSet = DescriptorSetManager.AllocateDescriptorSet(DescriptorSetIDs.StorageRenderTexture);
        var imageInfo = new DescriptorImageInfo
        {
            ImageLayout = ImageLayout.General,
            ImageView = GetRenderImageView(id)
        };

        var writeDescriptorSet = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageImage,
            DstBinding = 0,
            DstSet = descriptorSet,
            PImageInfo = &imageInfo
        };

        VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, &writeDescriptorSet, 0, null);
        _storageTextureDescriptorSets[id][VulkanEngine.ImageIndex] = descriptorSet;

        return descriptorSet;
    }

    private void CheckTextureSize(Identification id)
    {
        if (!_renderTextures.TryGetValue(id, out var textures))
        {
            throw new InvalidOperationException($"Render texture with id {id} has not been registered");
        }

        var currentFrame = VulkanEngine.ImageIndex;
        var currentTexture = textures[currentFrame];

        var currentSize = _renderTextureDescriptions[id].dimensions.Match(
            extentFunc => extentFunc(),
            _ => VulkanEngine.SwapchainExtent
        );

        if (currentTexture is not null && currentTexture.Width == currentSize.Width &&
            currentTexture.Height == currentSize.Height) return;


        DestroyCurrentTexture(id);
        CreateTexture(id);
    }

    private unsafe void DestroyCurrentTexture(Identification id)
    {
        var sampleDescriptor = _sampledTextureDescriptorSets[id][VulkanEngine.ImageIndex];
        var storageDescriptor = _storageTextureDescriptorSets[id][VulkanEngine.ImageIndex];

        if (sampleDescriptor.Handle != 0)
            DescriptorSetManager.FreeDescriptorSet(sampleDescriptor);
        if (storageDescriptor.Handle != 0)
            DescriptorSetManager.FreeDescriptorSet(storageDescriptor);

        _sampledTextureDescriptorSets[id][VulkanEngine.ImageIndex] = default;
        _storageTextureDescriptorSets[id][VulkanEngine.ImageIndex] = default;

        var imageView = _renderImageViews[id][VulkanEngine.ImageIndex];
        VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, imageView, null);
        _renderImageViews[id][VulkanEngine.ImageIndex] = default;

        _renderTextures[id][VulkanEngine.ImageIndex]?.Dispose();
        _renderTextures[id][VulkanEngine.ImageIndex] = null;
    }

    private void CreateTexture(Identification id)
    {
        var renderTextureDescription = _renderTextureDescriptions[id];

        var extent = renderTextureDescription.dimensions.Match(
            extentFunc => extentFunc(),
            _ => VulkanEngine.SwapchainExtent
        );
        var format = renderTextureDescription.format.Match(
            format => format,
            _ => VulkanEngine.SwapchainImageFormat
        );

        var textureDescription =
            TextureDescription.Texture2D(extent.Width, extent.Height, 1, 1, format, renderTextureDescription.usage);

        _renderTextures[id][VulkanEngine.ImageIndex] = TextureManager.Create(ref textureDescription);
    }
}