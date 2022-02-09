using System;
using MintyCore.Identifications;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

/// <summary>
/// Simple button ui element
/// </summary>
public class Button : Element
{
    private Image<Rgba32> _image;

    public TextBox? TextBox { get; private set; }

    private string _content;
    private bool _lastHoveredState;
    private RectangleF _innerLayout;
    private RectangleF _relativeLayout;

    /// <summary>
    /// Callback if the button is clicked
    /// </summary>
    public event Action OnLeftClickCb = delegate { };

    /// <summary>
    /// Create a new button
    /// </summary>
    /// <param name="layout">Layout of the button</param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public Button(RectangleF layout, string content = "") : base(layout)
    {
        _content = content;
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        _image = BorderBuilder.BuildBorderedImage((int)PixelSize.Width, (int)PixelSize.Height,
            Color.Transparent, out _innerLayout);
        _relativeLayout = new RectangleF
        {
            X = _innerLayout.X / PixelSize.Width,
            Y = _innerLayout.Y / PixelSize.Height,
            Width = _innerLayout.Width / PixelSize.Width,
            Height = _innerLayout.Height / PixelSize.Height
        };

        if (_content.Length != 0)
        {
            TextBox = new TextBox(_relativeLayout, _content, FontIDs.Akashi, useBorder: false)
            {
                Parent = this
            };
        }

        HasChanged = true;
        TextBox?.Initialize();
    }

    /// <inheritdoc />
    public override void Resize()
    {
        _image.Dispose();
        TextBox?.Dispose();
        TextBox = null;
        Initialize();
    }


    /// <inheritdoc />
    public override Image<Rgba32> Image => _image;

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
            _image.Mutate(context => { context.DrawImage(TextBox.Image, (Point)_innerLayout.Location, 1); });
        }
        else
        {
            if (!CursorHovering)
            {
                
            }
            _image.Mutate(context =>
            {
                context.Fill(CursorHovering ? Color.LightGray : Color.Gray, _innerLayout);
            });
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
        TextBox?.Dispose();
        base.Dispose();
        _image.Dispose();
    }
}