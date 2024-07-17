using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MintyCore.Network;
using MintyCore.Network.Messages;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Manage Entities per <see cref="IWorld" />
/// </summary>
public sealed class EntityManager : IEntityManager
{
    private readonly Dictionary<Identification, IArchetypeStorage> _archetypeStorages = new();

    /// <summary>
    ///     Dictionary to track the used ids for each archetype
    /// </summary>
    private readonly Dictionary<Identification, HashSet<uint>> _entityIdTracking = new();

    private readonly Dictionary<Entity, Player> _entityOwner = new();

    /// <summary>
    ///     Used to find a new free entity id faster
    /// </summary>
    private readonly Dictionary<Identification, uint> _lastFreeEntityId = new();

    private IWorld? _parent;
    private readonly Queue<Entity> _destroyQueue = new();

    private IWorld Parent => _parent ?? throw new Exception("Object is Disposed");

    private IArchetypeManager ArchetypeManager { get; }
    private IPlayerHandler PlayerHandler { get; }
    private INetworkHandler NetworkHandler { get; }

    /// <summary>
    ///     Create a <see cref="EntityManager" /> for a world
    /// </summary>
    public EntityManager(IWorld world, IArchetypeManager archetypeManager, IPlayerHandler playerHandler,
        INetworkHandler networkHandler)
    {
        ArchetypeManager = archetypeManager;
        PlayerHandler = playerHandler;
        NetworkHandler = networkHandler;

        foreach (var (id, _) in ArchetypeManager.GetArchetypes())
        {
            _archetypeStorages.Add(id, ArchetypeManager.CreateArchetypeStorage(id));
            _entityIdTracking.Add(id, new HashSet<uint>());
            _lastFreeEntityId.Add(id, Constants.InvalidId);
        }

        _parent = world;
    }

    /// <summary>
    ///     Get the entity count
    /// </summary>
    public int EntityCount
    {
        get { return _archetypeStorages.Values.Sum(storage => storage.Count); }
    }

    /// <summary>
    ///     Get a enumerable containing all entities
    /// </summary>
    public IEnumerable<Entity> Entities
    {
        get { return _archetypeStorages.Values.SelectMany(storages => storages.Entities); }
    }


    /// <summary>
    ///     Get the owner of an entity
    /// </summary>
    public Player GetEntityOwner(Entity entity)
    {
        return _entityOwner.TryGetValue(entity, out var owner) ? owner : Player.ServerPlayer;
    }

    private Entity GetNextFreeEntityId(Identification archetype)
    {
        var archetypeTrack = _entityIdTracking[archetype];

        var id = _lastFreeEntityId[archetype];
        while (true)
        {
            id++;
            if (!archetypeTrack.Contains(id))
            {
                archetypeTrack.Add(id);
                _lastFreeEntityId[archetype] = id;
                return new Entity(archetype, id);
            }

            if (id == uint.MaxValue)
                throw new MintyCoreException($"Maximum entity count for archetype {archetype} reached");
        }
    }

    private void FreeEntityId(Entity entity)
    {
        _entityIdTracking[entity.ArchetypeId].Remove(entity.Id);
        _lastFreeEntityId[entity.ArchetypeId] = entity.Id - 1;
    }

    /// <summary>
    ///     Get the storage for a specific archetype
    /// </summary>
    /// <param name="id">Id of the archetype</param>
    /// <returns>Archetype storage which contains all entities of an archetype</returns>
    public IArchetypeStorage GetArchetypeStorage(Identification id)
    {
        return _archetypeStorages[id];
    }
    
    /// <summary>
    ///     Create a new Entity
    /// </summary>
    /// <param name="archetypeId">Archetype of the entity</param>
    /// <param name="entitySetup">Setup interface for easier entity setup synchronization between server and client</param>
    /// <param name="owner">Owner of the entity</param>
    /// <returns></returns>
    public Entity CreateEntity(Identification archetypeId, Player owner, IEntitySetup? entitySetup = null)
    {
        if (!Parent.IsServerWorld) return default;

        AssertValidAccess();

        if (entitySetup is not null && !ArchetypeManager.TryGetEntitySetup(archetypeId, out _))
            throw new MintyCoreException($"Entity setup passed but no setup for archetype {archetypeId} registered");

        if (owner.GameId == Constants.InvalidId)
            throw new MintyCoreException("Invalid entity owner");

        var entity = GetNextFreeEntityId(archetypeId);

        _archetypeStorages[archetypeId].AddEntity(entity);

        //only track player controlled entities to reduce the amount of tracked entities
        if (owner != Player.ServerPlayer)
            _entityOwner.Add(entity, owner);

        entitySetup?.SetupEntity(Parent, entity);

        IEntityManager.InvokePostEntityCreateEvent(Parent, entity);


        using var addEntity = NetworkHandler.CreateMessage<AddEntity>();
        addEntity.Entity = entity;
        addEntity.Owner = owner;
        addEntity.EntitySetup = entitySetup;
        addEntity.WorldId = Parent.Identification;

        addEntity.Send(PlayerHandler.GetConnectedPlayers());

        return entity;
    }

