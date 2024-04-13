using System.Numerics;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Input;

/// <summary>
/// Input handler for handling keyboard and mouse input
/// </summary>
[PublicAPI]
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
    Vector2 MouseDelta { get; }

    /// <summary>
    /// Initialize the input handler
    /// </summary>
    void Setup(Window window);

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
    void RemoveInputAction(Identification id);

    /// <summary>
    /// Adds an input action associated with a key to the input handler.
    /// </summary>
    /// <param name="id">The identification of the input action.</param>
    /// <param name="desc">The description of the key action.</param>
    void AddInputAction(Identification id, InputActionDescription desc);

    /// <summary>
    /// Determines which InputConsumer should consume the user input.
    /// </summary>
    public InputConsumer InputConsumer { get; set; }
}

/// <summary>
/// Enum for the different input consumers
/// </summary>
public enum InputConsumer
{
    /// <summary>
    /// Input is consumed by the Avalonia UI
    /// </summary>
    Avalonia,
    /// <summary>
    ///  Input is consumed by the InputActions
    /// </summary>
    InputActions
}