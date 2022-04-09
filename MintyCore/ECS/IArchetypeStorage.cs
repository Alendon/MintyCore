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
    ///     Id of the archetype stored in this storage
    /// </summary>
// ReSharper disable once UnusedAutoPropertyAccessor.Global
    Identification Id { get; }
    
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
    /// Get the reference to the <see cref="TComponent"/> of the given <see cref="Entity"/>
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <typeparam name="TComponent">Type of the component.</typeparam>
    /// <returns>Reference to the component</returns>
    ref TComponent GetComponent<TComponent>(Entity entity)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    /// Get the reference to the <see cref="TComponent"/> of the given <see cref="Entity"/>
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <param name="componentId"><see cref="Identification"/> of the component</param>
    /// <typeparam name="TComponent">Type of the component.</typeparam>
    /// <returns>Reference to the component</returns>
    ref TComponent GetComponent<TComponent>(Entity entity, Identification componentId)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    /// Get the reference to the <see cref="TComponent"/> of the given entity index inside the storage
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

    internal bool AddEntity(Entity entity);
    internal void RemoveEntity(Entity entity);
    internal IEnumerable<(Entity entity, Identification componentId, IntPtr componentPtr)> GetDirtyEnumerator();
}

