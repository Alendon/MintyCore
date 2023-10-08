using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MintyCore.Network;
using MintyCore.Network.Messages;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Manage Entities per <see cref="IWorld" />
/// </summary>
[PublicAPI]
public sealed class EntityManager : IDisposable
{
    /// <summary>
    ///     EntityCallback delegate for entity specific events
    /// </summary>
    /// <param name="world"><see cref="IWorld" /> the entity lives in</param>
    /// <param name="entity"></param>
    public delegate void EntityCallback(IWorld world, Entity entity);


    private readonly Dictionary<Identification, IArchetypeStorage> _archetypeStorages = new();

    /// <summary>
    ///     Dictionary to track the used ids for each archetype
    /// </summary>
    private readonly Dictionary<Identification, HashSet<uint>> _entityIdTracking = new();

    private readonly Dictionary<Entity, ushort> _entityOwner = new();

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
    /// <param name="world"></param>
    public EntityManager(IWorld world, IArchetypeManager archetypeManager, IPlayerHandler playerHandler, INetworkHandler networkHandler)
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
    public ushort GetEntityOwner(Entity entity)
    {
        return _entityOwner.TryGetValue(entity, out var owner) ? owner : Constants.ServerId;
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
                Logger.WriteLog($"Maximum entity count for archetype {archetype} reached", LogImportance.Exception,
                    "ECS");
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
    ///     Event which get fired directly after an entity is created
    /// </summary>
    public static event EntityCallback PostEntityCreateEvent = delegate { };

    /// <summary>
    ///     Event which get fired directly before an entity gets destroyed
    /// </summary>
    public static event EntityCallback PreEntityDeleteEvent = delegate { };

    /// <summary>
    ///     Create a new Entity
    /// </summary>
    /// <param name="archetypeId">Archetype of the entity</param>
    /// <param name="entitySetup">Setup interface for easier entity setup synchronization between server and client</param>
    /// <param name="owner">Owner of the entity</param>
    /// <returns></returns>
    public Entity CreateEntity(Identification archetypeId, Player? owner = null, IEntitySetup? entitySetup = null)
    {
        return CreateEntity(archetypeId, owner?.GameId ?? Constants.ServerId, entitySetup);
    }

    /// <summary>
    ///     Create a new Entity
    /// </summary>
    /// <param name="archetypeId">Archetype of the entity</param>
    /// <param name="entitySetup">Setup interface for easier entity setup synchronization between server and client</param>
    /// <param name="owner">Owner of the entity</param>
    /// <returns></returns>
    public Entity CreateEntity(Identification archetypeId, ushort owner = Constants.ServerId,
        IEntitySetup? entitySetup = null)
    {
        if (!Parent.IsServerWorld) return default;

        AssertValidAccess();

        if (entitySetup is not null && !ArchetypeManager.TryGetEntitySetup(archetypeId, out _))
            Logger.WriteLog($"Entity setup passed but no setup for archetype {archetypeId} registered",
                LogImportance.Exception, "ECS");

        if (owner == Constants.InvalidId)
            Logger.WriteLog("Invalid entity owner", LogImportance.Exception, "ECS");


        var entity = GetNextFreeEntityId(archetypeId);

        _archetypeStorages[archetypeId].AddEntity(entity);

        if (owner != Constants.ServerId)
            _entityOwner.Add(entity, owner);

        entitySetup?.SetupEntity(Parent, entity);

        PostEntityCreateEvent.Invoke(Parent, entity);



        var addEntity = NetworkHandler.CreateMessage<AddEntity>();
        addEntity.Entity = entity;
        addEntity.Owner = owner;
        addEntity.EntitySetup = entitySetup;
        addEntity.WorldId = Parent.Identification;

        addEntity.Send(PlayerHandler.GetConnectedPlayers());

        return entity;
    }

    internal void AddEntity(Entity entity, ushort owner, IEntitySetup? entitySetup = null)
    {
        if (!_archetypeStorages[entity.ArchetypeId].AddEntity(entity)) return;

        if (owner != Constants.ServerId)
            _entityOwner.Add(entity, owner);

        entitySetup?.SetupEntity(Parent, entity);

        PostEntityCreateEvent.Invoke(Parent, entity);

        if (!Parent.IsServerWorld) return;

        var addEntity = NetworkHandler.CreateMessage<AddEntity>();
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

        PreEntityDeleteEvent.Invoke(Parent, entity);
        _archetypeStorages[entity.ArchetypeId].RemoveEntity(entity);
        if (_entityOwner.ContainsKey(entity)) _entityOwner.Remove(entity);
        FreeEntityId(entity);
    }

    internal void RemoveEntity(Entity entity)
    {
        PreEntityDeleteEvent.Invoke(Parent, entity);
        _archetypeStorages[entity.ArchetypeId].RemoveEntity(entity);
        if (_entityOwner.ContainsKey(entity)) _entityOwner.Remove(entity);

        if (!Parent.IsServerWorld) return;
        var removeEntity =  NetworkHandler.CreateMessage<RemoveEntity>();
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
    public IEnumerable<Entity> GetEntitiesByOwner(ushort playerId)
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
        Logger.AssertAndThrow(ArchetypeManager.HasComponent(archetypeId, componentIdentification),
            $"Archetype {archetypeId} does not contain the component {componentIdentification}", "ECS");
    }

    private void AssertValidAccess()
    {
        Logger.AssertAndThrow(!Parent.IsExecuting,
            $"Accessing the {nameof(EntityManager)} is forbidden while the corresponding World is Executing", "ECS");
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
    
    //TODO make a better solution for this
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
            PreEntityDeleteEvent(Parent, new Entity(id, ids));

        foreach (var archetypeStorage in _archetypeStorages.Values) archetypeStorage.Dispose();
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

    internal void EnqueueDestroyEntity(Entity entity)
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