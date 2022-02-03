using System;
using System.Numerics;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace MintyCore.UI;

public class TextBox : Element
{
    public override Image<Rgba32> Image => _image;

    private string _content;
    private Image<Rgba32> _image;
    private Identification _fontId;

    public TextBox(Layout layout, string content, Identification fontFamilyId) : base(layout)
    {
        _content = content;
        _fontId = fontFamilyId;
    }

    public override void Initialize()
    {
        var (font, size) = GetTextInfo();
        _image = new((int)size.Width, (int)size.Height);
        _image.Mutate(context =>
        {
            context.DrawText(_content, font, Color.White, new PointF(0, 0));
            var xScale = size.Width / PixelSize.X;
            var yScale = size.Height / PixelSize.Y;
            var usedScale = xScale > yScale ? xScale : yScale;
            var xSize = size.Width / usedScale;
            var ySize = size.Height / usedScale;
            context.Resize((int)xSize, (int)ySize);
        });
    }

    public override void Resize()
    {
        _image.Dispose();
        Initialize();
    }

    private (Font font, FontRectangle textSize) GetTextInfo()
    {
        for (int i = 1; i < int.MaxValue; i++)
        {
            var font = FontHandler.GetFont(_fontId, i);
            RendererOptions options = new(font);
            var size = TextMeasurer.MeasureBounds(_content, options);
            if (!(size.Width > PixelSize.X) && !(size.Height > PixelSize.Y)) continue;

            return (font, size);
        }

        throw new Exception();
    }

    public override void Dispose()
    {
        base.Dispose();
        _image.Dispose();
    }
}