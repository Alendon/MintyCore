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
    private int _desiredFontSize;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="layout">The layout to use for the text box</param>
    /// <param name="content">The string the text box will show</param>
    /// <param name="fontFamilyId">The font family to use for rendering</param>
    /// <param name="desiredFontSize">The desired size of the font used.</param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public TextBox(Layout layout, string content, Identification fontFamilyId, ushort desiredFontSize = ushort.MaxValue) : base(layout)
    {
        _content = content;
        _fontId = fontFamilyId;
        _desiredFontSize = desiredFontSize;
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        var font = GetFittingFont();

        _image = new((int)PixelSize.X, (int)PixelSize.Y);
        _image.Mutate(context =>
        {
            DrawingOptions options = new()
            {
                TextOptions =
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrapTextWidth = -1
                }
            };

            PointF center = new PointF(PixelSize.X, PixelSize.Y) / 2;

            context.DrawText(options, _content, font, Color.White, center);
        });
    }

    /// <inheritdoc />
    public override void Resize()
    {
        _image.Dispose();
        Initialize();
    }

    private Font GetFittingFont()
    {
        bool scaleDown = _desiredFontSize != ushort.MaxValue;
        var startSize = scaleDown ? _desiredFontSize : 1;
        var endSize = scaleDown ? 1 : _desiredFontSize;
        var increment = scaleDown ? -1 : 1;
        
        for (int i = startSize; i != endSize; i += increment)
        {
            var font = FontHandler.GetFont(_fontId, i);
            RendererOptions options = new(font)
            {
                WrappingWidth = -1
            };
            var size = TextMeasurer.MeasureBounds(_content, options);

            if (dontFit(size)) continue;

            return scaleDown ? font : FontHandler.GetFont(_fontId, i - 1);
        }

        throw new Exception();

        bool dontFit(FontRectangle size)
        {
            if (scaleDown)
            {
                return size.Width > PixelSize.X || size.Height > PixelSize.Y;
            }

            return !(size.Width > PixelSize.X) && !(size.Height > PixelSize.Y);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        _image.Dispose();
    }
}