    /// <inheritdoc />
    public void AddEntity(Entity entity, Player owner, IEntitySetup? entitySetup = null)
    {
        if (!_archetypeStorages[entity.ArchetypeId].AddEntity(entity)) return;

        if (owner != Player.ServerPlayer)
            _entityOwner.Add(entity, owner);

        entitySetup?.SetupEntity(Parent, entity);

        IEntityManager.InvokePostEntityCreateEvent(Parent, entity);

        if (!Parent.IsServerWorld) return;

        using var addEntity = NetworkHandler.CreateMessage<AddEntity>();
        addEntity.Entity = entity;
        addEntity.Owner = owner;
        addEntity.EntitySetup = entitySetup;
        addEntity.WorldId = Parent.Identification;

        addEntity.Send(PlayerHandler.GetConnectedPlayers());
    }

    /// <summary>
    ///     Destroy an <see cref="Entity" />
    /// </summary>
    /// <param name="entity"><see cref="Entity" /> to destroy</param>
    public void DestroyEntity(Entity entity)
    {
        if (!Parent.IsServerWorld) return;

        AssertValidAccess();

        var removeEntity = NetworkHandler.CreateMessage<RemoveEntity>();
        removeEntity.Entity = entity;
        removeEntity.WorldId = Parent.Identification;
        removeEntity.Send(PlayerHandler.GetConnectedPlayers());

        IEntityManager.InvokePreEntityDeleteEvent(Parent, entity);
        _archetypeStorages[entity.ArchetypeId].RemoveEntity(entity);
        if (_entityOwner.ContainsKey(entity)) _entityOwner.Remove(entity);
        FreeEntityId(entity);
    }

    /// <inheritdoc />
    public void RemoveEntity(Entity entity)
    {
        IEntityManager.InvokePreEntityDeleteEvent(Parent, entity);
        _archetypeStorages[entity.ArchetypeId].RemoveEntity(entity);
        if (_entityOwner.ContainsKey(entity)) _entityOwner.Remove(entity);

        if (!Parent.IsServerWorld) return;
        var removeEntity = NetworkHandler.CreateMessage<RemoveEntity>();
        removeEntity.Entity = entity;
        removeEntity.WorldId = Parent.Identification;
        removeEntity.Send(PlayerHandler.GetConnectedPlayers());
        FreeEntityId(entity);
    }

    /// <summary>
    ///     Check if a entity exists in this <see cref="EntityManager" />
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool EntityExists(Entity entity)
    {
        return _archetypeStorages.ContainsKey(entity.ArchetypeId) &&
               _archetypeStorages[entity.ArchetypeId].Contains(entity);
    }

    /// <summary>
    ///     Get all entities which belongs to a specific owner
    /// </summary>
    public IEnumerable<Entity> GetEntitiesByOwner(Player playerId)
    {
        List<Entity> entities = new();
        foreach (var (entity, id) in _entityOwner)
            if (id == playerId)
                entities.Add(entity);

        return entities;
    }

    #region componentAccess

    /// <summary>
    ///     Set the value of an <see cref="IComponent" /> of an <see cref="Entity" />
    /// </summary>
    public void SetComponent<TComponent>(Entity entity, TComponent component, bool markDirty = true)
        where TComponent : unmanaged, IComponent
    {
        AssertValidAccess();
        AssertArchetypeContainsComponent(entity.ArchetypeId, component.Identification);

        if (markDirty) component.Dirty = true;
        _archetypeStorages[entity.ArchetypeId].GetComponent<TComponent>(entity, component.Identification) = component;
    }

    private void AssertArchetypeContainsComponent(Identification archetypeId, Identification componentIdentification)
    {
        if (!ArchetypeManager.HasComponent(archetypeId, componentIdentification))
            throw new MintyCoreException(
                $"Archetype {archetypeId} does not contain the component {componentIdentification}");
    }

