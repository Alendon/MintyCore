using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DotNext.Diagnostics;
using MintyCore.Utils;
using MintyCore.Utils.Events;
using OneOf;
using Serilog;
using Silk.NET.GLFW;

namespace MintyCore.Input;

/// <summary>
///     Class to manage user input
/// </summary>
[Singleton<IInputHandler>(SingletonContextFlags.NoHeadless)]
internal unsafe class InputHandler(IEventBus eventBus) : IInputHandler
{
    private readonly Dictionary<Key, Timestamp> _keyDown = new();
    private readonly Dictionary<MouseButton, Timestamp> _mouseDown = new();

    private readonly Dictionary<Key, string> _localizedKeyRep = new();

    private readonly Dictionary<Identification, (OneOf<Key, MouseButton>, KeyModifiers)> _inputPerId = new();
    private readonly Dictionary<Identification, InputActionDescription> _inputActionDescriptions = new();

    private readonly Dictionary<Key, Dictionary<KeyModifiers, HashSet<Identification>>> _inputActionPerKey = new();

    private readonly Dictionary<MouseButton, Dictionary<KeyModifiers, HashSet<Identification>>>
        _inputActionPerMouseButton = new();


    /// <summary>
    ///     The delta of the scroll wheel
    /// </summary>
    public Vector2 ScrollWheelDelta
    {
        get
        {
            if (_lastScrollWheelTick != Engine.Tick)
            {
                _scrollWheelDelta = default;
                _lastScrollWheelTick = Engine.Tick;
            }
            
            return _scrollWheelDelta;
        }
        private set => _scrollWheelDelta = value;
    }

    private ulong _lastScrollWheelTick;

    /// <summary>
    ///     Get the current MousePosition
    /// </summary>
    public Vector2 MousePosition { get; set; }

    /// <summary>
    ///     Get the current MouseDelta
    /// </summary>
    public Vector2 MouseDelta
    {
        get
        {
            if (_lastMouseTick != Engine.Tick)
            {
                _mouseDelta = default;
                _lastMouseTick = Engine.Tick;
            }
            return _mouseDelta;
        }
        private set => _mouseDelta = value;
    }

    private ulong _lastMouseTick;
    private Vector2 _scrollWheelDelta;
    private Vector2 _mouseDelta;

    public void Setup(Window window)
    {
        RegisterGlfwNativeEvents(window);
        var glfwApi = Glfw.GetApi();

        foreach (var key in Enum.GetValues<Key>().Except([Key.Unknown]))
        {
            _keyDown.Add(key, default);

            var localizedKeyRep = glfwApi.GetKeyName((int)key, 0);
            if (!string.IsNullOrWhiteSpace(localizedKeyRep)) _localizedKeyRep.Add(key, localizedKeyRep);
        }

        foreach (var mouseButton in Enum.GetValues<MouseButton>())
        {
            _mouseDown.Add(mouseButton, default);
        }
    }

    private void OnGlfwScroll(WindowHandle* window, double offsetX, double offsetY)
    {
        ScrollWheelDelta += new Vector2((float)offsetX, (float)offsetY);

        eventBus.InvokeEvent(new ScrollEvent(offsetX, offsetY));
    }

    private void OnGlfwCursorPos(WindowHandle* window, double x, double y)
    {
        //the window potentially sends multiple positions per game tick, we always want the current position, but dont miss out on the delta

        var oldMousePosition = MousePosition;
        MousePosition = new Vector2((float)x, (float)y);

        MouseDelta += MousePosition - oldMousePosition;

        eventBus.InvokeEvent(new CursorPosEvent(x, y));
    }

    private void OnGlfwCursorEnter(WindowHandle* window, bool entered)
    {
        eventBus.InvokeEvent(new CursorEnterEvent(entered));
    }

    private void OnGlfwMouseButton(WindowHandle* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        _mouseDown[button] = action switch
        {
            InputAction.Press => new Timestamp(),
            InputAction.Release => default,
            _ => _mouseDown[button]
        };

        eventBus.InvokeEvent(new MouseButtonEvent(button, action, mods));

        TriggerInputAction(button, action, mods);
    }

    private void OnGlfwChar(WindowHandle* window, uint codepoint)
    {
        eventBus.InvokeEvent(new CharEvent((char)codepoint));
    }

    private void OnGlfwKey(WindowHandle* window, Key key, int scancode, InputAction action, KeyModifiers mods)
    {
        if (key == Key.Unknown) return;

        _keyDown[key] = action switch
        {
            InputAction.Press => new Timestamp(),
            InputAction.Release => default,
            _ => _keyDown[key]
        };

        _localizedKeyRep.TryGetValue(key, out var localizedKeyRep);
        eventBus.InvokeEvent(new KeyEvent(key, action, mods, localizedKeyRep, scancode));

        TriggerInputAction(key, action, mods);
    }

