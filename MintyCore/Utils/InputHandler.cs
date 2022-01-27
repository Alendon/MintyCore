using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Silk.NET.Input;

namespace MintyCore.Utils;

/// <summary>
///     Class to manage user input
/// </summary>
public static class InputHandler
{
    private static readonly Dictionary<Key, bool> _keyDown = new();
    private static readonly Dictionary<Key, float> _keyDownTime = new();
    private static readonly Dictionary<MouseButton, bool> _mouseDown = new();
        
    private static IMouse? _mouse;
    private static IKeyboard? _keyboard;

    private const float MinDownTimeForRepeat = 0.5f;
    private const float IntervalDownTimeForRepeat = 0.05f;

    public static event Action<Key> OnKeyPressed = delegate {  };
    public static event Action<Key> OnKeyRepeat = delegate {  };
    public static event Action<Key> OnKeyReleased = delegate {  };
    public static event Action<char> OnCharReceived = delegate {  };

    internal static Vector2 LastMousePos;

    /// <summary>
    ///     Get the current MousePosition
    /// </summary>
    public static Vector2 MousePosition { get; private set; }

    /// <summary>
    ///     Get the current MouseDelta
    /// </summary>
    public static Vector2 MouseDelta => MousePosition - LastMousePos;

    public static Vector2 ScrollWhellDelta;

    internal static void Setup(IMouse mouse, IKeyboard keyboard)
    {
        _mouse = mouse;
        _keyboard = keyboard;
            
        _keyboard.KeyDown += KeyDown;
        _keyboard.KeyUp += KeyUp;
        _keyboard.KeyChar += KeyChar;

        _mouse.MouseDown += MouseDown;
        _mouse.MouseUp += MouseUp;

        _mouse.MouseMove += MouseMove;
        _mouse.Scroll += MouseScroll;

        var supportedKeys = keyboard.SupportedKeys;
        _keyDown.EnsureCapacity(supportedKeys.Count);
        foreach (var key in supportedKeys)
        {
            _keyDown.Add(key, false);
            _keyDownTime.Add(key, 0);
        }

        var supportedButtons = mouse.SupportedButtons;
        _mouseDown.EnsureCapacity(supportedButtons.Count);
        foreach (var button in supportedButtons) _mouseDown.Add(button, false);
    }

    

    public static void Update()
    {
        foreach (var (key, down) in _keyDown)
        {
            if (!down)
            {
                _keyDownTime[key] = 0;
                continue;
            }
            var downTime = _keyDownTime[key];

            downTime += Engine.DeltaTime;

            //if (key == Key.Backspace) Console.WriteLine(downTime);

            while (downTime > MinDownTimeForRepeat)
            {
                OnKeyRepeat(key);
                downTime -= IntervalDownTimeForRepeat;
            }

            _keyDownTime[key] = downTime;
        }
        
        ScrollWhellDelta = Vector2.Zero;
    }
        
    internal static void MouseMoveEvent(IMouse mouse)
    {
        MousePosition = new Vector2(mouse.Position.X, mouse.Position.Y);
    }

    /// <summary>
    ///     Get the current <see cref="KeyEvent" /> for <see cref="Key" />
    /// </summary>
    public static bool GetKeyDown(Key key)
    {
        return _keyDown[key];
    }

    /// <summary>
    ///     Get the current <see cref="MouseEvent" /> for <see cref="MouseButton" />
    /// </summary>
    public static bool GetMouseDown(MouseButton mouseButton)
    {
        return _mouseDown[mouseButton];
    }

    private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        _keyDown[arg2] = true;
        OnKeyPressed(arg2);
    }

    private static void KeyUp(IKeyboard arg1, Key arg2, int arg3)
    {
        _keyDown[arg2] = false;
        OnKeyReleased(arg2);
    }
        
    private static void KeyChar(IKeyboard arg1, char arg2)
    {
        OnCharReceived(arg2);
    }

    private static void MouseDown(IMouse arg1, MouseButton arg2)
    {
        _mouseDown[arg2] = true;
    }

    private static void MouseUp(IMouse arg1, MouseButton arg2)
    {
        _mouseDown[arg2] = false;
    }

    private static void MouseMove(IMouse arg1, Vector2 arg2)
    {
        LastMousePos = MousePosition;
        MousePosition = new Vector2(arg2.X,Engine.Window.GetWindow().Size.Y - arg2.Y);
    }
    
    private static void MouseScroll(IMouse arg1, ScrollWheel arg2)
    {
        ScrollWhellDelta += new Vector2(arg2.X, arg2.Y);
    }

        
}