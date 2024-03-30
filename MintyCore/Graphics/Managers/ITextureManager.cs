using System;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MintyCore.Graphics.Managers;

/// <summary>
/// Interface for managing textures
/// </summary>
public interface ITextureManager
{
    /// <summary>
    /// Retrieves a texture from the manager by its ID.
    /// </summary>
    /// <param name="textureId">The unique identifier of the texture to retrieve.</param>
    /// <returns>The texture with the given ID.</returns>
    Texture GetTexture(Identification textureId);

    /// <summary>
    /// Retrieves a texture view from the manager by its ID.
    /// </summary>
    /// <param name="textureId">The unique identifier of the texture view to retrieve.</param>
    /// <returns>The texture view with the given ID.</returns>
    ImageView GetTextureView(Identification textureId);

    /// <summary>
    /// Retrieves a sampler from the manager by its ID.
    /// </summary>
    /// <param name="textureId">The unique identifier of the sampler to retrieve.</param>
    /// <returns>The sampler with the given ID.</returns>
    Sampler GetSampler(Identification textureId);

    /// <summary>
    /// Retrieves a texture resource set from the manager by its ID.
    /// </summary>
    /// <param name="texture">The unique identifier of the texture resource set to retrieve.</param>
    /// <returns>The texture resource set with the given ID.</returns>
    DescriptorSet GetTextureBindResourceSet(Identification texture);

    /// <summary>
    /// Copies an array of images to a texture.
    /// </summary>
    /// <param name="images">The images to copy. The length of this array must be the same as the mip map count of the target texture.</param>
    /// <param name="targetTexture">The texture to copy the images to.</param>
    /// <param name="flipY">Whether or not to flip the y axis of the images.</param>
    /// <typeparam name="TPixel">The pixel type of the images.</typeparam>
    void CopyImageToTexture<TPixel>(Span<Image<TPixel>> images, Texture targetTexture, bool flipY)
        where TPixel : unmanaged, IPixel<TPixel>;

    /// <summary>
    /// Adds a texture to the manager.
    /// </summary>
    /// <param name="textureId">The unique identifier for the texture.</param>
    /// <param name="mipMapping">Whether or not to use mip mapping for the texture.</param>
    /// <param name="resampler">The resampler to use for the texture.</param>
    /// <param name="flipY">Whether or not to flip the y axis of the texture.</param>
    void AddTexture(Identification textureId, bool mipMapping, IResampler resampler, bool flipY);
    
    /// <summary>
    /// Clears all internal data.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Removes a specific texture from the manager by its ID.
    /// </summary>
    /// <param name="objectId">The unique identifier of the texture to remove.</param>
    void RemoveTexture(Identification objectId);
    
    /// <summary>
    /// Creates a new texture with the given description.
    /// </summary>
    /// <param name="description">The description of the texture to create.</param>
    /// <returns>The created texture.</returns>
    Texture Create(ref TextureDescription description);
}