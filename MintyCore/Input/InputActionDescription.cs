using System;
using OneOf;
using Silk.NET.GLFW;

namespace MintyCore.Input;

public record struct InputActionDescription(
    OneOf<Key, MouseButton> DefaultInput,
    KeyModifiers RequiredModifiers,
    bool StrictModifiers,
    Func<InputActionParams, InputActionResult> ActionCallback);

public record struct InputActionParams(InputAction InputAction, KeyModifiers ActiveModifiers);

