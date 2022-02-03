using System;
using MintyCore.Utils;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

/// <summary>
/// Ui element to display a simple text
/// </summary>
public class TextBox : Element
{
    /// <inheritdoc />
    public override Image<Rgba32> Image => _image;

    private string _content;
    private Image<Rgba32> _image;
    private Identification _fontId;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="layout"></param>
    /// <param name="content"></param>
    /// <param name="fontFamilyId"></param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public TextBox(Layout layout, string content, Identification fontFamilyId) : base(layout)
    {
        _content = content;
        _fontId = fontFamilyId;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        _image.Dispose();
    }
}