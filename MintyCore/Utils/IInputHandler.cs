using System;
using System.Numerics;
using MintyCore.Registries;
using Silk.NET.Input;

namespace MintyCore.Utils;

/// <summary>
/// Input handler for handling keyboard and mouse input
/// </summary>
public interface IInputHandler
{
    /// <summary>
    ///     The delta of the scroll wheel
    /// </summary>
    Vector2 ScrollWheelDelta { get; }

    /// <summary>
    ///     Get the current MousePosition
    /// </summary>
    Vector2 MousePosition { get; set; }

    /// <summary>
    ///     Get the current MouseDelta
    /// </summary>
    Vector2 MouseDelta { get; set; }

    /// <summary>
    /// Initialize the input handler
    /// </summary>
    /// <param name="mouse"> The mouse instance to use </param>
    /// <param name="keyboard"> The keyboard instance to use </param>
    void Setup(IMouse mouse, IKeyboard keyboard, Window window);

    /// <summary>
    ///     Update the input handler
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    ///     Get the current down state for <see cref="Key" />
    /// </summary>
    bool GetKeyDown(Key key);

    /// <summary>
    ///     Get the current down state of a <see cref="MouseButton" />
    /// </summary>
    bool GetMouseDown(MouseButton mouseButton);

    /// <summary>
    ///     Clears the Key and Mouse button dictionaries
    /// </summary>
    void KeyClear();

    /// <summary>
    ///     Removes a Key or Mouse button action via ID
    /// </summary>
    /// <param name="id"></param>
    void RemoveKeyAction(Identification id);

    /// <summary>
    /// Adds a Keyboard Key with action to the registry
    /// </summary>
    /// <param name="id"></param>
    /// <param name="key"></param>
    /// <param name="action"></param>
    void AddKeyAction(Identification id, Key key, OnKeyPressedDelegate action);

    /// <summary>
    /// Adds a Mouse Button Key with action to the Registry
    /// </summary>
    /// <param name="id"></param>
    /// <param name="mouseButton"></param>
    /// <param name="action"></param>
    void AddKeyAction(Identification id, MouseButton mouseButton, OnKeyPressedDelegate action);

    /// <summary>
    /// Add a callback to be executed when a char is received
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    void AddOnCharReceived(Action<char> action);

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a char is received
    /// </summary>
    /// <param name="action"></param>
    void RemoveOnCharReceived(Action<char> action);

    /// <summary>
    /// Add a callback to be executed when a key is pressed
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    void AddOnKeyDown(Action<Key> action);

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a key is pressed
    /// </summary>
    /// <param name="action"></param>
    void RemoveOnKeyDown(Action<Key> action);

    /// <summary>
    /// Add a callback to be executed when a key is released
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    void AddOnKeyUp(Action<Key> action);

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a key is released
    /// </summary>
    /// <param name="action"></param>
    void RemoveOnKeyUp(Action<Key> action);

    /// <summary>
    /// Add a callback to be executed when a key is repeated
    /// IMPORTANT: Remember to remove the callback when not longer needed as this will otherwise break mod unloading
    /// </summary>
    /// <param name="action"></param>
    void AddOnKeyRepeat(Action<Key> action);

    /// <summary>
    /// Remove a callback from the list of callbacks to be executed when a key is repeated
    /// </summary>
    /// <param name="action"></param>
    void RemoveOnKeyRepeat(Action<Key> action);
    
    /// <summary>
    /// 
    /// </summary>
    public delegate void OnKeyPressedDelegate(KeyStatus? keyState, MouseButtonStatus? mouseButtonStatus);
}