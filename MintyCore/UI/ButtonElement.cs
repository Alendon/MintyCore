using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.UI;

/// <summary>
/// Simple button ui element
/// </summary>
public class ButtonElement : Element
{
    private Image<Rgba32> _normal;
    private Image<Rgba32> _hovered;

    /// <summary>
    /// Callback if the button is clicked
    /// </summary>
    public event Action OnLeftClickCb = delegate { };

    /// <summary>
    /// Create a new button
    /// </summary>
    /// <param name="layout">Layout of the button</param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public ButtonElement(Layout layout) : base(layout)
    {
        
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        _normal = BorderBuilder.BuildBorderedImage((int)PixelSize.X, (int)PixelSize.Y,
            new Rgba32(0, 0, 0, byte.MaxValue), out _);
        _hovered = BorderBuilder.BuildBorderedImage((int)PixelSize.X, (int)PixelSize.Y,
            new Rgba32(128, 128, 128, byte.MaxValue), out _);
        HasChanged = true;
    }

    /// <inheritdoc />
    public override void Resize()
    {
        _normal.Dispose();
        _hovered.Dispose();
        Initialize();
    }

    private bool _lastHoveredState;

    /// <inheritdoc />
    public override Image<Rgba32> Image => CursorHovering ? _hovered : _normal;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        HasChanged = _lastHoveredState != CursorHovering;
        _lastHoveredState = CursorHovering;
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
        base.Dispose();
        _hovered.Dispose();
        _normal.Dispose();
    }
}