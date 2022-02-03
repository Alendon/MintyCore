using MintyCore.Utils;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

/// <summary>
/// Ui element for a text input
/// </summary>
public class TextField : Element
{
    private Image<Rgba32> _image;
    private readonly TextInput _textInput;
    private readonly Identification _fontId;
    private Font _font;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="layout"></param>
    /// <param name="fontFamilyId"></param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public TextField(Layout layout, Identification fontFamilyId) : base(layout)
    {
        _textInput = new TextInput(false);
        _fontId = fontFamilyId;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override void Resize()
    {
        _image.Dispose();
        Initialize();
    }

    /// <inheritdoc />
    public override Image<Rgba32> Image => _image;

    /// <inheritdoc />
    public override void OnLeftClick()
    {
        _textInput.IsActive = CursorHovering;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        _image.Mutate(context =>
        {
            context.Fill(new(), _textInput.IsActive ? Color.DarkGray : Color.Gray);
            context.DrawText(_textInput.ToString(), _font, Color.White, new PointF(0, 0));
        });
    }


    /// <inheritdoc />
    public override void Dispose()
    {
        _image.Dispose();
    }
}