using System;
using System.Collections.Generic;
using System.Drawing;
using MintyCore.Identifications;
using MintyCore.Render.Utils;
using MintyCore.Render.VulkanObjects;
using MintyCore.UI;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Managers;

internal partial class TextureManager
{
    private readonly List<FontTextureWrapper> _managedTextures = new();
    
    /// <inheritdoc />
    public unsafe object CreateTexture(int width, int height)
    {
        var description = TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, Format.R8G8B8A8Unorm,
            TextureUsage.Sampled);
        var stagingDescription = TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, Format.R8G8B8A8Unorm,
            TextureUsage.Sampled | TextureUsage.Staging);

        var texture = Create(ref description);
        var stagingTexture = Create(ref stagingDescription);

        SamplerCreateInfo samplerCreateInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            AnisotropyEnable = Vk.True,
            BorderColor = BorderColor.FloatTransparentBlack,
            MaxAnisotropy = 4,
            AddressModeU = SamplerAddressMode.ClampToBorder,
            AddressModeV = SamplerAddressMode.ClampToBorder,
            AddressModeW = SamplerAddressMode.ClampToBorder,
            MipmapMode = SamplerMipmapMode.Linear,
            CompareOp = CompareOp.Never,
            CompareEnable = Vk.False,
            MinLod = 0,
            MaxLod = 1,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateSampler(VulkanEngine.Device, in samplerCreateInfo,
            null, out var sampler));

        ImageViewCreateInfo imageViewCreateInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Format = texture.Format,
            Image = texture.Image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LayerCount = 1,
                LevelCount = 1,
                BaseArrayLayer = 0,
                BaseMipLevel = 0
            },
            ViewType = ImageViewType.Type2D
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateImageView(VulkanEngine.Device, in imageViewCreateInfo,
            null, out var imageView));

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
            DstSet = descriptorSet,
            PImageInfo = &descriptorImageInfo,
        };

        VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, &writeDescriptorSet, 0, null);

        var textureWrapper = new FontTextureWrapper
        {
            Texture = texture,
            StagingTexture = stagingTexture,
            Sampler = sampler,
            ImageView = imageView,
            SampledImageDescriptorSet = descriptorSet,
            DescriptorSetManager = DescriptorSetManager,
            VulkanEngine = VulkanEngine,
            AllocationHandler = AllocationHandler
        };
        
        _managedTextures.Add(textureWrapper);
        
        return textureWrapper;
    }

    /// <inheritdoc />
    public Point GetTextureSize(object texture)
    {
        if (texture is not FontTextureWrapper tex)
        {
            throw new ArgumentException("Texture is not a FontTextureWrapper", nameof(texture));
        }

        return new Point((int)tex.Texture.Width, (int)tex.Texture.Height);
    }

    /// <inheritdoc />
    public unsafe void SetTextureData(object texture, Rectangle bounds, byte[] data)
    {
        if(texture is not FontTextureWrapper tex)
        {
            throw new ArgumentException("Texture is not a FontTextureWrapper", nameof(texture));
        }
        
        var stagingTexture = tex.StagingTexture;
        var layout = stagingTexture.GetSubresourceLayout(0);

        var dataSpan = data.AsSpan();
        var texSpan = new Span<byte>((void*)(MemoryManager.Map(stagingTexture.MemoryBlock).ToInt64() + (long)layout.Offset),
            (int)layout.Size);

        for (var y = 0; y < bounds.Height; y++)
        {
            var sourceSpan = dataSpan.Slice(y * bounds.Width * 4, bounds.Width * 4);
            var destinationSpan = texSpan.Slice((int)layout.RowPitch * (bounds.Y + y) + bounds.X * 4, bounds.Width * 4);
            sourceSpan.CopyTo(destinationSpan);
        }

        tex.StagingTexture = stagingTexture;
        tex.Changed = true;
    }

    /// <inheritdoc />
    public void ApplyChanges(ManagedCommandBuffer commandBuffer)
    {
        foreach (var texture in _managedTextures)
        {
            texture.ApplyChanges(commandBuffer);
        }
    }
}