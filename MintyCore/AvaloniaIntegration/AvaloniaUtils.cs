using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Silk.NET.GLFW;
using KeyModifiers = Silk.NET.GLFW.KeyModifiers;

namespace MintyCore.AvaloniaIntegration;

/// <summary>
/// Utility methods for working with Avalonia.
/// </summary>
public static class AvaloniaUtils
{
    /// <summary>
    /// Convert a <see cref="InputAction"/> to a <see cref="RawKeyEventType"/>.
    /// </summary>
    /// <param name="inputAction"></param>
    /// <returns></returns>
    public static RawKeyEventType ToAvalonia(this InputAction inputAction)
    {
        return inputAction switch
        {
            InputAction.Press => RawKeyEventType.KeyDown,
            InputAction.Release => RawKeyEventType.KeyUp,
            InputAction.Repeat => RawKeyEventType.KeyDown,
            _ => throw new ArgumentOutOfRangeException(nameof(inputAction), inputAction, null)
        };
    }

    /// <summary>
    ///  Convert a <see cref="Key"/> to a <see cref="Avalonia.Input.Key"/>.
    /// </summary>
    /// <param name="key"> The key to convert. </param>
    /// <param name="keyRep"> The key string representation of the current keyboard layout. </param>
    public static Avalonia.Input.Key ToLogicalAvaloniaKey(this Key key, string? keyRep)
    {
        return TryGetLogicalKey(keyRep, out var logicalKey) ? logicalKey : key.ToAvalonia().ToQwertyKey();
    }

    /// <summary>
    ///  Try to get the logical key from the key representation.
    /// </summary>
    public static bool TryGetLogicalKey(string? keyRep, out Avalonia.Input.Key logicalKey)
    {
        //currently only the default characters a-z can be remapped to their logical key

        logicalKey = Avalonia.Input.Key.None;

        if (keyRep is not { Length: 1 })
            return false;

        var c = keyRep[0];
        if (c is < 'a' or > 'z')
            return false;

        logicalKey = (Avalonia.Input.Key)(c - 'a' + (int)Avalonia.Input.Key.A);

        return true;
    }

