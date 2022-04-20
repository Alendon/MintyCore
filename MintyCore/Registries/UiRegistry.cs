using System;
using System.Collections.Generic;
using System.Linq;
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
        if(Engine.HeadlessModeActive)
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
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the prefab</param>
    /// <param name="stringIdentifier"><see cref="string" /> identifier of the prefab</param>
    /// <param name="prefabCreator">Function which returns a new instance of the element</param>
    /// <returns>Generated <see cref="Identification" /> of the prefab</returns>
    public static Identification RegisterUiPrefab(ushort modId, string stringIdentifier, Func<Element> prefabCreator)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Ui, stringIdentifier);
        if(Engine.HeadlessModeActive)
            return id;
        UiHandler.AddElementPrefab(id, prefabCreator);
        return id;
    }

    /// <summary>
    ///     Register a ui root element
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the element</param>
    /// <param name="stringIdentifier"><see cref="string" /> identifier of the element</param>
    /// <param name="rootElementPrefab">The <see cref="Identification"/> of the prefab creating the root element</param>
    /// <returns>Generated <see cref="Identification" /> of the element</returns>
    public static Identification RegisterUiRoot(ushort modId, string stringIdentifier, Identification rootElementPrefab)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Ui, stringIdentifier);
        if(Engine.HeadlessModeActive)
            return id;
        UiHandler.AddRootElement(id, rootElementPrefab);
        return id;
    }

    /// <summary>
    ///     Override a previous registered ui prefab
    ///     Call this at <see cref="OnPostRegister" />
    /// </summary>
    /// <param name="prefabId"><see cref="Identification" /> of the prefab</param>
    /// <param name="prefabCreator">The new prefab creation function</param>
    public static void SetUiPrefab(Identification prefabId, Func<Element> prefabCreator)
    {
        RegistryManager.AssertPostObjectRegistryPhase();
        if(Engine.HeadlessModeActive)
            return;
        UiHandler.SetElementPrefab(prefabId, prefabCreator);
    }

    /// <summary>
    ///     Override a previous registered root element
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="elementId"><see cref="Identification" /> of the element</param>
    /// <param name="rootElementPrefab">The new <see cref="Identification"/> of the prefab creating the root element</param>
    public static void SetRootElement(Identification elementId, Identification rootElementPrefab)
    {
        RegistryManager.AssertPostObjectRegistryPhase();
        if(Engine.HeadlessModeActive)
            return;
        UiHandler.SetRootElement(elementId, rootElementPrefab);
    }
}