    private void AssertValidAccess()
    {
        if (Parent.IsExecuting)
            throw new MintyCoreException(
                "Accessing the EntityManager is forbidden while the corresponding World is Executing");
    }

    /// <summary>
    /// Get the reference to the component of an <see cref="Entity" />
    /// This method is only valid to call while the ECS is not executing
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <typeparam name="TComponent">Type of component to get</typeparam>
    /// <returns>Reference to the component</returns>
    public ref TComponent GetComponent<TComponent>(Entity entity)
        where TComponent : unmanaged, IComponent
    {
        return ref GetComponent<TComponent>(entity, default(TComponent).Identification);
    }

    /// <summary>
    /// Get the reference to the component of an <see cref="Entity" />
    /// This method is only valid to call while the ECS is not executing
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <param name="componentId">Id of the component</param>
    /// <typeparam name="TComponent">Type of component to get</typeparam>
    /// <returns>Reference to the component</returns>
    public ref TComponent GetComponent<TComponent>(Entity entity, Identification componentId)
        where TComponent : unmanaged, IComponent
    {
        AssertValidAccess();
        AssertArchetypeContainsComponent(entity.ArchetypeId, componentId);

        return ref _archetypeStorages[entity.ArchetypeId].GetComponent<TComponent>(entity, componentId);
    }

    //TODO make a better solution for this
    /// <summary>
    /// Try to get the reference to the component of an <see cref="Entity" />
    /// This method is only valid to call while the ECS is not executing
    /// </summary>
    /// <param name="entity"> Entity to get component from</param>
    /// <param name="success"> True if the component was found</param>
    /// <typeparam name="TComponent"> Type of component to get</typeparam>
    /// <returns> Reference to the component, this is a null reference if the component was not found</returns>
    /// <remarks>This method is unusual to use. But as double references are not supported, the reference needs to be returned</remarks>
    public ref TComponent TryGetComponent<TComponent>(Entity entity, out bool success)
        where TComponent : unmanaged, IComponent
    {
        if (!ArchetypeManager.HasComponent(entity.ArchetypeId, default(TComponent).Identification))
        {
            success = false;
            return ref Unsafe.NullRef<TComponent>();
        }

        success = true;
        return ref GetComponent<TComponent>(entity, default(TComponent).Identification);
    }


    /// <summary>
    /// Get the pointer to the component of an <see cref="Entity" />
    /// This method is only valid to call while the ECS is not executing
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <param name="componentId">Id of the component</param>
    /// <returns>Pointer to the component</returns>
    public IntPtr GetComponentPtr(Entity entity, Identification componentId)
    {
        AssertValidAccess();
        AssertArchetypeContainsComponent(entity.ArchetypeId, componentId);

        return _archetypeStorages[entity.ArchetypeId].GetComponentPtr(entity, componentId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var (id, archetype) in _entityIdTracking)
        foreach (var ids in archetype)
            IEntityManager.InvokePreEntityDeleteEvent(Parent, new Entity(id, ids));

        foreach (var archetypeStorage in _archetypeStorages.Values) archetypeStorage.Dispose();
        _archetypeStorages.Clear();
        
        _parent = null;
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    public void Update()
    {
        while (_destroyQueue.Count > 0)
        {
            var entity = _destroyQueue.Dequeue();
            DestroyEntity(entity);
        }
    }

    /// <inheritdoc />
    public void EnqueueDestroyEntity(Entity entity)
    {
        _destroyQueue.Enqueue(entity);
    }
}

/// <summary>
///     Interface to declare a generic setup for a specific archetype
/// </summary>
public interface IEntitySetup
{
    /// <summary>
    ///     Setup the specified entity.
    /// </summary>
    /// <param name="world">World the entity lives in</param>
    /// <param name="entity">The entity representation</param>
    public void SetupEntity(IWorld world, Entity entity);

    /// <summary>
    ///     Retrieve all needed data to setup a copy of the existing entity.
    /// </summary>
    /// <param name="world">World the entity lives in</param>
    /// <param name="entity">The entity representation</param>
    public void GatherEntityData(IWorld world, Entity entity);

    /// <summary>
    ///     Serialize the entity setup data
    /// </summary>
    /// <param name="writer"></param>
    public void Serialize(DataWriter writer);

    /// <summary>
    ///     Deserialize the entity setup data
    /// </summary>
    /// <param name="reader"></param>
    /// <returns>True if deserialization was successful</returns>
    public bool Deserialize(DataReader reader);
}