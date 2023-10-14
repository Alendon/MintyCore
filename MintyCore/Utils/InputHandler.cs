using System;
using System.Collections.Generic;
using System.Numerics;
using MintyCore.Registries;
using Silk.NET.Input;

namespace MintyCore.Utils;

/// <summary>
///     Class to manage user input
/// </summary>
[Singleton<IInputHandler>(SingletonContextFlags.NoHeadless)]
public class InputHandler : IInputHandler
{
    private const float MinDownTimeForRepeat = 0.5f;
    private readonly Dictionary<Key, bool> _keyDown = new();
    private readonly Dictionary<Key, float> _keyDownTime = new();
    private readonly Dictionary<MouseButton, bool> _mouseDown = new();

    private IMouse? _mouse;
    private IKeyboard? _keyboard;

    /// <summary>
    ///     The delta of the scroll wheel
    /// </summary>
    public Vector2 ScrollWheelDelta { get; private set; }

    /// <summary>
    ///     Get the current MousePosition
    /// </summary>
    public Vector2 MousePosition { get; set; }

    /// <summary>
    ///     Get the current MouseDelta
    /// </summary>
    public Vector2 MouseDelta { get; set; }
    
    /// <summary>
    ///     Event when a character from the keyboard is received
    /// </summary>
    private event Action<char> OnCharReceived = delegate { };

    private event Action<Key> OnKeyDown = delegate { };
    private event Action<Key> OnKeyUp = delegate { };
    private event Action<Key> OnKeyRepeat = delegate { };

    public void Setup(IMouse mouse, IKeyboard keyboard)
    {
        _mouse = mouse;
        _keyboard = keyboard;

        _keyboard.KeyDown += KeyDown;
        _keyboard.KeyUp += KeyUp;
        _keyboard.KeyChar += KeyChar;

        _mouse.MouseDown += MouseDown;
        _mouse.MouseUp += MouseUp;

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
    public void Update(float deltaTime)
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

            downTime += deltaTime;

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
    public bool GetKeyDown(Key key)
    {
        return _keyDown.TryGetValue(key, out var down) && down;
    }

    /// <summary>
    ///     Get the current down state of a <see cref="MouseButton" />
    /// </summary>
    public bool GetMouseDown(MouseButton mouseButton)
    {
        return _mouseDown.TryGetValue(mouseButton, out var down) && down;
    }

    private void KeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        OnKeyDown(arg2);
        _keyDown[arg2] = true;

        if (arg2 != Key.Unknown)
        {
            var actionIds = _actionsPerKey[arg2];

            foreach (var id in actionIds)
            {
                _keyAction[id](KeyStatus.KeyDown, null);
            }
        }
    }

    private void KeyUp(IKeyboard arg1, Key arg2, int arg3)
    {
        OnKeyUp(arg2);
        _keyDown[arg2] = false;

        if (arg2 != Key.Unknown)
        {
            var actionIds = _actionsPerKey[arg2];

            foreach (var id in actionIds)
            {
                _keyAction[id](KeyStatus.KeyUp, null);
            }
        }
        //Logger.WriteLog($"Key pressed for: {_keyDownTime[arg2] += Engine.DeltaTime}", LogImportance.Info, "Input Handler");
    }

    private void KeyChar(IKeyboard arg1, char arg2)
    {
        OnCharReceived(arg2);
    }

    private void MouseDown(IMouse arg1, MouseButton arg2)
    {
        _mouseDown[arg2] = true;

        if (arg2 != MouseButton.Unknown)
        {
            var actionIds = _actionsPerMouseButton[arg2];

            foreach (var id in actionIds)
            {
                _keyAction[id](null, MouseButtonStatus.MouseButtonDown);
            }
        }
    }

    private void MouseUp(IMouse arg1, MouseButton arg2)
    {
        _mouseDown[arg2] = false;

        if (arg2 != MouseButton.Unknown)
        {
            var actionIds = _actionsPerMouseButton[arg2];

            foreach (var id in actionIds)
            {
                _keyAction[id](null, MouseButtonStatus.MouseButtonUp);
            }
        }
    }

