using System;
using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
/// Interface representing a storage class for all entities of a specific archetype.
/// </summary>
public interface IArchetypeStorage : IDisposable
{
    /// <summary>
    /// Number of entities stored in this storage
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Read only list of entities stored in this storage
    /// </summary>
    public ReadOnlyEntityList Entities { get; }

    /// <summary>
    /// Check if the storage contains the given entity
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True if the storage contains the given entity</returns>
    public bool Contains(Entity entity);

    /// <summary>
    /// Get the reference to the <see cref="IComponent"/> of the given <see cref="Entity"/>
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <typeparam name="TComponent">Type of the component.</typeparam>
    /// <returns>Reference to the component</returns>
    ref TComponent GetComponent<TComponent>(Entity entity)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    /// Get the reference to the <see cref="IComponent"/> of the given <see cref="Entity"/>
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <param name="componentId"><see cref="Identification"/> of the component</param>
    /// <typeparam name="TComponent">Type of the component.</typeparam>
    /// <returns>Reference to the component</returns>
    ref TComponent GetComponent<TComponent>(Entity entity, Identification componentId)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    /// Get the reference to the <see cref="IComponent"/> of the given entity index inside the storage
    /// </summary>
    /// <param name="entityIndex">Entity index to get component from</param>
    /// <param name="componentId"><see cref="Identification"/> of the component</param>
    /// <typeparam name="TComponent">Type of the component.</typeparam>
    /// <returns>Reference to the component</returns>
    ref TComponent GetComponent<TComponent>(int entityIndex, Identification componentId)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    /// Get the pointer to the component by component id of the given <see cref="Entity"/>
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <param name="componentId"><see cref="Identification"/> of the component</param>
    /// <returns>Pointer to the component</returns>
    IntPtr GetComponentPtr(Entity entity, Identification componentId);

    /// <summary>
    /// Get the pointer to the component by component id of the given entity index inside the storage
    /// </summary>
    /// <param name="entityIndex">Entity index to get component from</param>
    /// <param name="componentId"><see cref="Identification"/> of the component</param>
    /// <returns>Pointer to the component</returns>
    IntPtr GetComponentPtr(int entityIndex, Identification componentId);

    /// <summary>
    /// Add a entity to the archetype storage
    /// </summary>
    /// <param name="entity">Entity to add to the archetype storage</param>
    /// <remarks>Not intended for public use. Only public to allow the implementation in auto generated archetype storages</remarks>
    /// <returns>True if the entity was added successfully</returns>
    bool AddEntity(Entity entity);

    /// <summary>
    /// Remove a entity from the archetype storage
    /// </summary>
    /// <remarks>Not intended for public use. Only public to allow the implementation in auto generated archetype storages</remarks>
    /// <param name="entity">Entity to remove</param>
    void RemoveEntity(Entity entity);

    /// <summary>
    /// Get the dirty enumerator for the archetype storage
    /// This iterates over each component which is marked as dirty and unsets the dirty flag
    /// </summary>
    /// <remarks>Not intended for public use. Only public to allow the implementation in auto generated archetype storages</remarks>
    /// <returns></returns>
    IEnumerable<(Entity entity, Identification componentId, IntPtr componentPtr)> GetDirtyEnumerator();
}