    /// <summary>
    ///  Convert a <see cref="Key"/> to a <see cref="PhysicalKey"/>.
    /// </summary>
    public static PhysicalKey ToAvalonia(this Key key)
    {
        return key switch
        {
            Key.Unknown => PhysicalKey.None,
            Key.Space => PhysicalKey.Space,
            Key.Apostrophe => PhysicalKey.Quote,
            Key.Comma => PhysicalKey.Comma,
            Key.Minus => PhysicalKey.Minus,
            Key.Period => PhysicalKey.Period,
            Key.Slash => PhysicalKey.Slash,
            Key.Number0 => PhysicalKey.Digit0,
            Key.Number1 => PhysicalKey.Digit1,
            Key.Number2 => PhysicalKey.Digit2,
            Key.Number3 => PhysicalKey.Digit3,
            Key.Number4 => PhysicalKey.Digit4,
            Key.Number5 => PhysicalKey.Digit5,
            Key.Number6 => PhysicalKey.Digit6,
            Key.Number7 => PhysicalKey.Digit7,
            Key.Number8 => PhysicalKey.Digit8,
            Key.Number9 => PhysicalKey.Digit9,
            Key.Semicolon => PhysicalKey.Semicolon,
            Key.Equal => PhysicalKey.Equal,
            Key.A => PhysicalKey.A,
            Key.B => PhysicalKey.B,
            Key.C => PhysicalKey.C,
            Key.D => PhysicalKey.D,
            Key.E => PhysicalKey.E,
            Key.F => PhysicalKey.F,
            Key.G => PhysicalKey.G,
            Key.H => PhysicalKey.H,
            Key.I => PhysicalKey.I,
            Key.J => PhysicalKey.J,
            Key.K => PhysicalKey.K,
            Key.L => PhysicalKey.L,
            Key.M => PhysicalKey.M,
            Key.N => PhysicalKey.N,
            Key.O => PhysicalKey.O,
            Key.P => PhysicalKey.P,
            Key.Q => PhysicalKey.Q,
            Key.R => PhysicalKey.R,
            Key.S => PhysicalKey.S,
            Key.T => PhysicalKey.T,
            Key.U => PhysicalKey.U,
            Key.V => PhysicalKey.V,
            Key.W => PhysicalKey.W,
            Key.X => PhysicalKey.X,
            Key.Y => PhysicalKey.Y,
            Key.Z => PhysicalKey.Z,
            Key.LeftBracket => PhysicalKey.BracketLeft,
            Key.BackSlash => PhysicalKey.Backslash,
            Key.RightBracket => PhysicalKey.BracketRight,
            Key.GraveAccent => PhysicalKey.Backquote,
            Key.Escape => PhysicalKey.Escape,
            Key.Enter => PhysicalKey.Enter,
            Key.Tab => PhysicalKey.Tab,
            Key.Backspace => PhysicalKey.Backspace,
            Key.Insert => PhysicalKey.Insert,
            Key.Delete => PhysicalKey.Delete,
            Key.Right => PhysicalKey.ArrowRight,
            Key.Left => PhysicalKey.ArrowLeft,
            Key.Down => PhysicalKey.ArrowDown,
            Key.Up => PhysicalKey.ArrowUp,
            Key.PageUp => PhysicalKey.PageUp,
            Key.PageDown => PhysicalKey.PageDown,
            Key.Home => PhysicalKey.Home,
            Key.End => PhysicalKey.End,
            Key.CapsLock => PhysicalKey.CapsLock,
            Key.ScrollLock => PhysicalKey.ScrollLock,
            Key.NumLock => PhysicalKey.NumLock,
            Key.PrintScreen => PhysicalKey.PrintScreen,
            Key.Pause => PhysicalKey.Pause,
            Key.F1 => PhysicalKey.F1,
            Key.F2 => PhysicalKey.F2,
            Key.F3 => PhysicalKey.F3,
            Key.F4 => PhysicalKey.F4,
            Key.F5 => PhysicalKey.F5,
            Key.F6 => PhysicalKey.F6,
            Key.F7 => PhysicalKey.F7,
            Key.F8 => PhysicalKey.F8,
            Key.F9 => PhysicalKey.F9,
            Key.F10 => PhysicalKey.F10,
            Key.F11 => PhysicalKey.F11,
            Key.F12 => PhysicalKey.F12,
            Key.F13 => PhysicalKey.F13,
            Key.F14 => PhysicalKey.F14,
            Key.F15 => PhysicalKey.F15,
            Key.F16 => PhysicalKey.F16,
            Key.F17 => PhysicalKey.F17,
            Key.F18 => PhysicalKey.F18,
            Key.F19 => PhysicalKey.F19,
            Key.F20 => PhysicalKey.F20,
            Key.F21 => PhysicalKey.F21,
            Key.F22 => PhysicalKey.F22,
            Key.F23 => PhysicalKey.F23,
            Key.F24 => PhysicalKey.F24,
            Key.Keypad0 => PhysicalKey.NumPad0,
            Key.Keypad1 => PhysicalKey.NumPad1,
            Key.Keypad2 => PhysicalKey.NumPad2,
            Key.Keypad3 => PhysicalKey.NumPad3,
            Key.Keypad4 => PhysicalKey.NumPad4,
            Key.Keypad5 => PhysicalKey.NumPad5,
            Key.Keypad6 => PhysicalKey.NumPad6,
            Key.Keypad7 => PhysicalKey.NumPad7,
            Key.Keypad8 => PhysicalKey.NumPad8,
            Key.Keypad9 => PhysicalKey.NumPad9,
            Key.KeypadDecimal => PhysicalKey.NumPadDecimal,
            Key.KeypadDivide => PhysicalKey.NumPadDivide,
            Key.KeypadMultiply => PhysicalKey.NumPadMultiply,
            Key.KeypadSubtract => PhysicalKey.NumPadSubtract,
            Key.KeypadAdd => PhysicalKey.NumPadAdd,
            Key.KeypadEnter => PhysicalKey.NumPadEnter,
            Key.KeypadEqual => PhysicalKey.NumPadEqual,
            Key.ShiftLeft => PhysicalKey.ShiftLeft,
            Key.ControlLeft => PhysicalKey.ControlLeft,
            Key.AltLeft => PhysicalKey.AltLeft,
            Key.SuperLeft => PhysicalKey.MetaLeft,
            Key.ShiftRight => PhysicalKey.ShiftRight,
            Key.ControlRight => PhysicalKey.ControlRight,
            Key.AltRight => PhysicalKey.AltRight,
            Key.SuperRight => PhysicalKey.MetaRight,
            Key.Menu => PhysicalKey.ContextMenu,
            _ => PhysicalKey.None
        };
    }


    /// <summary>
    ///  Convert a <see cref="KeyModifiers"/> to a <see cref="RawInputModifiers"/>.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags", Justification = "Flag attribute is missing")]
    public static RawInputModifiers ToAvalonia(this KeyModifiers key)
    {
        var modifiers = RawInputModifiers.None;

        if ((key & KeyModifiers.Alt) != 0)
            modifiers |= RawInputModifiers.Alt;
        if ((key & KeyModifiers.Control) != 0)
            modifiers |= RawInputModifiers.Control;
        if ((key & KeyModifiers.Shift) != 0)
            modifiers |= RawInputModifiers.Shift;
        if ((key & KeyModifiers.Super) != 0)
            modifiers |= RawInputModifiers.Meta;

        return modifiers;
    }

    /// <summary>
    /// Get the <see cref="RawPointerEventArgs"/> from a <see cref="MouseButton"/> and <see cref="InputAction"/>.
    /// </summary>
    /// <param name="button"></param>
    /// <param name="action"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryGetRawPointerEventType(MouseButton button, InputAction action, out RawPointerEventType result)
    {
        result = default;

        switch (action)
        {
            case InputAction.Press:
                switch (button)
                {
                    case MouseButton.Left:
                        result = RawPointerEventType.LeftButtonDown;
                        break;
                    case MouseButton.Right:
                        result = RawPointerEventType.RightButtonDown;
                        break;
                    case MouseButton.Middle:
                        result = RawPointerEventType.MiddleButtonDown;
                        break;
                    default:
                        return false;
                }

                break;
            case InputAction.Release:
                switch (button)
                {
                    case MouseButton.Left:
                        result = RawPointerEventType.LeftButtonUp;
                        break;
                    case MouseButton.Right:
                        result = RawPointerEventType.RightButtonUp;
                        break;
                    case MouseButton.Middle:
                        result = RawPointerEventType.MiddleButtonUp;
                        break;
                    default:
                        return false;
                }

                break;
            case InputAction.Repeat:
                return false;
            default:
                return false;
        }

        return true;
    }
    
}