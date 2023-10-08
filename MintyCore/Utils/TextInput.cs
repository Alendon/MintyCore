using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Silk.NET.Input;
using TextCopy;

namespace MintyCore.Utils;

/// <summary>
///     Simple class to handle text input
/// </summary>
[PublicAPI]
public class TextInput : IDisposable
{
    private List<char> _characters = new();
    private int _selectionLength;

    private InputHandler _inputHandler;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="multilineEnable"></param>
    public TextInput(bool multilineEnable, InputHandler inputHandler)
    {
        _inputHandler = inputHandler;
        MultiLineEnable = multilineEnable;

        _inputHandler.AddOnCharReceived(OnCharReceived);
        _inputHandler.AddOnKeyDown(OnKeyReceived);
        _inputHandler.AddOnKeyRepeat(OnKeyReceived);
    }


    /// <summary>
    ///     Current cursor position
    /// </summary>
    public int CursorPosition { get; private set; }

    /// <summary>
    ///     Get/Set whether or not the input is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    ///     Get whether or not multiline input is enabled
    /// </summary>
    public bool MultiLineEnable { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _inputHandler.RemoveOnCharReceived(OnCharReceived);
        _inputHandler.RemoveOnKeyDown(OnKeyReceived);
        _inputHandler.RemoveOnKeyRepeat(OnKeyReceived);
    }

    /// <summary>
    ///     Set the new cursor position
    /// </summary>
    /// <param name="newPosition"></param>
    /// <param name="setSelectionLength">Whether or not the selection should be updated</param>
    public void SetCursorPosition(int newPosition, bool setSelectionLength = true)
    {
        if (newPosition < 0) newPosition = 0;
        if (newPosition > _characters.Count) newPosition = _characters.Count;
        if (ShiftKey() && setSelectionLength)
        {
            var selectionDelta = CursorPosition - newPosition;
            _selectionLength += selectionDelta;
        }

        CursorPosition = newPosition;
    }

    /// <summary>
    ///     Triggered when Enter is pressed
    /// </summary>
    public event Action? OnEnterCallback;

