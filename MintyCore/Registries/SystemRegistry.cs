using System;
using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="ASystem" />
/// </summary>
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
        Logger.WriteLog("Clearing Systems", LogImportance.INFO, "Registry");
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
    ///     Register a <see cref="ASystem" />
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="ASystem" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="ASystem" /></param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="ASystem" /></returns>
    public static Identification RegisterSystem<TSystem>(ushort modId, string stringIdentifier)
        where TSystem : ASystem, new()
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.System, stringIdentifier);
        SystemManager.RegisterSystem<TSystem>(id);
        return id;
    }

    /// <summary>
    ///     Override a previously registered system
    ///     Call this at <see cref="OnPostRegister" />
    /// </summary>
    /// <param name="systemId">Id of the system</param>
    /// <typeparam name="TSystem">Type of the new system</typeparam>
    public static void SetSystem<TSystem>(Identification systemId) where TSystem : ASystem, new()
    {
        RegistryManager.AssertPostObjectRegistryPhase();
        SystemManager.SetSystem<TSystem>(systemId);
    }
}