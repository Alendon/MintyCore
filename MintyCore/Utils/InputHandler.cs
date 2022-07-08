using System;
using System.Collections.Generic;
using System.Numerics;
using MintyCore.Registries;
using Silk.NET.Input;

namespace MintyCore.Utils;

/// <summary>
///     Class to manage user input
/// </summary>
public static class InputHandler
{
    private const float MinDownTimeForRepeat = 0.5f;
    private static readonly Dictionary<Key, bool> _keyDown = new();
    private static readonly Dictionary<Key, float> _keyDownTime = new();
    private static readonly Dictionary<MouseButton, bool> _mouseDown = new();

    private static IMouse? _mouse;
    private static IKeyboard? _keyboard;

    internal static Vector2 LastMousePos;

    /// <summary>
    ///     The delta of the scroll wheel
    /// </summary>
    public static Vector2 ScrollWheelDelta;

    /// <summary>
    ///     Get the current MousePosition
    /// </summary>
    public static Vector2 MousePosition { get; private set; }

    /// <summary>
    ///     Get the current MouseDelta
    /// </summary>
    public static Vector2 MouseDelta => MousePosition - LastMousePos;

    /// <summary>
    ///     Event when a character from the keyboard is received
    /// </summary>
    private static event Action<char> OnCharReceived = delegate { };

    private static event Action<Key> OnKeyDown = delegate { };
    private static event Action<Key> OnKeyUp = delegate { };
    private static event Action<Key> OnKeyRepeat = delegate { };

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

        // Ensure Capacity for dictionary
        _keyDown.EnsureCapacity(supportedKeys.Count);
        _keyDownTime.EnsureCapacity(supportedKeys.Count);
        foreach (var key in supportedKeys)
        {
            _keyDown.Add(key, false);
            _keyDownTime.Add(key, 0);
            _actionsPerKey.Add(key, new HashSet<Identification>());
        }