    private void OnKeyReceived(Key key)
    {
        //TODO: Check down times (key repeat) for consistent input behavior

        if (!IsActive) return;

        switch (key)
        {
            case Key.X:
            {
                if (!ControlKey() || _selectionLength == 0) break;
                var start = CursorPosition;
                var length = Math.Abs(_selectionLength);
                if (_selectionLength < 0) start += _selectionLength;

                var text = new string(_characters.ToArray().AsSpan(start, length));
                ClipboardService.SetText(text);

                DeleteSelected();
                break;
            }
            case Key.C:
            {
                if (!ControlKey() || _selectionLength == 0) break;
                var start = CursorPosition;
                var length = Math.Abs(_selectionLength);
                if (_selectionLength < 0) start += _selectionLength;

                var text = new string(_characters.ToArray().AsSpan(start, length));
                ClipboardService.SetText(text);
                break;
            }
            case Key.V:
            {
                if (!ControlKey()) break;
                var text = ClipboardService.GetText();
                if (string.IsNullOrEmpty(text)) break;
                foreach (var c in text.Where(c => c != '\n' || MultiLineEnable))
                {
                    _characters.Insert(CursorPosition, c);
                    SetCursorPosition(CursorPosition + 1, false);
                }

                break;
            }
            case Key.A:
            {
                if (!ControlKey()) break;
                SetCursorPosition(0);
                _selectionLength = _characters.Count;
                break;
            }
            case Key.Backspace:
            {
                if (_selectionLength != 0)
                {
                    DeleteSelected();
                    break;
                }

                if (CursorPosition != 0)
                {
                    _characters.RemoveAt(CursorPosition - 1);
                    SetCursorPosition(CursorPosition - 1);
                }

                break;
            }
            case Key.Delete:
            {
                if (_selectionLength != 0)
                {
                    DeleteSelected();
                    break;
                }

                if (CursorPosition < _characters.Count) _characters.RemoveAt(CursorPosition);

                break;
            }
            case Key.Enter:
            {
                if (!ShiftKey() && OnEnterCallback is not null)
                {
                    OnEnterCallback();
                    break;
                }

                NewLine();
                break;
            }
            case Key.Left:
            {
                if (!ShiftKey() && _selectionLength != 0)
                {
                    if (_selectionLength < 0) SetCursorPosition(CursorPosition + _selectionLength, false);

                    _selectionLength = 0;
                    break;
                }

                SetCursorPosition(CursorPosition - 1);
                break;
            }
            case Key.Right:
            {
                if (!ShiftKey() && _selectionLength != 0)
                {
                    if (_selectionLength > 0) SetCursorPosition(CursorPosition + _selectionLength, false);

                    _selectionLength = 0;
                    break;
                }

                SetCursorPosition(CursorPosition + 1);
                break;
            }
            case Key.Unknown:
                break;
            case Key.Space:
                break;
            case Key.Apostrophe:
                break;
            case Key.Comma:
                break;
            case Key.Minus:
                break;
            case Key.Period:
                break;
            case Key.Slash:
                break;
            case Key.Number0:
                break;
            case Key.Number1:
                break;
            case Key.Number2:
                break;
            case Key.Number3:
                break;
            case Key.Number4:
                break;
            case Key.Number5:
                break;
            case Key.Number6:
                break;
            case Key.Number7:
                break;
            case Key.Number8:
                break;
            case Key.Number9:
                break;
            case Key.Semicolon:
                break;
            case Key.Equal:
                break;
            case Key.B:
                break;
            case Key.D:
                break;
            case Key.E:
                break;
            case Key.F:
                break;
            case Key.G:
                break;
            case Key.H:
                break;
            case Key.I:
                break;
            case Key.J:
                break;
            case Key.K:
                break;
            case Key.L:
                break;
            case Key.M:
                break;
            case Key.N:
                break;
            case Key.O:
                break;
            case Key.P:
                break;
            case Key.Q:
                break;
            case Key.R:
                break;
            case Key.S:
                break;
            case Key.T:
                break;
            case Key.U:
                break;
            case Key.W:
                break;
            case Key.Y:
                break;
            case Key.Z:
                break;
            case Key.LeftBracket:
                break;
            case Key.BackSlash:
                break;
            case Key.RightBracket:
                break;
            case Key.GraveAccent:
                break;
            case Key.World1:
                break;
            case Key.World2:
                break;
            case Key.Escape:
                break;
            case Key.Tab:
                break;
            case Key.Insert:
                break;
            case Key.Down:
                break;
            case Key.Up:
                break;
            case Key.PageUp:
                break;
            case Key.PageDown:
                break;
            case Key.Home:
                break;
            case Key.End:
                break;
            case Key.CapsLock:
                break;
            case Key.ScrollLock:
                break;
            case Key.NumLock:
                break;
            case Key.PrintScreen:
                break;
            case Key.Pause:
                break;
            case Key.F1:
                break;
            case Key.F2:
                break;
            case Key.F3:
                break;
            case Key.F4:
                break;
            case Key.F5:
                break;
            case Key.F6:
                break;
            case Key.F7:
                break;
            case Key.F8:
                break;
            case Key.F9:
                break;
            case Key.F10:
                break;
            case Key.F11:
                break;
            case Key.F12:
                break;
            case Key.F13:
                break;
            case Key.F14:
                break;
            case Key.F15:
                break;
            case Key.F16:
                break;
            case Key.F17:
                break;
            case Key.F18:
                break;
            case Key.F19:
                break;
            case Key.F20:
                break;
            case Key.F21:
                break;
            case Key.F22:
                break;
            case Key.F23:
                break;
            case Key.F24:
                break;
            case Key.F25:
                break;
            case Key.Keypad0:
                break;
            case Key.Keypad1:
                break;
            case Key.Keypad2:
                break;
            case Key.Keypad3:
                break;
            case Key.Keypad4:
                break;
            case Key.Keypad5:
                break;
            case Key.Keypad6:
                break;
            case Key.Keypad7:
                break;
            case Key.Keypad8:
                break;
            case Key.Keypad9:
                break;
            case Key.KeypadDecimal:
                break;
            case Key.KeypadDivide:
                break;
            case Key.KeypadMultiply:
                break;
            case Key.KeypadSubtract:
                break;
            case Key.KeypadAdd:
                break;
            case Key.KeypadEnter:
                break;
            case Key.KeypadEqual:
                break;
            case Key.ShiftLeft:
                break;
            case Key.ControlLeft:
                break;
            case Key.AltLeft:
                break;
            case Key.SuperLeft:
                break;
            case Key.ShiftRight:
                break;
            case Key.ControlRight:
                break;
            case Key.AltRight:
                break;
            case Key.SuperRight:
                break;
            case Key.Menu:
                break;
            default: return;
        }
    }

    private void NewLine()
    {
        if (MultiLineEnable)
            WriteChar('\n');
    }

    private void OnCharReceived(char character)
    {
        if (IsActive) WriteChar(character);
    }

    private void WriteChar(char character)
    {
        if (_selectionLength != 0) DeleteSelected();

        _characters.Insert(CursorPosition, character);
        SetCursorPosition(CursorPosition + 1, false);
    }

    private void DeleteSelected()
    {
        var length = _selectionLength;
        var start = CursorPosition;
        if (length < 0)
        {
            length = Math.Abs(length);
            start -= length;
        }

        _characters.RemoveRange(start, length);
        SetCursorPosition(CursorPosition + _selectionLength, false);
        _selectionLength = 0;
    }

    private bool ControlKey()
    {
        return _inputHandler.GetKeyDown(Key.ControlLeft) || _inputHandler.GetKeyDown(Key.ControlRight);
    }

    private bool ShiftKey()
    {
        return _inputHandler.GetKeyDown(Key.ShiftLeft) || _inputHandler.GetKeyDown(Key.ShiftRight);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return new string(_characters.ToArray());
    }

    /// <summary>
    ///     Set the input text
    /// </summary>
    /// <param name="value"></param>
    public void SetText(string value)
    {
        _characters = new List<char>(value);
        CursorPosition = Math.Min(CursorPosition, _characters.Count);
    }
}