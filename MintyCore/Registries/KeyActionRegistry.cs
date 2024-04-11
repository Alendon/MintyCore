using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Input;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all Key Actions
/// </summary>
[Registry("key_action", applicableGameType: GameType.Client)]
[PublicAPI]
public class KeyActionRegistry : IRegistry
{
    /// <summary />
    public ushort RegistryId => RegistryIDs.KeyAction;

    /// <summary />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <summary/>
    public required IInputHandler InputHandler { private get; init; }


    /// <inheritdoc />
    public void Clear()
    {
        InputHandler.KeyClear();
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        InputHandler.RemoveInputAction(objectId);
    }


    /// <summary>
    ///     Register the key action
    /// </summary>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterInputAction(Identification id, InputActionDescription description)
    {
        InputHandler.AddInputAction(id, description);
    }
}