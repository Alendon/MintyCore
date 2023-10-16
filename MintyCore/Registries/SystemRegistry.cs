using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="ASystem" />
/// </summary>
[Registry("system")]
[PublicAPI]
public class SystemRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.System;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();
    
    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        SystemManager.RemoveSystem(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
        SystemManager.SortSystems();
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Systems", LogImportance.Info, "Registry");
        SystemManager.Clear();
    }

    public void PostRegister(ObjectRegistryPhase currentPhase)
    {
        if (currentPhase == ObjectRegistryPhase.Main)
            SystemManager.SortSystems();
    }


    /// <summary>
    /// Register a new system
    /// This method is used by the source generator for the auto registry
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TSystem"></typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterSystem<TSystem>(Identification id) where TSystem : ASystem
    {
        SystemManager.RegisterSystem<TSystem>(id);
    }
}