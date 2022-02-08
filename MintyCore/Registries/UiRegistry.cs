using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.UI;
using MintyCore.Utils;

namespace MintyCore.Registries;

public class UiRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.Ui;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public static event Action OnPrefabRegister = delegate { };
    public static event Action OnRootRegister = delegate { };


    public static Identification RegisterUiPrefab(ushort modId, string stringIdentifier, Func<Element> prefabCreator)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Ui, stringIdentifier);
        UiHandler.AddElementPrefab(id, prefabCreator);
        return id;
    }

    public static Identification RegisterUiRoot(ushort modId, string stringIdentifier, Element rootElement)
    {
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Ui, stringIdentifier);
        UiHandler.AddRootElement(id, rootElement);
        return id;
    }

    public void PreRegister()
    {
    }

    public void Register()
    {
        OnPrefabRegister();
        OnRootRegister();
    }

    public void PostRegister()
    {
    }

    public void Clear()
    {
        ClearRegistryEvents();
    }

    public void ClearRegistryEvents()
    {
        OnPrefabRegister = delegate { };
    }
}