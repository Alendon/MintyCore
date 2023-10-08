using System;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MintyCore.Render.Managers.Interfaces;

public interface ITextureManager
{
    /// <summary>
    ///     Get a Texture
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    Texture GetTexture(Identification textureId);

    /// <summary>
    ///     Get a TextureView
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    ImageView GetTextureView(Identification textureId);

    /// <summary>
    ///     Get a Sampler
    /// </summary>
    /// <param name="textureId"></param>
    /// <returns></returns>
    Sampler GetSampler(Identification textureId);

    /// <summary>
    ///     Get TextureResourceSet
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    DescriptorSet GetTextureBindResourceSet(Identification texture);

    /// <summary>
    ///     Copy a <see cref="Image{TPixel}" /> array to a <see cref="Texture" />
    /// </summary>
    /// <param name="images">Images to copy; Must be same length as mip map count</param>
    /// <param name="targetTexture">Texture to copy to</param>
    /// <param name="flipY">Whether or not to flip the y axis</param>
    /// <typeparam name="TPixel"></typeparam>
    unsafe void CopyImageToTexture<TPixel>(Span<Image<TPixel>> images, Texture targetTexture, bool flipY)
        where TPixel : unmanaged, IPixel<TPixel>;

    unsafe void AddTexture(Identification textureId, bool mipMapping, IResampler resampler, bool flipY);
    unsafe void Clear();
    unsafe void RemoveTexture(Identification objectId);
}