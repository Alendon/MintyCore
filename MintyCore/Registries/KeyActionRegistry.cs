using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;
using Silk.NET.Input;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all Key Actions
/// </summary>
[Registry("key_action")]
[PublicAPI]
public class KeyActionRegistry : IRegistry
{
    /// <summary />
    public ushort RegistryId => RegistryIDs.KeyAction;

    /// <summary />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();
    
    public required IInputHandler InputHandler { private get; init; }


    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <inheritdoc />
    public void Clear()
    {
        InputHandler.KeyClear();
        ClearRegistryEvents();
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void PreRegister()
    {
        OnPreRegister();
    }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        InputHandler.RemoveKeyAction(objectId);
    }


    /// <summary>
    ///     Register the key action
    /// </summary>
    /// <param name="id"></param>
    /// <param name="info"></param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterKeyAction(Identification id, KeyActionInfo info)
    {
        Logger.AssertAndThrow(!(info.Key is null && info.MouseButton is null), "Key and Mouse Button cannot be null",
            "Engine/InputHandler");
        Logger.AssertAndThrow(!(info.Key is not null && info.MouseButton is not null),
            "Key and Mouse Button cannot both have a Value", "Engine/InputHandler");

        if (info.Key is not null)
            InputHandler.AddKeyAction(id, info.Key.Value, info.Action);

        if (info.MouseButton is not null)
            InputHandler.AddKeyAction(id, info.MouseButton.Value, info.Action);
    }
}

/// <summary>
///     Struct to hold information required to register a new key action    
/// </summary>
public struct KeyActionInfo
{
    /// <summary>
    ///     Holds information about the key
    ///     If <see cref="KeyActionInfo.MouseButton"/> is null <see cref="KeyActionInfo.MouseButton"/> can not be null
    /// </summary>
    public Key? Key;

    /// <summary>
    ///     Holds information about the mouse button
    ///     If <see cref="KeyActionInfo.Key"/> is null <see cref="KeyActionInfo.MouseButton"/> can not be null
    /// </summary>
    public MouseButton? MouseButton;

    /// <summary>
    ///     Action that is executed if <see cref="KeyActionInfo.Key"/> or <see cref="KeyActionInfo.MouseButton"/> is pressed>>
    /// </summary>
    public InputHandler.OnKeyPressedDelegate Action;
}

/// <summary>
///     Enum to declare the status that the key needs to have that the action is triggered
/// </summary>
public enum KeyStatus
{
    /// <summary>
    ///     Action if key is pressed
    /// </summary>
    KeyDown,

    /// <summary>
    ///     Action if key is released
    /// </summary>
    KeyUp,

    /// <summary>
    ///     Action if key gets repeated
    /// </summary>
    KeyRepeat
}

/// <summary>
///     Enum to declare the status that the key needs to have that the action is triggered
/// </summary>
public enum MouseButtonStatus
{
    /// <summary>
    ///     Action if mouse button gets pressed
    /// </summary>
    MouseButtonDown,

    /// <summary>
    ///     Action if mouse button gets released
    /// </summary>
    MouseButtonUp
}