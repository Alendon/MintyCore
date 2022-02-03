using MintyCore.Identifications;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

/// <summary>
/// Helper class to build a border image
/// </summary>
public static class BorderBuilder
{
    /// <summary>
    /// Build a border image
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="fillColor"></param>
    /// <returns></returns>
    public static Image<Rgba32> BuildBorderedImage(int width, int height, Rgba32 fillColor)
    {
        Image<Rgba32> image = new(width, height, fillColor);

        image.Mutate(context =>
        {
            const float opacity = 1;

            var cornerLl = ImageHandler.GetImage(ImageIDs.UiCornerLowerLeft);
            context.DrawImage(cornerLl, new Point(0, height - cornerLl.Height), opacity);

            var cornerUl = ImageHandler.GetImage(ImageIDs.UiCornerUpperLeft);
            context.DrawImage(cornerUl, new Point(0, 0), opacity);

            var cornerLr = ImageHandler.GetImage(ImageIDs.UiCornerLowerRight);
            context.DrawImage(cornerLr, new Point(height - cornerLr.Width, height - cornerLr.Height), opacity);

            var cornerUr = ImageHandler.GetImage(ImageIDs.UiCornerUpperRight);
            context.DrawImage(cornerUr, new Point(height - cornerUr.Width, 0),
                opacity);

            var borderLeft = ImageHandler.GetImage(ImageIDs.UiBorderLeft);
            var borderRight = ImageHandler.GetImage(ImageIDs.UiBorderRight);

            for (int y = cornerLl.Height; y < height - cornerUl.Height; y++)
            {
                context.DrawImage(borderRight, new Point(0, y), opacity);
            }

            for (int y = cornerLr.Height; y < height - cornerUr.Height; y++)
            {
                context.DrawImage(borderLeft, new Point(width - borderLeft.Width, y), opacity);
            }
            
            var borderTop = ImageHandler.GetImage(ImageIDs.UiBorderTop);
            var borderBottom = ImageHandler.GetImage(ImageIDs.UiBorderBottom);
            for (int x = cornerLl.Width; x < width - cornerLr.Width; x++)
            {
                context.DrawImage(borderTop, new Point(x, 0), opacity);
            }

            for (int x = cornerUl.Width; x < width - cornerUr.Width; x++)
            {
                context.DrawImage(borderBottom, new Point(x, height - borderBottom.Height), opacity);
            }
        });

        return image;
    }
}