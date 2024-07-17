using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///   Interface to manage the entities in a world
/// </summary>
[PublicAPI]
public interface IEntityManager : IDisposable
{
    /// <summary>
    ///     Get the entity count
    /// </summary>
    int EntityCount { get; }

    /// <summary>
    ///     Get a enumerable containing all entities
    /// </summary>
    IEnumerable<Entity> Entities { get; }

    /// <summary>
    ///     Get the owner of an entity
    /// </summary>
    Player GetEntityOwner(Entity entity);

    /// <summary>
    ///     Get the storage for a specific archetype
    /// </summary>
    /// <param name="id">Id of the archetype</param>
    /// <returns>Archetype storage which contains all entities of an archetype</returns>
    IArchetypeStorage GetArchetypeStorage(Identification id);

    /// <summary>
    ///     Create a new Entity
    /// </summary>
    /// <param name="archetypeId">Archetype of the entity</param>
    /// <param name="entitySetup">Setup interface for easier entity setup synchronization between server and client</param>
    /// <param name="owner">Owner of the entity</param>
    /// <returns></returns>
    Entity CreateEntity(Identification archetypeId, Player owner, IEntitySetup? entitySetup = null);

    /// <summary>
    /// Add an existing entity to the world
    /// This method is used to add a entity to a client world, which was created on the server
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="owner"></param>
    /// <param name="entitySetup"></param>
    void AddEntity(Entity entity, Player owner, IEntitySetup? entitySetup = null);

    /// <summary>
    ///     Destroy an <see cref="Entity" />
    /// </summary>
    /// <param name="entity"><see cref="Entity" /> to destroy</param>
    void DestroyEntity(Entity entity);

    /// <summary>
    /// Remove an entity from the world
    /// This method is used to remove a entity from a client world
    /// </summary>
    /// <param name="entity"></param>
    void RemoveEntity(Entity entity);

    /// <summary>
    ///     Check if a entity exists in this <see cref="EntityManager" />
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    bool EntityExists(Entity entity);

    /// <summary>
    ///     Get all entities which belongs to a specific owner
    /// </summary>
    IEnumerable<Entity> GetEntitiesByOwner(Player playerId);

    /// <summary>
    ///     Set the value of an <see cref="IComponent" /> of an <see cref="Entity" />
    /// </summary>
    void SetComponent<TComponent>(Entity entity, TComponent component, bool markDirty = true)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    /// Get the reference to the component of an <see cref="Entity" />
    /// This method is only valid to call while the ECS is not executing
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <typeparam name="TComponent">Type of component to get</typeparam>
    /// <returns>Reference to the component</returns>
    ref TComponent GetComponent<TComponent>(Entity entity)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    /// Get the reference to the component of an <see cref="Entity" />
    /// This method is only valid to call while the ECS is not executing
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <param name="componentId">Id of the component</param>
    /// <typeparam name="TComponent">Type of component to get</typeparam>
    /// <returns>Reference to the component</returns>
    ref TComponent GetComponent<TComponent>(Entity entity, Identification componentId)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    /// Try to get the reference to the component of an <see cref="Entity" />
    /// This method is only valid to call while the ECS is not executing
    /// </summary>
    /// <param name="entity"> Entity to get component from</param>
    /// <param name="success"> True if the component was found</param>
    /// <typeparam name="TComponent"> Type of component to get</typeparam>
    /// <returns> Reference to the component, this is a null reference if the component was not found</returns>
    /// <remarks>This method is unusual to use. But as double references are not supported, the reference needs to be returned</remarks>
    ref TComponent TryGetComponent<TComponent>(Entity entity, out bool success)
        where TComponent : unmanaged, IComponent;

    /// <summary>
    /// Get the pointer to the component of an <see cref="Entity" />
    /// This method is only valid to call while the ECS is not executing
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <param name="componentId">Id of the component</param>
    /// <returns>Pointer to the component</returns>
    IntPtr GetComponentPtr(Entity entity, Identification componentId);

    /// <summary>
    /// 
    /// </summary>
    void Update();

    /// <summary>
    ///  Enqueue a entity to be destroyed
    /// </summary>
    /// <param name="entity"> Entity to destroy</param>
    void EnqueueDestroyEntity(Entity entity);
    
    /// <summary>
    ///     EntityCallback delegate for entity specific events
    /// </summary>
    /// <param name="world"><see cref="IWorld" /> the entity lives in</param>
    /// <param name="entity"></param>
    public delegate void EntityCallback(IWorld world, Entity entity);

    /// <summary>
    ///   Event which is called after a entity was created
    /// </summary>
    public static event EntityCallback PostEntityCreateEvent = delegate {  };
    
    /// <summary>
    ///  Event which is called before a entity is deleted
    /// </summary>
    public static event EntityCallback PreEntityDeleteEvent = delegate {  };
    
    /// <summary>
    ///   Invoke the <see cref="PostEntityCreateEvent"/>
    /// </summary>
    /// <param name="world"> The world the entity was created in </param>
    /// <param name="entity"> The entity which was created </param>
    protected static void InvokePostEntityCreateEvent(IWorld world, Entity entity) => PostEntityCreateEvent(world, entity);
    
    /// <summary>
    ///  Invoke the <see cref="PreEntityDeleteEvent"/>
    /// </summary>
    /// <param name="world"> The world the entity was created in </param>
    /// <param name="entity"> The entity which was created </param>
    protected static void InvokePreEntityDeleteEvent(IWorld world, Entity entity) => PreEntityDeleteEvent(world, entity);
}