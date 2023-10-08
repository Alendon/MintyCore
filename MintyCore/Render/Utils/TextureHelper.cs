using System;
using System.Diagnostics;
using MintyCore.Render.VulkanObjects;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MintyCore.Render.Utils;

/// <summary>
///     Contains helper methods for dealing with mipmaps.
/// </summary>
public static class TextureHelper
{
    /// <summary>
    ///     Computes the number of mipmap levels in a texture.
    /// </summary>
    /// <param name="width">The width of the texture.</param>
    /// <param name="height">The height of the texture.</param>
    /// <returns>The number of mipmap levels needed for a texture of the given dimensions.</returns>
    private static int ComputeMipLevels(int width, int height)
    {
        return 1 + (int) Math.Floor(Math.Log(Math.Max(width, height), 2));
    }

    internal static int GetDimension(int largestLevelDimension, int mipLevel)
    {
        var ret = largestLevelDimension;
        for (var i = 0; i < mipLevel; i++) ret /= 2;

        return Math.Max(1, ret);
    }

    internal static Image<Rgba32>[] GenerateMipmaps(Image<Rgba32> baseImage, IResampler resampler)
    {
        var mipLevelCount = ComputeMipLevels(baseImage.Width, baseImage.Height);
        var mipLevels = new Image<Rgba32>[mipLevelCount];
        mipLevels[0] = baseImage;
        var i = 1;

        var currentWidth = baseImage.Width;
        var currentHeight = baseImage.Height;
        while (currentWidth != 1 || currentHeight != 1)
        {
            var newWidth = Math.Max(1, currentWidth / 2);
            var newHeight = Math.Max(1, currentHeight / 2);
            var newImage = baseImage.Clone(context => context.Resize(newWidth, newHeight, resampler));
            Debug.Assert(i < mipLevelCount);
            mipLevels[i] = newImage;

            i++;
            currentWidth = newWidth;
            currentHeight = newHeight;
        }

        Debug.Assert(i == mipLevelCount);

        return mipLevels;
    }
    
    /// <summary>
    ///     Compute the offset of a subresource
    /// </summary>
    /// <param name="tex">The texture to calculate the subresource</param>
    /// <param name="mipLevel">The mip level of the subresource</param>
    /// <param name="arrayLayer">The array layer of the subresource</param>
    /// <returns>Offset</returns>
    public static ulong ComputeSubresourceOffset(Texture tex, uint mipLevel, uint arrayLayer)
    {
        Debug.Assert((tex.Usage & TextureUsage.Staging) == TextureUsage.Staging);
        return ComputeArrayLayerOffset(tex, arrayLayer) + ComputeMipOffset(tex, mipLevel);
    }

    private static uint ComputeMipOffset(Texture tex, uint mipLevel)
    {
        var blockSize = FormatHelpers.IsCompressedFormat(tex.Format) ? 4u : 1u;
        uint offset = 0;
        for (uint level = 0; level < mipLevel; level++)
        {
            GetMipDimensions(tex, level, out var mipWidth, out var mipHeight, out var mipDepth);
            var storageWidth = Math.Max(mipWidth, blockSize);
            var storageHeight = Math.Max(mipHeight, blockSize);
            offset += FormatHelpers.GetRegionSize(storageWidth, storageHeight, mipDepth, tex.Format);
        }

        return offset;
    }

    private static uint ComputeArrayLayerOffset(Texture tex, uint arrayLayer)
    {
        if (arrayLayer == 0) return 0;

        var blockSize = FormatHelpers.IsCompressedFormat(tex.Format) ? 4u : 1u;
        uint layerPitch = 0;
        for (uint level = 0; level < tex.MipLevels; level++)
        {
            GetMipDimensions(tex, level, out var mipWidth, out var mipHeight, out var mipDepth);
            var storageWidth = Math.Max(mipWidth, blockSize);
            var storageHeight = Math.Max(mipHeight, blockSize);
            layerPitch += FormatHelpers.GetRegionSize(storageWidth, storageHeight, mipDepth, tex.Format);
        }

        return layerPitch * arrayLayer;
    }

    /// <summary>
    ///     Get mip level and array layer of subresource
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="subresource"></param>
    /// <param name="mipLevel"></param>
    /// <param name="arrayLayer"></param>
    public static void GetMipLevelAndArrayLayer(Texture tex, uint subresource, out uint mipLevel,
        out uint arrayLayer)
    {
        arrayLayer = subresource / tex.MipLevels;
        mipLevel = subresource - arrayLayer * tex.MipLevels;
    }

    /// <summary>
    /// </summary>
    public static void GetMipDimensions(uint texWidth, uint texHeight, uint texDepth, uint mipLevel, out uint width,
        out uint height, out uint depth)
    {
        width = GetDimension(texWidth, mipLevel);
        height = GetDimension(texHeight, mipLevel);
        depth = GetDimension(texDepth, mipLevel);
    }

    /// <summary>
    /// </summary>
    public static void GetMipDimensions(this Texture tex, uint mipLevel, out uint width, out uint height, out uint depth)
    {
        width = GetDimension(tex.Width, mipLevel);
        height = GetDimension(tex.Height, mipLevel);
        depth = GetDimension(tex.Depth, mipLevel);
    }

    /// <summary>
    /// </summary>
    /// <param name="largestLevelDimension"></param>
    /// <param name="mipLevel"></param>
    /// <returns></returns>
    public static uint GetDimension(uint largestLevelDimension, uint mipLevel)
    {
        var ret = largestLevelDimension;
        for (uint i = 0; i < mipLevel; i++) ret /= 2;

        return Math.Max(1, ret);
    }
}