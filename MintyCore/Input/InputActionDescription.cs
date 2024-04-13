using System;
using OneOf;
using Silk.NET.GLFW;

namespace MintyCore.Input;

/// <summary>
///  Represents an input action.
/// </summary>
/// <param name="DefaultInput"> The default input for the action. </param>
/// <param name="RequiredModifiers"> The modifiers required to trigger the action. </param>
/// <param name="StrictModifiers"> Whether the modifiers must match exactly. </param>
/// <param name="ActionCallback"> The callback to execute when the action is triggered. </param>
public record struct InputActionDescription(
    OneOf<Key, MouseButton> DefaultInput,
    KeyModifiers RequiredModifiers,
    bool StrictModifiers,
    Func<InputActionParams, InputActionResult> ActionCallback);

/// <summary>
///  Represents the parameters for an input action.
/// </summary>
/// <param name="InputAction"> The glfw <see cref="Silk.NET.GLFW.InputAction"/> </param>
/// <param name="ActiveModifiers"> The active modifiers. </param>
public record struct InputActionParams(InputAction InputAction, KeyModifiers ActiveModifiers);