        var supportedButtons = mouse.SupportedButtons;
        _mouseDown.EnsureCapacity(supportedButtons.Count);
        foreach (var button in supportedButtons)
        {
            _mouseDown.Add(button, false);
            _actionsPerMouseButton.Add(button, new HashSet<Identification>());
        }
    }

    /// <summary>
    ///     Update the input handler
    /// </summary>
    public static void Update()
    {
        foreach (var (key, down) in _keyDown)
        {
            if (key == Key.Unknown) continue;

            if (!down)
            {
                _keyDownTime[key] = 0;
                continue;
            }

            var downTime = _keyDownTime[key];

            downTime += Engine.DeltaTimeF;

            if (downTime > MinDownTimeForRepeat)
            {
                KeyRepeat(key);
            }

            _keyDownTime[key] = downTime;
        }

        ScrollWheelDelta = Vector2.Zero;
    }

    /// <summary>
    ///     Get the current down state for <see cref="Key" />
    /// </summary>
    public static bool GetKeyDown(Key key)
    {
        return _keyDown.TryGetValue(key, out var down) && down;
    }

    /// <summary>
    ///     Get the current down state of a <see cref="MouseButton" />
    /// </summary>
    public static bool GetMouseDown(MouseButton mouseButton)
    {
        return _mouseDown.TryGetValue(mouseButton, out var down) && down;
    }

    private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        OnKeyDown(arg2);
        _keyDown[arg2] = true;

        var actionIds = _actionsPerKey[arg2];

        foreach (var id in actionIds)
        {
            var keyStat = _keyStatus[id];

            if (keyStat == KeyStatus.KeyDown)
                _keyAction[id]();
        }
    }

    private static void KeyUp(IKeyboard arg1, Key arg2, int arg3)
    {
        OnKeyUp(arg2);
        _keyDown[arg2] = false;

        var actionIds = _actionsPerKey[arg2];

        foreach (var id in actionIds)
        {
            var keyStat = _keyStatus[id];

            if (keyStat == KeyStatus.KeyUp)
                _keyAction[id]();
        }
        //Logger.WriteLog($"Key pressed for: {_keyDownTime[arg2] += Engine.DeltaTime}", LogImportance.Info, "Input Handler");
    }

    private static void KeyChar(IKeyboard arg1, char arg2)
    {
        OnCharReceived(arg2);
    }

    private static void MouseDown(IMouse arg1, MouseButton arg2)
    {
        _mouseDown[arg2] = true;

        var actionIds = _actionsPerMouseButton[arg2];

        foreach (var id in actionIds)
        {
            var mouseButtonStatus = _mouseButtonStatus[id];

            if (mouseButtonStatus == MouseButtonStatus.MouseButtonDown)
                _keyAction[id]();
        }
    }

    private static void MouseUp(IMouse arg1, MouseButton arg2)
    {
        _mouseDown[arg2] = false;

        var actionIds = _actionsPerMouseButton[arg2];

        foreach (var id in actionIds)
        {
            var mouseButtonStatus = _mouseButtonStatus[id];

            if (mouseButtonStatus == MouseButtonStatus.MouseButtonUp)
                _keyAction[id]();
        }
    }

    private static void MouseMove(IMouse arg1, Vector2 arg2)
    {
        LastMousePos = MousePosition;
        MousePosition = Engine.Window is not null
            ? new Vector2(arg2.X, Engine.Window.WindowInstance.Size.Y - arg2.Y)
            : Vector2.Zero;
    }

    private static void MouseScroll(IMouse arg1, ScrollWheel arg2)
    {
        ScrollWheelDelta += new Vector2(arg2.X, arg2.Y);
    }

    private static void KeyRepeat(Key arg1)
    {
        OnKeyRepeat(arg1);
        var actionIds = _actionsPerKey[arg1];

        foreach (var id in actionIds)
        {
            var keyStat = _keyStatus[id];

            if (keyStat == KeyStatus.KeyRepeat)
                _keyAction[id]();
        }
    }

    /// <summary>
    ///     Clears the Key and Mouse button dictionaries
    /// </summary>
    internal static void KeyClear()
    {
        _keyPerId.Clear();
        _keyAction.Clear();
        _keyStatus.Clear();
        foreach (var set in _actionsPerKey.Values)
        {
            set.Clear();
        }

        _mouseButtonPerId.Clear();
        _mouseButtonStatus.Clear();
        foreach (var set in _actionsPerMouseButton.Values)
        {
            set.Clear();
        }
    }

    /// <summary>
    ///     Removes a Key or Mouse button action via ID
    /// </summary>
    /// <param name="id"></param>
    internal static void RemoveKeyAction(Identification id)
    {
        if (_keyPerId.Remove(id, out var key))
        {
            _actionsPerKey[key].Remove(id);
        }

        _keyAction.Remove(id);
        _keyStatus.Remove(id);

        if (_mouseButtonPerId.Remove(id, out var mouseButton))
        {
            _actionsPerMouseButton[mouseButton].Remove(id);
        }

        _mouseButtonStatus.Remove(id);
    }

    private static readonly Dictionary<Identification, Key> _keyPerId = new();
    private static readonly Dictionary<Identification, Action> _keyAction = new();
    private static readonly Dictionary<Identification, KeyStatus> _keyStatus = new();
    private static readonly Dictionary<Key, HashSet<Identification>> _actionsPerKey = new();

    private static readonly Dictionary<Identification, MouseButton> _mouseButtonPerId = new();
    private static readonly Dictionary<Identification, MouseButtonStatus> _mouseButtonStatus = new();
    private static readonly Dictionary<MouseButton, HashSet<Identification>> _actionsPerMouseButton = new();

    //TODO: Implement Menue Registry
    //private static readonly Dictionary<Identification, HashSet<Menue>> _actionPerMenue = new();

    /// <summary>
    /// Adds a Keyboard Key with action to the registry
    /// </summary>
    /// <param name="id"></param>
    /// <param name="key"></param>
    /// <param name="action"></param>
    /// <param name="status"></param>
    internal static void AddKeyAction(Identification id, Key key, Action action, KeyStatus status)
    {
        _keyPerId[id] = key;
        _keyAction[id] = action;
        _keyStatus[id] = status;

        if (!_actionsPerKey.ContainsKey(key))
        {
            _actionsPerKey.Add(key, new HashSet<Identification>());
        }

        _actionsPerKey[key].Add(id);
    }

    /// <summary>
    /// Adds a Mouse Button Key with action to the Registry
    /// </summary>
    /// <param name="id"></param>
    /// <param name="mouseButton"></param>
    /// <param name="action"></param>
    /// <param name="status"></param>
    internal static void AddKeyAction(Identification id, MouseButton mouseButton, Action action,
        MouseButtonStatus status)
    {
        _mouseButtonPerId[id] = mouseButton;
        _keyAction[id] = action;
        _mouseButtonStatus[id] = status;

        if (!_actionsPerMouseButton.ContainsKey(mouseButton))
        {
            _actionsPerMouseButton.Add(mouseButton, new HashSet<Identification>());
        }

        _actionsPerMouseButton[mouseButton].Add(id);
    }

    /// <summary>
    /// Add a callback to be executed when a char is received
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    public static void AddOnCharReceived(Action<char> action)
    {
        OnCharReceived += (action);
    }

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a char is received
    /// </summary>
    /// <param name="action"></param>
    public static void RemoveOnCharReceived(Action<char> action)
    {
        OnCharReceived -= (action);
    }

    /// <summary>
    /// Add a callback to be executed when a key is pressed
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    public static void AddOnKeyDown(Action<Key> action)
    {
        OnKeyDown += (action);
    }

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a key is pressed
    /// </summary>
    /// <param name="action"></param>
    public static void RemoveOnKeyDown(Action<Key> action)
    {
        OnKeyDown -= (action);
    }

    /// <summary>
    /// Add a callback to be executed when a key is released
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    public static void AddOnKeyUp(Action<Key> action)
    {
        OnKeyUp += (action);
    }

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a key is released
    /// </summary>
    /// <param name="action"></param>
    public static void RemoveOnKeyUp(Action<Key> action)
    {
        OnKeyUp -= (action);
    }

    /// <summary>
    /// Add a callback to be executed when a key is repeated
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    public static void AddOnKeyRepeat(Action<Key> action)
    {
        OnKeyRepeat += (action);
    }

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a key is repeated
    /// </summary>
    /// <param name="action"></param>
    public static void RemoveOnKeyRepeat(Action<Key> action)
    {
        OnKeyRepeat -= (action);
    }
}