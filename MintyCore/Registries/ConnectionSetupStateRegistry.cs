using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Network.ConnectionSetup;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("connection_setup")]
public class ConnectionSetupStateRegistry(IConnectionSetupManager connectionSetupManager) : IRegistry
{
    public ushort RegistryId => RegistryIDs.ConnectionSetup;
    public IEnumerable<ushort> RequiredRegistries => [RegistryIDs.Message];
    public void UnRegister(Identification objectId)
    {
        connectionSetupManager.RemoveConnectionSetupState(objectId);
    }

    public void Clear()
    {
    }

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterConnectionSetupState<TSetupState>(Identification id) where TSetupState : class, ISetupState
    {
        connectionSetupManager.AddConnectionSetupState<TSetupState>(id);
    } 
}