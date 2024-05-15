using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render.Managers;

/// <summary>
/// Interface for managing render data.
/// </summary>
public interface IRenderDataManager
{
    /// <summary>
    /// Registers a new render texture with the given identification and texture data.
    /// </summary>
    /// <param name="id">The identification of the render texture.</param>
    /// <param name="textureData">The data of the render texture.</param>
    public void RegisterRenderTexture(Identification id, RenderTextureDescription textureData);

    /// <summary>
    /// Retrieves the render texture description associated with the given identification.
    /// </summary>
    /// <param name="id">The identification of the render texture.</param>
    /// <returns>The render texture description.</returns>
    public RenderTextureDescription GetRenderTextureDescription(Identification id);

    /// <summary>
    /// Retrieves the render texture associated with the given identification.
    /// </summary>
    /// <param name="id">The identification of the render texture.</param>
    /// <returns>The render texture.</returns>
    public Texture GetRenderTexture(Identification id);

    /// <summary>
    /// Retrieves the clear color value associated with the given identification.
    /// </summary>
    /// <param name="id">The identification of the render texture.</param>
    /// <returns>The clear color value.</returns>
    public ClearColorValue? GetClearColorValue(Identification id);

    /// <summary>
    /// Retrieves the render image view associated with the given identification.
    /// </summary>
    /// <param name="id">The identification of the render texture.</param>
    /// <returns>The render image view.</returns>
    public ImageView GetRenderImageView(Identification id);

    /// <summary>
    /// Retrieves the sampled texture descriptor set associated with the given identification.
    /// </summary>
    /// <param name="id">The identification of the render texture.</param>
    /// <returns>The sampled texture descriptor set.</returns>
    public DescriptorSet GetSampledTextureDescriptorSet(Identification id, ColorAttachmentSampleMode sampleMode);

    /// <summary>
    /// Retrieves the storage texture descriptor set associated with the given identification.
    /// </summary>
    /// <param name="id">The identification of the render texture.</param>
    /// <returns>The storage texture descriptor set.</returns>
    public DescriptorSet GetStorageTextureDescriptorSet(Identification id);

    /// <summary>
    /// Removes the render texture associated with the given identification.
    /// </summary>
    /// <param name="objectId">The identification of the render texture.</param>
    void RemoveRenderTexture(Identification objectId);

    /// <summary>
    /// Clears all internal data.
    /// </summary>
    void Clear();
}

internal static class RenderDataManagerDescriptors
{
    [RegisterDescriptorSet("sampled_render_texture")]
    public static DescriptorSetInfo SampledRenderTextureDescriptorSetInfo => new()
    {
        Bindings =
        [
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlags.FragmentBit
            }
        ],
        DescriptorSetsPerPool = 100
    };

    [RegisterDescriptorSet("storage_render_texture")]
    public static DescriptorSetInfo StorageRenderTextureDescriptorSetInfo => new()
    {
        Bindings =
        [
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageImage,
                StageFlags = ShaderStageFlags.FragmentBit
            }
        ],
        DescriptorSetsPerPool = 100
    };
}