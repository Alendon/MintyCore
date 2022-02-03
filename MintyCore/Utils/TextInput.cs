using System;
using System.Collections.Generic;
using Silk.NET.Input;
using TextCopy;

namespace MintyCore.Utils;

/// <summary>
/// Simple class to handle text input
/// </summary>
public class TextInput : IDisposable
{
    private int _cursorPosition;

    /// <summary>
    /// Current cursor position
    /// </summary>
    public int CursorPosition
    {
        get => _cursorPosition;
    }

    /// <summary>
    /// Set the new cursor position
    /// </summary>
    /// <param name="newPosition"></param>
    /// <param name="setSelectionLength">Whether or not the selection should be updated</param>
    public void SetCursorPosition(int newPosition, bool setSelectionLength = true)
    {
        if (newPosition < 0) newPosition = 0;
        if (newPosition > characters.Count) newPosition = characters.Count;
        if (ShiftKey() && setSelectionLength)
        {
            var selectionDelta = _cursorPosition - newPosition;
            _selectionLength += selectionDelta;
        }

        _cursorPosition = newPosition;
    }

    /// <summary>
    /// Triggered when Enter is pressed
    /// </summary>
    public event Action? OnEnterCallback;

    /// <summary>
    /// Get/Set whether or not the input is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Get whether or not multiline input is enabled
    /// </summary>
    public bool MultiLineEnable { get; }

    private List<char> characters = new();
    private int _selectionLength;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="multilineEnable"></param>
    public TextInput(bool multilineEnable)
    {
        MultiLineEnable = multilineEnable;


        InputHandler.OnCharReceived += OnCharReceived;
        InputHandler.OnKeyPressed += OnKeyReceived;
        InputHandler.OnKeyRepeat += OnKeyReceived;
    }

    private void OnKeyReceived(Key key)
    {
        if (!IsActive) return;

        switch (key)
        {
            case Key.X:
            {
                if (!ControlKey() || _selectionLength == 0) break;
                int start = _cursorPosition;
                int length = Math.Abs(_selectionLength);
                if (_selectionLength < 0)
                {
                    start += _selectionLength;
                }

                var text = new string(characters.ToArray().AsSpan(start, length));
                ClipboardService.SetText(text);

                DeleteSelected();
                break;
            }
            case Key.C:
            {
                if (!ControlKey() || _selectionLength == 0) break;
                int start = _cursorPosition;
                int length = Math.Abs(_selectionLength);
                if (_selectionLength < 0)
                {
                    start += _selectionLength;
                }

                var text = new string(characters.ToArray().AsSpan(start, length));
                ClipboardService.SetText(text);
                break;
            }
            case Key.V:
            {
                if (!ControlKey()) break;
                var text = ClipboardService.GetText();
                if (string.IsNullOrEmpty(text)) break;
                foreach (var c in text)
                {
                    if (c == '\n' && !MultiLineEnable) continue;
                    characters.Insert(CursorPosition, c);
                    SetCursorPosition(CursorPosition + 1, false);
                }

                break;
            }
            case Key.A:
            {
                if (!ControlKey()) break;
                SetCursorPosition(0);
                _selectionLength = characters.Count;
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
                    characters.RemoveAt(CursorPosition - 1);
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

                if (CursorPosition < characters.Count)
                {
                    characters.RemoveAt(CursorPosition);
                }

                break;
            }
            case Key.Enter:
            {
                if (OnEnterCallback is not null && !ShiftKey())
                {
                    OnEnterCallback?.Invoke();
                    break;
                }

                NewLine();
                break;
            }
            case Key.Left:
            {
                if (!ShiftKey() && _selectionLength != 0)
                {
                    if (_selectionLength < 0)
                    {
                        SetCursorPosition(CursorPosition + _selectionLength, false);
                    }

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
                    if (_selectionLength > 0)
                    {
                        SetCursorPosition(CursorPosition + _selectionLength, false);
                    }

                    _selectionLength = 0;
                    break;
                }

                SetCursorPosition(CursorPosition + 1);
                break;
            }
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
        if (_selectionLength != 0)
        {
            DeleteSelected();
        }

        characters.Insert(CursorPosition, character);
        SetCursorPosition(CursorPosition + 1, false);
    }

    private void DeleteSelected()
    {
        int length = _selectionLength;
        int start = _cursorPosition;
        if (length < 0)
        {
            length = Math.Abs(length);
            start -= length;
        }

        characters.RemoveRange(start, length);
        SetCursorPosition(CursorPosition + _selectionLength, false);
        _selectionLength = 0;
    }

    private bool ControlKey()
    {
        return InputHandler.GetKeyDown(Key.ControlLeft) || InputHandler.GetKeyDown(Key.ControlRight);
    }

    private bool ShiftKey()
    {
        return InputHandler.GetKeyDown(Key.ShiftLeft) || InputHandler.GetKeyDown(Key.ShiftRight);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        InputHandler.OnCharReceived -= OnCharReceived;
        InputHandler.OnKeyPressed -= OnKeyReceived;
        InputHandler.OnKeyRepeat -= OnKeyReceived;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return new string(characters.ToArray());
    }
}