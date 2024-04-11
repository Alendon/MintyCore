using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyCore.Utils.Events;
using Silk.NET.GLFW;

namespace MintyCore.Input;

/// <summary>
/// Key event which is triggered when a key is pressed, released or repeated
/// </summary>
/// <param name="PhysicalKey"> The physical key that was pressed </param>
/// <param name="Action"> The action that was performed on the key </param>
/// <param name="KeyModifiers"> The modifiers that were active when the key was pressed </param>
/// <param name="LocalizedKeyRep"> The localized representation of the key eg. "y" for US, "z" for DE </param>
/// <param name="Scancode"> The scancode of the key </param>
[RegisterEvent("key")]
public record struct KeyEvent(
    Key PhysicalKey,
    InputAction Action,
    KeyModifiers KeyModifiers,
    string? LocalizedKeyRep,
    int Scancode) : IEvent
{
    /// <inheritdoc />
    public static Identification Identification => EventIDs.Key;

    /// <inheritdoc />
    public static bool ModificationAllowed => false;
}

/// <summary>
/// Represents a character input event.
/// </summary>
/// <param name="Character">The character that was input.</param>
[RegisterEvent("char")]
public record struct CharEvent(char Character) : IEvent
{
    /// <inheritdoc />
    public static Identification Identification => EventIDs.Char;

    /// <inheritdoc />
    public static bool ModificationAllowed => false;
}

/// <summary>
/// Represents a mouse button event.
/// </summary>
/// <param name="Button">The mouse button that triggered the event.</param>
/// <param name="Action">The action that was performed on the mouse button.</param>
/// <param name="Mods">The modifiers that were active when the mouse button event occurred.</param>
[RegisterEvent("mouse_button")]
public record struct MouseButtonEvent(MouseButton Button, InputAction Action, KeyModifiers Mods) : IEvent
{
    /// <inheritdoc />
    public static Identification Identification => EventIDs.MouseButton;

    /// <inheritdoc />
    public static bool ModificationAllowed => false;
}

/// <summary>
/// Represents a cursor enter event.
/// This event is triggered when the cursor enters or leaves the window.
/// </summary>
/// <param name="Entered">A boolean indicating whether the cursor entered (true) or left (false) the window.</param>
[RegisterEvent("cursor_enter")]
public record struct CursorEnterEvent(bool Entered) : IEvent
{
    /// <inheritdoc />
    public static Identification Identification => EventIDs.CursorEnter;

    /// <inheritdoc />
    public static bool ModificationAllowed => false;
}

/// <summary>
/// Represents a cursor position event.
/// This event is triggered when the cursor is moved within the window.
/// </summary>
/// <param name="X">The new X-coordinate of the cursor.</param>
/// <param name="Y">The new Y-coordinate of the cursor.</param>
[RegisterEvent("cursor_pos")]
public record struct CursorPosEvent(double X, double Y) : IEvent
{
    /// <inheritdoc />
    public static Identification Identification => EventIDs.CursorPos;

    /// <inheritdoc />
    public static bool ModificationAllowed => false;
}

/// <summary>
/// Represents a scroll event.
/// This event is triggered when the scroll wheel is used.
/// </summary>
/// <param name="OffsetX">The horizontal scroll offset.</param>
/// <param name="OffsetY">The vertical scroll offset.</param>
[RegisterEvent("scroll")]
public record struct ScrollEvent(double OffsetX, double OffsetY) : IEvent
{
    /// <inheritdoc />
    public static Identification Identification => EventIDs.Scroll;

    /// <inheritdoc />
    public static bool ModificationAllowed => false;
}