using System;
using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.ECS;

public interface IComponentManager
{
    void SetComponent<TComponent>(Identification id) where TComponent : unmanaged, IComponent;

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

    void PopulateComponentDefaultValues(Identification componentId, IntPtr componentLocation);

    /// <summary>
    ///     Cast a <see cref="IntPtr" /> to <see cref="IComponent" /> by the given component <see cref="Identification" />
    /// </summary>
    /// <param name="componentId"><see cref="Identification" /> of the component</param>
    /// <param name="componentPtr">Location of the component in memory</param>
    /// <returns><see cref="IComponent" /> parent of the component</returns>
    IComponent CastPtrToIComponent(Identification componentId, IntPtr componentPtr);

    void Clear();
    IEnumerable<Identification> GetComponentList();
    void RemoveComponent(Identification objectId);

    /// <summary>
    /// Get the type of a component
    /// </summary>
    /// <param name="componentId"></param>
    /// <returns></returns>
    Type? GetComponentType(Identification componentId);
}