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
/// Ui element to display a simple text
/// </summary>
public class TextBox : Element
{
    /// <inheritdoc />
    public override Image<Rgba32> Image => _image;

    private Image<Rgba32> _image;
    private readonly Identification _fontId;
    private readonly int _desiredFontSize;
    private readonly bool _useBorder;
    private string _content;
    private Color _fillColor;
    private HorizontalAlignment _horizontalAlignment;
    private Layout _innerLayout;
    private Font _font;
    private Color _drawColor;

    /// <summary>
    /// Get or set the content
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
    /// Get or set the horizontal alignment of the text
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
    /// Get or set the fill / background color
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
    /// Get or set the draw color
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

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="layout">The layout to use for the text box</param>
    /// <param name="content">The string the text box will show</param>
    /// <param name="fontFamilyId">The font family to use for rendering</param>
    /// <param name="desiredFontSize">The desired size of the font used.</param>
    /// <param name="useBorder">Whether or not a border should be drawn around the element</param>
    /// <param name="horizontalAlignment">Which horizontal alignment the text should use</param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public TextBox(Layout layout, string content, Identification fontFamilyId, ushort desiredFontSize = ushort.MaxValue,
        bool useBorder = true, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center) : base(layout)
    {
        Content = content;
        _fontId = fontFamilyId;
        _desiredFontSize = desiredFontSize;
        _useBorder = useBorder;
        HorizontalAlignment = horizontalAlignment;
        _drawColor = Color.White;
        _fillColor = Color.Transparent;
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        _image = _useBorder
            ? BorderBuilder.BuildBorderedImage((int)PixelSize.X, (int)PixelSize.Y, Color.Transparent, out _innerLayout)
            : new((int)PixelSize.X, (int)PixelSize.Y);
        if (!_useBorder) _innerLayout = new Layout(Vector2.Zero, new Vector2(PixelSize.X, PixelSize.Y));

        _font = GetFittingFont();

        HasChanged = true;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (!HasChanged) return;
        
        _image.Mutate(context =>
        {
            RectangleF fillArea = new(new PointF(_innerLayout.Offset.X, _innerLayout.Offset.Y),
                new SizeF(_innerLayout.Extent.X, _innerLayout.Extent.Y));
            context.Fill(FillColor, fillArea);

            PointF drawPoint = default;
            drawPoint.Y = _innerLayout.Offset.Y + _innerLayout.Extent.Y * 0.5f;
            switch (HorizontalAlignment)
            {
                default:
                case HorizontalAlignment.Left:
                    _horizontalAlignment = HorizontalAlignment.Left;
                    drawPoint.X = fillArea.Location.X;
                    break;
                case HorizontalAlignment.Right:
                    drawPoint.X = fillArea.Location.X;
                    break;
                case HorizontalAlignment.Center:
                    drawPoint.X = fillArea.Location.X + fillArea.Size.Width / 2f;
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
            var size = TextMeasurer.MeasureBounds(Content.Length != 0 ? Content : "Measure |", options);

            if (DontFit(size)) continue;

            return scaleDown ? font : FontHandler.GetFont(_fontId, i - 1);
        }

        throw new Exception();

        bool DontFit(FontRectangle size)
        {
            if (scaleDown)
            {
                return size.Width > _innerLayout.Extent.X || size.Height > _innerLayout.Extent.Y;
            }

            return !(size.Width > _innerLayout.Extent.X) && !(size.Height > _innerLayout.Extent.Y);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        _image.Dispose();
    }
}