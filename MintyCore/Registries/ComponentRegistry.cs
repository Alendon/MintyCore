using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="IComponent" />
/// </summary>
[Registry("component")]
[PublicAPI]
public class ComponentRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Component;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        ComponentManager.RemoveComponent(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Components", LogImportance.Info, "Registry");
        ClearRegistryEvents();
        ComponentManager.Clear();
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
    /// Register a <see cref="IComponent" />
    /// Used by the SourceGenerator for the <see cref="Registries.RegisterComponentAttribute"/>
    /// </summary>
    /// <param name="componentId">Id of the component to register</param>
    /// <typeparam name="TComponent">Type of the component</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterComponent<TComponent>(Identification componentId)
        where TComponent : unmanaged, IComponent
    {
        ComponentManager.AddComponent<TComponent>(componentId);
    }


    /// <summary>
    ///     Override a previously registered component
    ///     Call this at <see cref="OnPostRegister" />
    /// </summary>
    /// <param name="id">Id of the component</param>
    /// <typeparam name="TComponent">Type of the component to override</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Post, RegisterMethodOptions.UseExistingId)]
    public static void OverrideComponent<TComponent>(Identification id) where TComponent : unmanaged, IComponent
    {
        RegistryManager.AssertPostObjectRegistryPhase();

        ComponentManager.SetComponent<TComponent>(id);
    }
}