    private void MouseScroll(IMouse arg1, ScrollWheel arg2)
    {
        ScrollWheelDelta += new Vector2(arg2.X, arg2.Y);
    }

    private void KeyRepeat(Key arg1)
    {
        OnKeyRepeat(arg1);
        var actionIds = _actionsPerKey[arg1];

        foreach (var id in actionIds)
        {
            _keyAction[id](KeyStatus.KeyRepeat, null);
        }
    }

    /// <summary>
    ///     Clears the Key and Mouse button dictionaries
    /// </summary>
    public void KeyClear()
    {
        _keyPerId.Clear();
        _keyAction.Clear();
        foreach (var set in _actionsPerKey.Values)
        {
            set.Clear();
        }

        _mouseButtonPerId.Clear();
        foreach (var set in _actionsPerMouseButton.Values)
        {
            set.Clear();
        }
    }

    /// <summary>
    ///     Removes a Key or Mouse button action via ID
    /// </summary>
    /// <param name="id"></param>
    public void RemoveKeyAction(Identification id)
    {
        if (_keyPerId.Remove(id, out var key))
        {
            _actionsPerKey[key].Remove(id);
        }

        _keyAction.Remove(id);

        if (_mouseButtonPerId.Remove(id, out var mouseButton))
        {
            _actionsPerMouseButton[mouseButton].Remove(id);
        }
    }

    private readonly Dictionary<Identification, Key> _keyPerId = new();
    private readonly Dictionary<Identification, OnKeyPressedDelegate> _keyAction = new();
    private readonly Dictionary<Key, HashSet<Identification>> _actionsPerKey = new();

    private readonly Dictionary<Identification, MouseButton> _mouseButtonPerId = new();
    private readonly Dictionary<MouseButton, HashSet<Identification>> _actionsPerMouseButton = new();

    /// <summary>
    /// 
    /// </summary>
    public delegate void OnKeyPressedDelegate(KeyStatus? keyState, MouseButtonStatus? mouseButtonStatus);

    // TODO: Implement menu registry

    /// <summary>
    /// Adds a Keyboard Key with action to the registry
    /// </summary>
    /// <param name="id"></param>
    /// <param name="key"></param>
    /// <param name="action"></param>
    public void AddKeyAction(Identification id, Key key, OnKeyPressedDelegate action)
    {
        _keyPerId[id] = key;
        _keyAction[id] = action;

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
    public void AddKeyAction(Identification id, MouseButton mouseButton, OnKeyPressedDelegate action)
    {
        _mouseButtonPerId[id] = mouseButton;
        _keyAction[id] = action;

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
    public void AddOnCharReceived(Action<char> action)
    {
        OnCharReceived += action;
    }

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a char is received
    /// </summary>
    /// <param name="action"></param>
    public void RemoveOnCharReceived(Action<char> action)
    {
        OnCharReceived -= action;
    }

    /// <summary>
    /// Add a callback to be executed when a key is pressed
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    public void AddOnKeyDown(Action<Key> action)
    {
        OnKeyDown += action;
    }

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a key is pressed
    /// </summary>
    /// <param name="action"></param>
    public void RemoveOnKeyDown(Action<Key> action)
    {
        OnKeyDown -= action;
    }

    /// <summary>
    /// Add a callback to be executed when a key is released
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    public void AddOnKeyUp(Action<Key> action)
    {
        OnKeyUp += action;
    }

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a key is released
    /// </summary>
    /// <param name="action"></param>
    public void RemoveOnKeyUp(Action<Key> action)
    {
        OnKeyUp -= action;
    }

    /// <summary>
    /// Add a callback to be executed when a key is repeated
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    public void AddOnKeyRepeat(Action<Key> action)
    {
        OnKeyRepeat += action;
    }

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a key is repeated
    /// </summary>
    /// <param name="action"></param>
    public void RemoveOnKeyRepeat(Action<Key> action)
    {
        OnKeyRepeat -= action;
    }
}