using System;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.AvaloniaIntegration;

[RegisterInputDataModule("avalonia_ui")]
internal class UiInputModule(
    ITextureManager textureManager,
    IAvaloniaController avaloniaController,
    IVulkanEngine vulkanEngine,
    IDescriptorSetManager descriptorSetManager) : InputModule
{
    private Func<UiIntermediateData>? _provider;
    private Sampler _sampler;

    public override unsafe void Setup()
    {
        _provider = ModuleDataAccessor.ProvideIntermediateData<UiIntermediateData>(IntermediateRenderDataIDs.AvaloniaUi,
            this);

        SamplerCreateInfo createInfo = new()
        {
            SType = StructureType.SamplerCreateInfo
        };
        VulkanUtils.Assert(vulkanEngine.Vk.CreateSampler(vulkanEngine.Device, createInfo, null, out _sampler));
    }

    public override void Update(ManagedCommandBuffer commandBuffer)
    {
        var data = _provider?.Invoke();
        if (data is null)
            return;

        var newTexture = avaloniaController.Draw(data.Texture);

        if (ReferenceEquals(newTexture, data.Texture)) return;
        
        var oldTexture = data.Texture;
        data.Texture = newTexture;
        RecreateDescriptorSet(data);
        oldTexture?.Dispose();
        data.Texture.TransitionImageLayout(commandBuffer, 0, 1, 0, 1, ImageLayout.ShaderReadOnlyOptimal);
    }

    private unsafe void RecreateDescriptorSet(UiIntermediateData data)
    {
        if (data.DescriptorSet.Handle != default)
            descriptorSetManager.FreeDescriptorSet(data.DescriptorSet);

        if (data.ImageView.Handle != default)
            vulkanEngine.Vk.DestroyImageView(vulkanEngine.Device, data.ImageView, null);

        ImageViewCreateInfo imageView = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Format = Format.R8G8B8A8Unorm,
            Image = data.Texture!.Image,
            Components =
                { A = ComponentSwizzle.A, B = ComponentSwizzle.B, G = ComponentSwizzle.G, R = ComponentSwizzle.R },
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit, LayerCount = 1, LevelCount = 1, BaseArrayLayer = 0,
                BaseMipLevel = 0
            },
            ViewType = ImageViewType.Type2D
        };

        VulkanUtils.Assert(vulkanEngine.Vk.CreateImageView(vulkanEngine.Device, imageView, null, out data.ImageView));

        data.DescriptorSet = descriptorSetManager.AllocateDescriptorSet(DescriptorSetIDs.SampledTexture);

        DescriptorImageInfo imageInfo = new()
        {
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            Sampler = _sampler,
            ImageView = data.ImageView
        };

        WriteDescriptorSet write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DstBinding = 0,
            DstSet = data.DescriptorSet,
            DstArrayElement = 0,
            PImageInfo = &imageInfo
        };

        vulkanEngine.Vk.UpdateDescriptorSets(vulkanEngine.Device, 1, &write, 0, null);
    }

    public override bool UpdateAlways => true;
    public override Identification Identification => RenderInputModuleIDs.AvaloniaUi;

    public override void Dispose()
    {
    }
}