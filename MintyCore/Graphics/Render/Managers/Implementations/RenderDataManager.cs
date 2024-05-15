using System;
using System.Collections.Generic;
using System.Linq;
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
    private Dictionary<Identification, DescriptorSet[]> _sampledNearestTextureDescriptorSets = new();
    private Dictionary<Identification, DescriptorSet[]> _sampledLinearTextureDescriptorSets = new();
    private Dictionary<Identification, DescriptorSet[]> _storageTextureDescriptorSets = new();

    private Sampler _linearSampler;
    private Sampler _nearestSampler;

    public void RegisterRenderTexture(Identification id, RenderTextureDescription textureData)
    {
        if ((textureData.Usage & TextureUsage.DepthStencil) != 0 && textureData.Usage != TextureUsage.DepthStencil)
            throw new InvalidOperationException("DepthStencil usage must be used exclusively");

        if ((textureData.Usage & TextureUsage.Staging) != 0)
            throw new InvalidOperationException("Staging usage is not allowed for render textures");

        if ((textureData.Usage & TextureUsage.Cubemap) != 0)
            throw new InvalidOperationException("Cubemap usage is not allowed for render textures");

        _renderTextureDescriptions.Add(id, textureData);
        _renderTextures.Add(id, new Texture?[VulkanEngine.SwapchainImageCount]);

        _renderImageViews.Add(id, new ImageView[VulkanEngine.SwapchainImageCount]);
        _sampledLinearTextureDescriptorSets.Add(id, new DescriptorSet[VulkanEngine.SwapchainImageCount]);
        _sampledNearestTextureDescriptorSets.Add(id, new DescriptorSet[VulkanEngine.SwapchainImageCount]);
        _storageTextureDescriptorSets.Add(id, new DescriptorSet[VulkanEngine.SwapchainImageCount]);
    }

    public RenderTextureDescription GetRenderTextureDescription(Identification id)
    {
        return _renderTextureDescriptions[id];
    }

    public Texture GetRenderTexture(Identification id)
    {
        CheckTextureSize(id);

        return _renderTextures[id][VulkanEngine.RenderIndex]!;
    }

    public ClearColorValue? GetClearColorValue(Identification id)
    {
        return _renderTextureDescriptions[id].ClearColorValue;
    }

    public unsafe ImageView GetRenderImageView(Identification id)
    {
        CheckTextureSize(id);

        var imageView = _renderImageViews[id][VulkanEngine.RenderIndex];
        if (imageView.Handle != 0)
            return imageView;

        var textureDescription = _renderTextureDescriptions[id];
        var format = textureDescription.Format.Match(
            format => format,
            _ => VulkanEngine.SwapchainImageFormat
        );

        var texture = _renderTextures[id][VulkanEngine.RenderIndex];

        var createInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Format = format,
            Components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B,
                ComponentSwizzle.A),
            SubresourceRange =
            {
                AspectMask = textureDescription.Usage == TextureUsage.DepthStencil
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

        _renderImageViews[id][VulkanEngine.RenderIndex] = imageView;

        return imageView;
    }

    public unsafe DescriptorSet GetSampledTextureDescriptorSet(Identification id, ColorAttachmentSampleMode sampleMode)
    {
        CheckTextureSize(id);
        CreateSampler();

        var descriptorSet = sampleMode switch
        {
            ColorAttachmentSampleMode.Linear => _sampledLinearTextureDescriptorSets[id][VulkanEngine.RenderIndex],
            ColorAttachmentSampleMode.Nearest => _sampledNearestTextureDescriptorSets[id][VulkanEngine.RenderIndex],
            _ => throw new ArgumentOutOfRangeException(nameof(sampleMode), sampleMode, null)
        };
        
        if (descriptorSet.Handle != 0)
            return descriptorSet;

        descriptorSet = DescriptorSetManager.AllocateDescriptorSet(DescriptorSetIDs.SampledRenderTexture);
        var imageInfo = new DescriptorImageInfo
        {
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = GetRenderImageView(id),
            Sampler = sampleMode switch
            {
                ColorAttachmentSampleMode.Linear => _linearSampler,
                ColorAttachmentSampleMode.Nearest => _nearestSampler,
                _ => throw new ArgumentOutOfRangeException(nameof(sampleMode), sampleMode, null)
            }
        };

        var writeDescriptorSet = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DstBinding = 0,
            DstSet = descriptorSet,
            PImageInfo = &imageInfo
        };

        VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, &writeDescriptorSet, 0, null);

        switch (sampleMode)
        {
            case ColorAttachmentSampleMode.Linear:
                _sampledLinearTextureDescriptorSets[id][VulkanEngine.RenderIndex] = descriptorSet;
                break;
            case ColorAttachmentSampleMode.Nearest:
                _sampledNearestTextureDescriptorSets[id][VulkanEngine.RenderIndex] = descriptorSet;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sampleMode), sampleMode, null);
        }
        
        return descriptorSet;
    }

    private unsafe void CreateSampler()
    {
        if (_linearSampler.Handle != 0)
            return;

        var samplerCreateInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            AddressModeU = SamplerAddressMode.ClampToBorder,
            AddressModeV = SamplerAddressMode.ClampToBorder,
            AddressModeW = SamplerAddressMode.ClampToBorder,
            MinLod = 0,
            MaxLod = 1,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateSampler(VulkanEngine.Device, samplerCreateInfo, null, out _linearSampler));
        
        samplerCreateInfo.MinFilter = Filter.Nearest;
        samplerCreateInfo.MagFilter = Filter.Nearest;
        samplerCreateInfo.MipmapMode = SamplerMipmapMode.Nearest;
        
        VulkanUtils.Assert(VulkanEngine.Vk.CreateSampler(VulkanEngine.Device, samplerCreateInfo, null, out _nearestSampler));
    }

    public unsafe DescriptorSet GetStorageTextureDescriptorSet(Identification id)
    {
        CheckTextureSize(id);

        var descriptorSet = _storageTextureDescriptorSets[id][VulkanEngine.RenderIndex];
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
        _storageTextureDescriptorSets[id][VulkanEngine.RenderIndex] = descriptorSet;

        return descriptorSet;
    }

    public unsafe void RemoveRenderTexture(Identification objectId)
    {
        _renderTextureDescriptions.Remove(objectId);

        if (_sampledLinearTextureDescriptorSets.Remove(objectId, out var sampledDescriptorSets))
        {
            foreach (var descriptorSet in sampledDescriptorSets)
            {
                DescriptorSetManager.FreeDescriptorSet(descriptorSet);
            }
        }
        
        if (_sampledNearestTextureDescriptorSets.Remove(objectId, out sampledDescriptorSets))
        {
            foreach (var descriptorSet in sampledDescriptorSets)
            {
                DescriptorSetManager.FreeDescriptorSet(descriptorSet);
            }
        }

        if (_storageTextureDescriptorSets.Remove(objectId, out var storageDescriptorSets))
        {
            foreach (var descriptorSet in storageDescriptorSets)
            {
                DescriptorSetManager.FreeDescriptorSet(descriptorSet);
            }
        }

        if (_renderImageViews.Remove(objectId, out var imageViews))
        {
            foreach (var imageView in imageViews)
            {
                VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, imageView, null);
            }
        }

        if (_renderTextures.Remove(objectId, out var textures))
        {
            foreach (var texture in textures)
            {
                texture?.Dispose();
            }
        }
    }

    public unsafe void Clear()
    {
        var remainingIds = _renderTextureDescriptions.Keys.ToArray();

        foreach (var id in remainingIds)
        {
            RemoveRenderTexture(id);
        }

        VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, _linearSampler, null);
        VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, _nearestSampler, null);
    }

    private void CheckTextureSize(Identification id)
    {
        if (!_renderTextures.TryGetValue(id, out var textures))
        {
            throw new InvalidOperationException($"Render texture with id {id} has not been registered");
        }

        var currentFrame = VulkanEngine.RenderIndex;
        var currentTexture = textures[currentFrame];

        var currentSize = _renderTextureDescriptions[id].Dimensions.Match(
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
        var sampleNearestDescriptor = _sampledNearestTextureDescriptorSets[id][VulkanEngine.RenderIndex];
        var sampleLinearDescriptor = _sampledLinearTextureDescriptorSets[id][VulkanEngine.RenderIndex];
        var storageDescriptor = _storageTextureDescriptorSets[id][VulkanEngine.RenderIndex];

        if (sampleNearestDescriptor.Handle != 0)
            DescriptorSetManager.FreeDescriptorSet(sampleNearestDescriptor);
        if (sampleLinearDescriptor.Handle != 0)
            DescriptorSetManager.FreeDescriptorSet(sampleLinearDescriptor);
        if (storageDescriptor.Handle != 0)
            DescriptorSetManager.FreeDescriptorSet(storageDescriptor);

        _sampledNearestTextureDescriptorSets[id][VulkanEngine.RenderIndex] = default;
        _sampledLinearTextureDescriptorSets[id][VulkanEngine.RenderIndex] = default;
        _storageTextureDescriptorSets[id][VulkanEngine.RenderIndex] = default;

        var imageView = _renderImageViews[id][VulkanEngine.RenderIndex];
        VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, imageView, null);
        _renderImageViews[id][VulkanEngine.RenderIndex] = default;

        _renderTextures[id][VulkanEngine.RenderIndex]?.Dispose();
        _renderTextures[id][VulkanEngine.RenderIndex] = null;
    }

    private void CreateTexture(Identification id)
    {
        var renderTextureDescription = _renderTextureDescriptions[id];

        var extent = renderTextureDescription.Dimensions.Match(
            extentFunc => extentFunc(),
            _ => VulkanEngine.SwapchainExtent
        );
        var format = renderTextureDescription.Format.Match(
            format => format,
            _ => VulkanEngine.SwapchainImageFormat
        );

        var textureDescription =
            TextureDescription.Texture2D(extent.Width, extent.Height, 1, 1, format, renderTextureDescription.Usage);

        _renderTextures[id][VulkanEngine.RenderIndex] = TextureManager.Create(ref textureDescription);
    }
}