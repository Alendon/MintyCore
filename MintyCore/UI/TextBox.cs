using System;
using System.Numerics;
using MintyCore.Utils;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

//TODO implement a dynamic offset, so that the text dont stick directly to the edges

/// <summary>
///     Ui element to display a simple text
/// </summary>
public class TextBox : Element
{
    private readonly int _desiredFontSize;
    private readonly Identification _fontId;
    private readonly bool _useBorder;
    private string _content;
    private Color _drawColor;
    private Color _fillColor;
    private Font? _font;
    private HorizontalAlignment _horizontalAlignment;

    private Image<Rgba32>? _image;
    private RectangleF _innerLayout;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="layout">The layout to use for the text box</param>
    /// <param name="content">The string the text box will show</param>
    /// <param name="fontFamilyId">The font family to use for rendering</param>
    /// <param name="desiredFontSize">The desired size of the font used.</param>
    /// <param name="useBorder">Whether or not a border should be drawn around the element</param>
    /// <param name="horizontalAlignment">Which horizontal alignment the text should use</param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public TextBox(RectangleF layout, string content, Identification fontFamilyId,
        ushort desiredFontSize = ushort.MaxValue,
        bool useBorder = true, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center) : base(layout)
    {
        _content = content;
        _fontId = fontFamilyId;
        _desiredFontSize = desiredFontSize;
        _useBorder = useBorder;
        _horizontalAlignment = horizontalAlignment;
        _drawColor = Color.White;
        _fillColor = Color.Transparent;
    }

    /// <inheritdoc />
    public override Image<Rgba32>? Image => _image;

    /// <summary>
    ///     Get or set the content
    /// </summary>
    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            HasChanged = true;
        }
    }

    /// <summary>
    ///     Get or set the horizontal alignment of the text
    /// </summary>
    public HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set
        {
            _horizontalAlignment = value;
            HasChanged = true;
        }
    }

    /// <summary>
    ///     Get or set the fill / background color
    /// </summary>
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            HasChanged = true;
        }
    }

    /// <summary>
    ///     Get or set the draw color
    /// </summary>
    public Color DrawColor
    {
        get => _drawColor;
        set
        {
            _drawColor = value;
            HasChanged = true;
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        _image = _useBorder
            ? BorderBuilder.BuildBorderedImage((int)PixelSize.Width, (int)PixelSize.Height, Color.Transparent,
                out _innerLayout)
            : new Image<Rgba32>((int)PixelSize.Width, (int)PixelSize.Height);
        if (!_useBorder) _innerLayout = new RectangleF(Vector2.Zero, new SizeF(PixelSize.Width, PixelSize.Height));

        _font = GetFittingFont();

        HasChanged = true;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (!HasChanged) return;

        _image.Mutate(context =>
        {
            context.Fill(FillColor, _innerLayout);

            PointF drawPoint = default;
            drawPoint.Y = _innerLayout.Y + _innerLayout.Height * 0.5f;
            switch (HorizontalAlignment)
            {
                default:
                case HorizontalAlignment.Left:
                    _horizontalAlignment = HorizontalAlignment.Left;
                    drawPoint.X = _innerLayout.X;
                    break;
                case HorizontalAlignment.Right:
                    drawPoint.X = _innerLayout.X + _innerLayout.Width;
                    break;
                case HorizontalAlignment.Center:
                    drawPoint.X = _innerLayout.X + _innerLayout.Width / 2f;
                    break;
            }

            DrawingOptions options = new()
            {
                TextOptions =
                {
                    HorizontalAlignment = HorizontalAlignment,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            context.DrawText(options, _content, _font, DrawColor, drawPoint);
        });

        HasChanged = false;
    }

    /// <inheritdoc />
    public override void Resize()
    {
        _image?.Dispose();
        Initialize();
    }

    private Font GetFittingFont()
    {
        var scaleDown = _desiredFontSize != ushort.MaxValue;
        var startSize = scaleDown ? _desiredFontSize : 1;
        var endSize = scaleDown ? 1 : _desiredFontSize;
        var increment = scaleDown ? -1 : 1;

        for (var i = startSize; i != endSize; i += increment)
        {
            var font = FontHandler.GetFont(_fontId, i);
            RendererOptions options = new(font)
            {
                WrappingWidth = -1
            };
            var size = TextMeasurer.MeasureBounds(Content.Length != 0 ? Content : "Measure |", options);

            if (DontFit(size)) continue;

            return scaleDown ? font : FontHandler.GetFont(_fontId, i - 1);
        }

        throw new Exception();

        bool DontFit(FontRectangle size)
        {
            if (scaleDown) return size.Width > _innerLayout.Width || size.Height > _innerLayout.Height;

            return !(size.Width > _innerLayout.Width) && !(size.Height > _innerLayout.Height);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        _image?.Dispose();
    }
}