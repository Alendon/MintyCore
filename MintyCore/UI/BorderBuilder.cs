using System.Numerics;
using MintyCore.Identifications;
using MintyCore.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

/// <summary>
///     Helper class to build a border image
/// </summary>
public static class BorderBuilder
{
    /// <summary>
    ///     Build a border image
    /// </summary>
    /// <param name="width">Width of the image to create</param>
    /// <param name="height">Height of the image to create</param>
    /// <param name="fillColor">Color to fill the image with</param>
    /// <param name="innerLayout">The layout of the image inside the border</param>
    /// <returns>Created image with a border texture</returns>
    public static Image<Rgba32> BuildBorderedImage(int width, int height, Rgba32 fillColor, out RectangleF innerLayout)
    {
        Logger.AssertAndThrow(width > 0 && height > 0, "Image dimensions must be positive", "UI");
        Image<Rgba32> image = new(width, height, fillColor);

        var borderLeft = ImageHandler.GetImage(ImageIDs.UiBorderLeft);
        var borderRight = ImageHandler.GetImage(ImageIDs.UiBorderRight);
        var borderTop = ImageHandler.GetImage(ImageIDs.UiBorderTop);
        var borderBottom = ImageHandler.GetImage(ImageIDs.UiBorderBottom);

        innerLayout = new RectangleF(new PointF(borderLeft.Width, borderBottom.Height),
            new SizeF(width - borderLeft.Width - borderRight.Width, height - borderBottom.Height - borderTop.Height));

        if (innerLayout.Width < 0 || innerLayout.Height < 0)
        {
            Logger.WriteLog("Border image does not fit into given dimensions", LogImportance.Error, "UI");
            innerLayout = new RectangleF(Vector2.Zero, new SizeF(width, height));
            return image;
        }

        image.Mutate(context =>
        {
            const float opacity = 1;

            var cornerLl = ImageHandler.GetImage(ImageIDs.UiCornerLowerLeft);
            var cornerUl = ImageHandler.GetImage(ImageIDs.UiCornerUpperLeft);
            var cornerLr = ImageHandler.GetImage(ImageIDs.UiCornerLowerRight);
            var cornerUr = ImageHandler.GetImage(ImageIDs.UiCornerUpperRight);

            context.DrawImage(cornerUl, new Point(0, 0), opacity);
            context.DrawImage(cornerLl, new Point(0, height - cornerLl.Height), opacity);
            context.DrawImage(cornerUr, new Point(width - cornerUr.Width, 0), opacity);
            context.DrawImage(cornerLr, new Point(width - cornerLr.Width, height - cornerLr.Width), opacity);

            for (var y = cornerLl.Height; y < height - cornerUl.Height; y++)
            {
                context.DrawImage(borderLeft, new Point(0, y), opacity);
                context.DrawImage(borderRight, new Point(width - borderRight.Width, y), opacity);
            }

            for (var x = cornerLl.Width; x < width - cornerLr.Width; x++)
            {
                context.DrawImage(borderTop, new Point(x, 0), opacity);
                context.DrawImage(borderBottom, new Point(x, height - borderBottom.Height), opacity);
            }
        });


        return image;
    }
}