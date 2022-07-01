using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;
using Silk.NET.Input;


namespace MintyCore.Registries;
[Registry("key_action")]
public class KeyActionRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.KeyAction;

    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();


    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    public void Clear()
    {
        InputHandler.KeyClear();
        ClearRegistryEvents();
    }

    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    public void PostRegister()
    {
        OnPostRegister();
    }

    public void PostUnRegister()
    {

    }

    public void PreRegister()
    {
        OnPreRegister();
    }

    public void PreUnRegister()
    {

    }

    public void Register()
    {
        OnRegister();
    }

    public void UnRegister(Identification objectId)
    {
        InputHandler.RemoveKeyAction(objectId);
    }


    /// <summary>
    /// Register the key action
    /// </summary>
    /// <param name="id"></param>
    /// <param name="info"></param>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterKeyAction(Identification id, KeyActionInfo info)
    {
        Logger.AssertAndThrow(!(info.Key is null && info.MouseButton is null), "Key and Mouse Button cannot be null", "Engine/InputHandler");
        Logger.AssertAndThrow(!(info.Key is not null && info.MouseButton is not null), "Key and Mouse Button cannot both have a Value", "Engine/InputHandler");

        if(info.Key is not null && info.MouseButton is null && info.KeyStatus is not null)
            InputHandler.AddKeyAction(id, info.Key.Value, info.Action, info.KeyStatus.Value);

        if (info.MouseButton is not null && info.Key is null && info.MouseButtonStatus is not null)
            InputHandler.AddKeyAction(id, info.MouseButton.Value, info.Action, info.MouseButtonStatus.Value);
    }
}

public struct KeyActionInfo
{
    public Key? Key;
    public MouseButton? MouseButton;
    public Action Action;
    public KeyStatus? KeyStatus;
    public MouseButtonStatus? MouseButtonStatus;
}

public enum KeyStatus
{
    KeyDown,
    KeyUp,
    KeyRepeat
}

public enum MouseButtonStatus
{
    MouseButtonDown,
    MouseButtonUp,
    MouseButtonPressed
}