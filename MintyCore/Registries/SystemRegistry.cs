using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
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
    public void PreUnRegister()
    {
    }

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
        ClearRegistryEvents();
        SystemManager.Clear();
    }

    /// <inheritdoc />
    public void PreRegister()
    {
        OnPreRegister();
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
        SystemManager.SortSystems();
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };


    /// <summary>
    /// Register a new system
    /// This method is used by the source generator for the auto registry
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TSystem"></typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterSystem<TSystem>(Identification id) where TSystem : ASystem, new()
    {
        SystemManager.RegisterSystem<TSystem>(id);
    }

    /// <summary>
    ///     Override a previously registered system
    ///     Call this at <see cref="OnPostRegister" />
    /// </summary>
    /// <param name="systemId">Id of the system</param>
    /// <typeparam name="TSystem">Type of the new system</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Post, RegisterMethodOptions.UseExistingId)]
    public static void SetSystem<TSystem>(Identification systemId) where TSystem : ASystem, new()
    {
        RegistryManager.AssertPostObjectRegistryPhase();
        SystemManager.SetSystem<TSystem>(systemId);
    }
}