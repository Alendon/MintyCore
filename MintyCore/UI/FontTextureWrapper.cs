using System;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

public class FontTextureWrapper : IDisposable
{
    public required Texture Texture { get; set; }
    public required Texture StagingTexture { get; set; }
    public bool Changed { get; set; }
    public required ImageView ImageView { get; set; }
    public required Sampler Sampler { get; set; }
    public required DescriptorSet SampledImageDescriptorSet { get; set; }
    public required IDescriptorSetManager DescriptorSetManager { get; set; }
    public required IVulkanEngine VulkanEngine { get; set; }
    public required IAllocationHandler AllocationHandler { get; set; }

    public void ApplyChanges(ManagedCommandBuffer commandBuffer)
    {
        if (!Changed) return;

        Texture.CopyTo(commandBuffer, (StagingTexture, 0, 0, 0, 0, 0), (Texture, 0, 0, 0, 0, 0), Texture.Width,
            Texture.Height, 1, 1);
        Changed = false;
    }

    public unsafe void Dispose()
    {
        DescriptorSetManager.FreeDescriptorSet(SampledImageDescriptorSet);
        VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, Sampler, null);
        VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, ImageView, null);

        Texture.Dispose();
        StagingTexture.Dispose();
    }
}