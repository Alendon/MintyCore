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
public class TextField : TextBox
{
    private readonly TextInput _textInput;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="layout"></param>
    /// /// <param name="fontFamilyId">The font family to use for rendering</param>
    /// <param name="desiredFontSize">The desired size of the font used.</param>
    /// <param name="useBorder">Whether or not a border should be drawn around the element</param>
    /// <param name="horizontalAlignment">Which horizontal alignment the text should use</param>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public TextField(Layout layout, Identification fontFamilyId, ushort desiredFontSize = ushort.MaxValue,
        bool useBorder = true, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center) : base(layout,
        "To Measure |", fontFamilyId, desiredFontSize, useBorder, horizontalAlignment)
    {
        _textInput = new TextInput(false);
        FillColor = Color.Gray;
    }

    /// <inheritdoc />
    public override void OnLeftClick()
    {
        var oldState = _textInput.IsActive;
        _textInput.IsActive = CursorHovering;
        if (_textInput.IsActive == oldState) return;
        FillColor = _textInput.IsActive ? Color.DarkGray : Color.Gray;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var currentInput = _textInput.ToString();
        if (!Content.Equals(currentInput))
        {
            Content = currentInput;
        }
        base.Update(deltaTime);
    }
}