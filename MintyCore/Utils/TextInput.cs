using System;
using System.Collections.Generic;
using System.Text;
using Silk.NET.Input;
using TextCopy;

namespace MintyCore.Utils;

public class TextInput : IDisposable
{
    private int _cursorPosition;

    public int CursorPosition
    {
        get => _cursorPosition;
    }

    public void SetCursorPosition(int newPosition, bool setSelectionLength = true)
    {
        if (newPosition < 0) newPosition = 0;
        if (newPosition > characters.Count) newPosition = characters.Count;
        if (ShiftKey() && setSelectionLength)
        {
            var selectionDelta = _cursorPosition - newPosition;
            selectionLength += selectionDelta;
        }

        _cursorPosition = newPosition;
    }

    public event Action? OnEnterCallback;

    public bool IsActive { get; set; } = true;
    public bool MultiLineEnable { get; }
    private List<char> characters = new();
    private int selectionLength;

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
                if (!ControlKey() || selectionLength == 0) break;
                int start = _cursorPosition;
                int length = Math.Abs(selectionLength);
                if (selectionLength < 0)
                {
                    start += selectionLength;
                }

                var text = new string(characters.ToArray().AsSpan(start, length));
                ClipboardService.SetText(text);

                DeleteSelected();
                break;
            }
            case Key.C:
            {
                if (!ControlKey() || selectionLength == 0) break;
                int start = _cursorPosition;
                int length = Math.Abs(selectionLength);
                if (selectionLength < 0)
                {
                    start += selectionLength;
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
                selectionLength = characters.Count;
                break;
            }
            case Key.Backspace:
            {
                if (selectionLength != 0)
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
                if (selectionLength != 0)
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
                if (!ShiftKey() && selectionLength != 0)
                {
                    if (selectionLength < 0)
                    {
                        SetCursorPosition(CursorPosition + selectionLength, false);
                    }

                    selectionLength = 0;
                    break;
                }

                SetCursorPosition(CursorPosition - 1);
                break;
            }
            case Key.Right:
            {
                if (!ShiftKey() && selectionLength != 0)
                {
                    if (selectionLength > 0)
                    {
                        SetCursorPosition(CursorPosition + selectionLength, false);
                    }

                    selectionLength = 0;
                    break;
                }

                SetCursorPosition(CursorPosition + 1);
                break;
            }
            default: return;
        }

        Console.WriteLine(this);
    }

    private void NewLine()
    {
        if (MultiLineEnable)
            WriteChar('\n');
    }

    private void OnCharReceived( char character)
    {
        if (IsActive) WriteChar(character);
        Console.WriteLine(this);
    }

    private void WriteChar(char character)
    {
        if (selectionLength != 0)
        {
            DeleteSelected();
        }

        characters.Insert(CursorPosition, character);
        SetCursorPosition(CursorPosition + 1, false);
    }

    private void DeleteSelected()
    {
        int length = selectionLength;
        int start = _cursorPosition;
        if (length < 0)
        {
            length = Math.Abs(length);
            start -= length;
        }

        characters.RemoveRange(start, length);
        SetCursorPosition(CursorPosition + selectionLength, false);
        selectionLength = 0;
    }

    private bool ControlKey()
    {
        return InputHandler.GetKeyDown(Key.ControlLeft) || InputHandler.GetKeyDown(Key.ControlRight);
    }

    private bool ShiftKey()
    {
        return InputHandler.GetKeyDown(Key.ShiftLeft) || InputHandler.GetKeyDown(Key.ShiftRight);
    }

    public void Dispose()
    {
        InputHandler.OnCharReceived -= OnCharReceived;
        InputHandler.OnKeyPressed -= OnKeyReceived;
        InputHandler.OnKeyRepeat -= OnKeyReceived;
    }

    public override string ToString()
    {
        return new string(characters.ToArray());
    }
}