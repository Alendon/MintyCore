using System.Numerics;
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
    public static Image<Rgba32> BuildBorderedImage(int width, int height, Rgba32 fillColor, BorderImages borderImages, out RectangleF innerLayout)
    {
        Logger.AssertAndThrow(width > 0 && height > 0, "Image dimensions must be positive", "UI");
        Image<Rgba32> image = new(width, height, fillColor);

        var borderLeft = ImageHandler.GetImage(borderImages.Left);
        var borderRight = ImageHandler.GetImage(borderImages.Right);
        var borderTop = ImageHandler.GetImage(borderImages.Top);
        var borderBottom = ImageHandler.GetImage(borderImages.Bottom);

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

            var cornerLl = ImageHandler.GetImage(borderImages.CornerLowerLeft);
            var cornerUl = ImageHandler.GetImage(borderImages.CornerUpperLeft);
            var cornerLr = ImageHandler.GetImage(borderImages.CornerLowerRight);
            var cornerUr = ImageHandler.GetImage(borderImages.CornerUpperRight);

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

/// <summary>
/// 
/// </summary>
public struct BorderImages
{
    /// <summary>
    /// 
    /// </summary>
    public Identification Left;
    /// <summary>
    /// 
    /// </summary>
    public Identification Right;
    /// <summary>
    /// 
    /// </summary>
    public Identification Top;
    /// <summary>
    /// 
    /// </summary>
    public Identification Bottom;
    
    /// <summary>
    /// 
    /// </summary>
    public Identification CornerUpperLeft;
    /// <summary>
    /// 
    /// </summary>
    public Identification CornerUpperRight;
    /// <summary>
    /// 
    /// </summary>
    public Identification CornerLowerLeft;
    /// <summary>
    /// 
    /// </summary>
    public Identification CornerLowerRight;
}