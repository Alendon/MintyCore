using System;
using System.Runtime.CompilerServices;
using MintyCore.Identifications;
using MintyCore.Render;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.UI;

public static class BorderBuilder
{
    public static unsafe Texture BuildBorderedTexture(uint width, uint height, Rgba32 fillColor)
    {
        TextureDescription description =
            TextureDescription.Texture2D(width, height, 1, 1, Format.R8G8B8A8Unorm, TextureUsage.STAGING);

        Texture texture = new Texture(ref description);

        CommandBuffer copyTextureBuffer = VulkanEngine.GetSingleTimeCommandBuffer();

        Texture cornerLl = TextureHandler.GetTexture(TextureIDs.UiCornerLowerLeft);

        Texture.CopyTo(copyTextureBuffer, (cornerLl, 0, 0, 0, 0, 0), (texture, 0, 0, 0, 0, 0), cornerLl.Width,
            cornerLl.Height, cornerLl.Depth, 1);

        Texture cornerUl = TextureHandler.GetTexture(TextureIDs.UiCornerUpperLeft);
        Texture.CopyTo(copyTextureBuffer, (cornerUl, 0, 0, 0, 0, 0),
            (texture, 0, texture.Height - cornerUl.Height, 0, 0, 0), cornerUl.Width,
            cornerUl.Height, cornerUl.Depth, 1);

        Texture cornerLr = TextureHandler.GetTexture(TextureIDs.UiCornerLowerRight);
        Texture.CopyTo(copyTextureBuffer, (cornerLr, 0, 0, 0, 0, 0),
            (texture, texture.Width - cornerLr.Width, 0, 0, 0, 0), cornerLr.Width,
            cornerLr.Height, cornerLr.Depth, 1);

        Texture cornerUr = TextureHandler.GetTexture(TextureIDs.UiCornerUpperRight);
        Texture.CopyTo(copyTextureBuffer, (cornerUr, 0, 0, 0, 0, 0),
            (texture, texture.Width - cornerUr.Width, texture.Height - cornerUr.Height, 0, 0, 0), cornerUr.Width,
            cornerUr.Height, cornerUr.Depth, 1);


        Texture borderLeft = TextureHandler.GetTexture(TextureIDs.UiBorderLeft);
        for (uint y = cornerLl.Height; y < height - cornerUl.Height; y++)
        {
            Texture.CopyTo(copyTextureBuffer, (borderLeft, 0, 0, 0, 0, 0), (texture, 0, y, 0, 0, 0),
                borderLeft.Width, borderLeft.Height, borderLeft.Depth, 1);
        }

        Texture borderRight = TextureHandler.GetTexture(TextureIDs.UiBorderRight);
        for (uint y = cornerLr.Height; y < height - cornerUr.Height; y++)
        {
            Texture.CopyTo(copyTextureBuffer, (borderRight, 0, 0, 0, 0, 0),
                (texture, width - borderRight.Width, y, 0, 0, 0),
                borderRight.Width, borderRight.Height, borderRight.Depth, 1);
        }

        Texture borderBottom = TextureHandler.GetTexture(TextureIDs.UiBorderBottom);
        for (uint x = cornerLl.Width; x < width - cornerLr.Width; x++)
        {
            Texture.CopyTo(copyTextureBuffer, (borderBottom, 0, 0, 0, 0, 0), (texture, x, 0, 0, 0, 0),
                borderBottom.Width, borderBottom.Height, borderBottom.Depth, 1);
        }
            
        Texture borderTop = TextureHandler.GetTexture(TextureIDs.UiBorderTop);
        for (uint x = cornerUl.Width; x < width - cornerUr.Width; x++)
        {
            Texture.CopyTo(copyTextureBuffer, (borderTop, 0, 0, 0, 0, 0), (texture, x, height - borderTop.Height, 0, 0, 0),
                borderTop.Width, borderTop.Height, borderTop.Depth, 1);
        }
            
        VulkanEngine.ExecuteSingleTimeCommandBuffer(copyTextureBuffer);
            
        var pointer = MemoryManager.Map(texture.MemoryBlock);
        Span<Rgba32> targetPixels = new Span<Rgba32>(pointer.ToPointer(), (int)(texture.Width * texture.Height));

        for (uint y = borderBottom.Height; y < height - borderTop.Height; y++)
        {
            for (uint x = borderLeft.Width; x < width - borderRight.Width; x++)
            {
                targetPixels[(int)(y * width + x)] = fillColor;
            }
        }

        MemoryManager.UnMap(texture.MemoryBlock);
            
        return texture;
    }
}