    private void RegisterGlfwNativeEvents(Window window)
    {
        var glfwApi = Glfw.GetApi();
        var handle = (WindowHandle*)window.WindowInstance.Native!.Glfw!;

        glfwApi.SetKeyCallback(handle, OnGlfwKey);
        glfwApi.SetCharCallback(handle, OnGlfwChar);
        glfwApi.SetMouseButtonCallback(handle, OnGlfwMouseButton);
        glfwApi.SetCursorEnterCallback(handle, OnGlfwCursorEnter);
        glfwApi.SetCursorPosCallback(handle, OnGlfwCursorPos);
        glfwApi.SetScrollCallback(handle, OnGlfwScroll);
    }

    /// <summary>
    ///     Get the current down state for <see cref="Key" />
    /// </summary>
    public bool GetKeyDown(Key key)
    {
        return _keyDown.TryGetValue(key, out var down) && !down.IsEmpty;
    }

    /// <summary>
    ///     Get the current down state of a <see cref="MouseButton" />
    /// </summary>
    public bool GetMouseDown(MouseButton mouseButton)
    {
        return _mouseDown.TryGetValue(mouseButton, out var down) && !down.IsEmpty;
    }

    /// <summary>
    ///     Clears the Key and Mouse button dictionaries
    /// </summary>
    public void KeyClear()
    {
        _inputPerId.Clear();
        _inputActionDescriptions.Clear();
        _inputActionPerKey.Clear();
        _inputActionPerMouseButton.Clear();
    }

    private void TriggerInputAction(Key key, InputAction action, KeyModifiers mods)
    {
        if (_inputActionPerKey.TryGetValue(key, out var inner))
            TriggerInputAction(inner, action, mods);
    }

    private void TriggerInputAction(MouseButton mouseButton, InputAction action, KeyModifiers mods)
    {
        if (_inputActionPerMouseButton.TryGetValue(mouseButton, out var inner))
            TriggerInputAction(inner, action, mods);
    }

    private void TriggerInputAction(Dictionary<KeyModifiers, HashSet<Identification>> innerInputDic, InputAction action,
        KeyModifiers mods)
    {
        //This method uses a linq query which gathers all id's which have a matching key modifier flag combination
        //and sorts them by the hamming distance between the input key modifiers and the required

        var sortedActions =
            from entry in innerInputDic
            where ((int)entry.Key & (int)mods) == (int)entry.Key
            orderby int.PopCount((int)entry.Key ^ (int)mods)
            select (entry.Key, entry.Value);

        foreach (var (modifiers, idSet) in sortedActions)
        {
            foreach (var id in idSet)
            {
                var actionDesc = _inputActionDescriptions[id];

                if (actionDesc.StrictModifiers && modifiers != mods) continue;

                var res = actionDesc.ActionCallback(new InputActionParams(action, mods));

                if (res == InputActionResult.Stop) return;
            }
        }
    }

    public void AddInputAction(Identification id, InputActionDescription desc)
    {
        _inputActionDescriptions[id] = desc;
        _inputPerId[id] = (desc.DefaultInput, desc.RequiredModifiers);

        var innerDic = desc.DefaultInput.Match(
            key =>
            {
                if (!_inputActionPerKey.ContainsKey(key))
                {
                    _inputActionPerKey.Add(key, new Dictionary<KeyModifiers, HashSet<Identification>>());
                }

                return _inputActionPerKey[key];
            },
            mouseButton =>
            {
                if (!_inputActionPerMouseButton.ContainsKey(mouseButton))
                {
                    _inputActionPerMouseButton.Add(mouseButton,
                        new Dictionary<KeyModifiers, HashSet<Identification>>());
                }

                return _inputActionPerMouseButton[mouseButton];
            });

        if (!innerDic.ContainsKey(desc.RequiredModifiers))
        {
            innerDic.Add(desc.RequiredModifiers, new HashSet<Identification>());
        }

        innerDic[desc.RequiredModifiers].Add(id);
    }

    /// <summary>
    ///     Removes a Key or Mouse button action via ID
    /// </summary>
    /// <param name="id"></param>
    public void RemoveInputAction(Identification id)
    {
        _inputActionDescriptions.Remove(id);

        if (!_inputPerId.Remove(id, out var input)) return;

        var inner = input.Item1.Match(
            key =>
            {
                _inputActionPerKey.TryGetValue(key, out var res);
                return res;
            },
            mouse =>
            {
                _inputActionPerMouseButton.TryGetValue(mouse, out var res);
                return res;
            });

        if (inner is not null && inner.TryGetValue(input.Item2, out var idSet))
        {
            idSet.Remove(id);
        }
    }
}