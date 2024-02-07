using System;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Registries;
using MintyCore.Utils;
using OneOf;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render.Managers;

public interface IRenderDataManager
{
    public void RegisterRenderTexture(Identification id, RenderTextureDescription textureData);
    public RenderTextureDescription GetRenderTextureDescription(Identification id);
    
    public Texture GetRenderTexture(Identification id);
    public ClearColorValue? GetClearColorValue(Identification id);
    public ImageView GetRenderImageView(Identification id);
    public DescriptorSet GetSampledTextureDescriptorSet(Identification id);
    public DescriptorSet GetStorageTextureDescriptorSet(Identification id);
    void RemoveRenderTexture(Identification objectId);
    void Clear();
}

internal static class RenderDataManagerDescriptors
{

    [RegisterDescriptorSet("sampled_render_texture")]
    public static DescriptorSetInfo SampledRenderTextureDescriptorSetInfo => new()
    {
        Bindings = [
        new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            StageFlags = ShaderStageFlags.FragmentBit
        }],
        DescriptorSetsPerPool = 100
    };
    
    [RegisterDescriptorSet("storage_render_texture")]
    public static DescriptorSetInfo StorageRenderTextureDescriptorSetInfo => new()
    {
        Bindings = [
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageImage,
                StageFlags = ShaderStageFlags.FragmentBit
            }],
        DescriptorSetsPerPool = 100
    };

}