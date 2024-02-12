using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;
using Serilog;

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
    
    /// <summary/>
    public required IComponentManager ComponentManager { private get; init; }

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
        Log.Information("Clearing Components");
        ComponentManager.Clear();
    }


    /// <summary>
    /// Register a <see cref="IComponent" />
    /// Used by the SourceGenerator for the <see cref="Registries.RegisterComponentAttribute"/>
    /// </summary>
    /// <param name="componentId">Id of the component to register</param>
    /// <typeparam name="TComponent">Type of the component</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterComponent<TComponent>(Identification componentId) where TComponent : unmanaged, IComponent
    {
        ComponentManager.AddComponent<TComponent>(componentId);
    }
}