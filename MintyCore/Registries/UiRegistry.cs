using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.UI;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     Registry to handle ui root element and element prefab registration
/// </summary>
[Registry("ui")]
[PublicAPI]
public class UiRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Ui;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

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
        UiHandler.CreateRootElements();
    }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if (Engine.HeadlessModeActive)
            return;
        UiHandler.RemoveElement(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public void Clear()
    {
        UiHandler.Clear();
        ClearRegistryEvents();
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };


    /// <summary>
    ///     Register a ui prefab
    /// This method is used by the source generator for the auto registry
    /// </summary>
    /// <param name="id"></param>
    /// <param name="prefabElementInfo"></param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterUiPrefab(Identification id, PrefabElementInfo prefabElementInfo)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        if (Engine.HeadlessModeActive)
            return;
        UiHandler.AddElementPrefab(id, prefabElementInfo.PrefabCreator);
    }

    /// <summary>
    ///     Register a ui root element
    /// This method is used by the source generator for the auto registry
    /// </summary>
    /// <param name="id"></param>
    /// <param name="info"></param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterUiRoot(Identification id, RootElementInfo info)
    {
        if (Engine.HeadlessModeActive)
            return;
        UiHandler.AddRootElement(id, info.RootElementPrefab);
    }


    /// <summary>
    /// Set/override a previous registered ui prefab
    /// This method is used by the source generator for the auto registry
    /// </summary>
    /// <param name="prefabId"></param>
    /// <param name="prefabElementInfo"></param>
    [RegisterMethod(ObjectRegistryPhase.Post, RegisterMethodOptions.UseExistingId)]
    public static void SetUiPrefab(Identification prefabId, PrefabElementInfo prefabElementInfo)
    {
        if (Engine.HeadlessModeActive)
            return;
        UiHandler.SetElementPrefab(prefabId, prefabElementInfo.PrefabCreator);
    }

    /// <summary>
    /// Set/override a previous registered ui root element
    /// This method is used by the source generator for the auto registry
    /// </summary>
    /// <param name="elementId"></param>
    /// <param name="info"></param>
    [RegisterMethod(ObjectRegistryPhase.Post, RegisterMethodOptions.UseExistingId)]
    public static void SetRootElement(Identification elementId, RootElementInfo info)
    {
        if (Engine.HeadlessModeActive)
            return;
        UiHandler.SetRootElement(elementId, info.RootElementPrefab);
    }
}

/// <summary>
/// Wrapper struct to register a ui prefab
/// </summary>
public struct PrefabElementInfo
{
    /// <summary>
    /// Function which instantiates the prefab
    /// </summary>
    public Func<Element> PrefabCreator;
}

/// <summary>
/// Wrapper struct to register a new ui root element
/// </summary>
public struct RootElementInfo
{
    /// <summary>
    /// Id of the prefab function which creates the root element
    /// </summary>
    public Identification RootElementPrefab;
}