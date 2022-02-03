using System;
using MintyCore.Identifications;
using MintyCore.Utils;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

public class TextField : Element
{
    private Image<Rgba32> _image;
    private TextInput _textInput;
    private Identification _fontId;
    private Font _font;

    public TextField(Layout layout, Identification fontFamilyId) : base(layout)
    {
        _textInput = new TextInput(false);
        _fontId = fontFamilyId;
    }

    public override void Initialize()
    {
        _image = new Image<Rgba32>((int)PixelSize.X, (int)PixelSize.Y);
        CalculateFontSize();
    }

    private void CalculateFontSize()
    {
        for (int i = 1; i < 65; i++)
        {
            RendererOptions options = new(FontHandler.GetFont(_fontId, i));
            var size = TextMeasurer.MeasureBounds("Simple Text |", options);
            if (!(size.Height > PixelSize.Y) && !(size.Width > PixelSize.X)) continue;
            _font = FontHandler.GetFont(_fontId, i - 1);
            return;
        }

        _font = FontHandler.GetFont(_fontId, 64 - 1);
    }

    public override void Resize()
    {
        _image.Dispose();
        Initialize();
    }

    public override Image<Rgba32> Image => _image;

    public override void OnLeftClick()
    {
        _textInput.IsActive = CursorHovering;
    }

    public override void Update(float deltaTime)
    {
        _image.Mutate(context =>
        {
            context.Fill(new(), _textInput.IsActive ? Color.DarkGray : Color.Gray);
            context.DrawText(_textInput.ToString(), _font, Color.White, new PointF(0, 0));
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image.Dispose();
        }
    }

    public sealed override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}