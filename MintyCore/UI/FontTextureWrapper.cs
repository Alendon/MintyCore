using System;
using JetBrains.Annotations;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

/// <summary>
/// Represents a wrapper for a font texture in the game.
/// This class is responsible for managing the lifecycle of a font texture and its associated resources.
/// </summary>
[PublicAPI]
public class FontTextureWrapper : IDisposable
{
    /// <summary>
    /// Gets or sets the texture of the font.
    /// </summary>
    public required Texture Texture { get; set; }

    /// <summary>
    /// Gets or sets the staging texture of the font.
    /// </summary>
    public required Texture StagingTexture { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the font texture has changed.
    /// </summary>
    public bool Changed { get; set; }

    /// <summary>
    /// Gets or sets the image view of the font texture.
    /// </summary>
    public required ImageView ImageView { get; set; }

    /// <summary>
    /// Gets or sets the sampler of the font texture.
    /// </summary>
    public required Sampler Sampler { get; set; }

    /// <summary>
    /// Gets or sets the descriptor set of the sampled image.
    /// </summary>
    public required DescriptorSet SampledImageDescriptorSet { get; set; }

    /// <summary/>
    public required IDescriptorSetManager DescriptorSetManager { get; set; }

    /// <summary/>
    public required IVulkanEngine VulkanEngine { get; set; }

    /// <summary/>
    public required IAllocationHandler AllocationHandler { get; set; }

    /// <summary>
    /// Applies changes to the font texture if any changes have been made.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to use for applying changes.</param>
    public void ApplyChanges(ManagedCommandBuffer commandBuffer)
    {
        if (!Changed) return;

        Texture.CopyTo(commandBuffer, (StagingTexture, 0, 0, 0, 0, 0), (Texture, 0, 0, 0, 0, 0), Texture.Width,
            Texture.Height, 1, 1);
        Changed = false;
    }

    /// <inheritdoc />
    public unsafe void Dispose()
    {
        DescriptorSetManager.FreeDescriptorSet(SampledImageDescriptorSet);
        VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, Sampler, null);
        VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, ImageView, null);

        Texture.Dispose();
        StagingTexture.Dispose();
    }
}