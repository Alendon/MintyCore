using System;
using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
/// Interface to manage individual components
/// </summary>
public interface IComponentManager
{
    /// <summary>
    /// Add a new component type to the manager
    /// </summary>
    /// <param name="componentId"> ID of the component </param>
    /// <typeparam name="TComponent"> Type of the component </typeparam>
    void AddComponent<TComponent>(Identification componentId)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    ///     Check if a <see cref="IComponent" /> is player controlled
    /// </summary>
    bool IsPlayerControlled(Identification componentId);

    /// <summary>
    ///     Get the size in bytes of a <see cref="IComponent" />
    /// </summary>
    /// <param name="componentId"><see cref="Identification" /> of the component</param>
    /// <returns>Offset in bytes</returns>
    int GetComponentSize(Identification componentId);

    /// <summary>
    ///     Serialize a component
    /// </summary>
    void SerializeComponent(IntPtr component, Identification componentId, DataWriter dataWriter,
        IWorld world, Entity entity);

    /// <summary>
    ///     Deserialize a component
    /// </summary>
    /// <returns>True if deserialization was successful</returns>
    bool DeserializeComponent(IntPtr component, Identification componentId, DataReader dataReader,
        IWorld world, Entity entity);

    /// <summary>
    /// Populate the default values of a component
    /// </summary>
    /// <param name="componentId"> ID of the component </param>
    /// <param name="componentLocation"> Location of the component in memory </param>
    void PopulateComponentDefaultValues(Identification componentId, IntPtr componentLocation);

    /// <summary>
    ///     Cast a <see cref="IntPtr" /> to <see cref="IComponent" /> by the given component <see cref="Identification" />
    /// </summary>
    /// <param name="componentId"><see cref="Identification" /> of the component</param>
    /// <param name="componentPtr">Location of the component in memory</param>
    /// <returns><see cref="IComponent" /> parent of the component</returns>
    IComponent CastPtrToIComponent(Identification componentId, IntPtr componentPtr);

    /// <summary>
    /// Clear all internal data
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Get a list of all registered components
    /// </summary>
    /// <returns></returns>
    IEnumerable<Identification> GetComponentList();
    
    /// <summary>
    /// Remove a component from the manager
    /// </summary>
    /// <param name="objectId"> ID of the component to remove </param>
    void RemoveComponent(Identification objectId);

    /// <summary>
    /// Get the type of a component
    /// </summary>
    /// <param name="componentId"></param>
    /// <returns></returns>
    Type? GetComponentType(Identification componentId);
}