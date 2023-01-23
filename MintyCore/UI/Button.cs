using System;
using JetBrains.Annotations;
using MintyCore.Identifications;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

/// <summary>
///     Simple button ui element
/// </summary>
[PublicAPI]
public class Button : Element
{
    private readonly string _content;
    private readonly ushort _desiredFontSize;
    private Image<Rgba32>? _image;
    private RectangleF _innerLayout;
    private bool _lastHoveredState;
    private RectangleF _relativeLayout;

    /// <summary>
    ///     Create a new button
    /// </summary>
    /// <param name="layout">Layout of the button</param>
    /// <param name="content">Optional string to display inside of the button</param>
    /// <param name="desiredFontSize">Font size of the optional string</param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public Button(RectangleF layout, string content = "", ushort desiredFontSize = ushort.MaxValue) : base(layout)
    {
        _content = content;
        _desiredFontSize = desiredFontSize;
    }

    /// <summary>
    ///     Text box which lives inside the button if a string button content is provided
    /// </summary>
    public TextBox? TextBox { get; private set; }


    /// <inheritdoc />
    public override Image<Rgba32>? Image => _image;

    /// <summary>
    ///     Callback if the button is clicked
    /// </summary>
    public event Action OnLeftClickCb = delegate { };

    /// <inheritdoc />
    public override void Initialize()
    {
        _image = BorderBuilder.BuildBorderedImage((int) PixelSize.Width, (int) PixelSize.Height,
            Color.Transparent, out _innerLayout);
        _relativeLayout = new RectangleF
        {
            X = _innerLayout.X / PixelSize.Width,
            Y = _innerLayout.Y / PixelSize.Height,
            Width = _innerLayout.Width / PixelSize.Width,
            Height = _innerLayout.Height / PixelSize.Height
        };

        if (_content.Length != 0)
            TextBox = new TextBox(_relativeLayout, _content, FontIDs.Akashi, useBorder: false,
                desiredFontSize: _desiredFontSize)
            {
                Parent = this
            };

        HasChanged = true;
        TextBox?.Initialize();
    }

    /// <inheritdoc />
    public override void Resize()
    {
        _image?.Dispose();
        TextBox?.Dispose();
        TextBox = null;
        Initialize();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        HasChanged = _lastHoveredState != CursorHovering;
        _lastHoveredState = CursorHovering;

        var childChanged = TextBox?.HasChanged ?? false;

        if (!HasChanged && !childChanged) return;

        if (TextBox is not null)
        {
            TextBox.DrawColor = CursorHovering ? Color.Green : Color.White;
            TextBox.Update(deltaTime);
            _image.Mutate(context => { context.DrawImage(TextBox.Image, (Point) _innerLayout.Location, 1); });
        }
        else
        {
            _image.Mutate(context => { context.Fill(CursorHovering ? Color.LightGray : Color.Gray, _innerLayout); });
        }
    }


    /// <inheritdoc />
    public override void OnLeftClick()
    {
        if (!CursorHovering) return;
        OnLeftClickCb();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        TextBox?.Dispose();
        base.Dispose();
        _image?.Dispose();
    }
}