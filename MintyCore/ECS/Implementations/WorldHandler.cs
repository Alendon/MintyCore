using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Autofac;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Network.Messages;
using MintyCore.Utils;
using MintyCore.Utils.Events;
using MintyCore.Utils.Maths;
using Serilog;

namespace MintyCore.ECS.Implementations;

/// <summary>
/// General class to handle all created worlds
/// </summary>
[PublicAPI]
[Singleton<IWorldHandler>]
internal class WorldHandler : IWorldHandler
{
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _worldContainerBuilder = new();
    private readonly Dictionary<Identification, IWorld> _serverWorlds = new();
    private readonly Dictionary<Identification, IWorld> _clientWorlds = new();
    private ILifetimeScope? _worldLifetimeScope;

    /// <summary/>
    public required IModManager ModManager { private get; init; }
    
    /// <summary/>
    public required IComponentManager ComponentManager { private get; init; }

    /// <summary/>
    public required IArchetypeManager ArchetypeManager { private get; init; }

    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; init; }

    /// <summary/>
    public required INetworkHandler NetworkHandler { private get; init; }
    
    public required IEventBus EventBus { private get; init; }

    /// <summary/>
    /// <summary>
    /// Event which gets fired right after a world gets created
    /// The <see cref="IWorld"/> parameter is the world which was created
    /// </summary>
    public event Action<IWorld> OnWorldCreate = delegate { };

    /// <summary>
    /// Event which gets fired right before a world gets destroyed
    /// The <see cref="IWorld"/> parameter is the world which will be destroyed
    /// </summary>
    public event Action<IWorld> OnWorldDestroy = delegate { };

    /// <summary>
    /// Event which gets fired before a specific World gets updated
    /// The <see cref="IWorld"/> parameter is the world which will be updated
    /// </summary>
    public event Action<IWorld> BeforeWorldUpdate = delegate { };

    /// <summary>
    /// Event which gets fired after a specific World was updated
    /// The <see cref="IWorld"/> parameter is the world which was updated
    /// </summary>
    public event Action<IWorld> AfterWorldUpdate = delegate { };
    
    /// <inheritdoc />
    public void AddWorld<TWorld>(Identification worldId) where TWorld : class, IWorld
    {
        _worldContainerBuilder[worldId] = builder =>
        {
            //I don't like this solution, but i need to set the IsServerWorld property
            builder.RegisterType<TWorld>().Keyed<IWorld>((worldId, GameType.Client)).As<IWorld>()
                .WithProperty(nameof(IWorld.IsServerWorld), false)
                .WithParameter("isServerWorld", false)
                .ExternallyOwned();

            builder.RegisterType<TWorld>().Keyed<IWorld>((worldId, GameType.Server)).As<IWorld>()
                .WithParameter("isServerWorld", true)
                .ExternallyOwned();
        };
        InvalidateLifetimeScope();
    }

    private void InvalidateLifetimeScope()
    {
        foreach (var world in _serverWorlds.Values)
        {
            OnWorldDestroy(world);
            world.Dispose();
        }

        foreach (var world in _clientWorlds.Values)
        {
            OnWorldDestroy(world);
            world.Dispose();
        }

        _clientWorlds.Clear();
        _serverWorlds.Clear();
        _worldLifetimeScope?.Dispose();
        _worldLifetimeScope = null;
    }

    bool _playerEventRegistered;
    
    public void PostRegister()
    {
        _worldLifetimeScope = ModManager.ModLifetimeScope.BeginLifetimeScope(builder =>
        {
            foreach (var (_, value) in _worldContainerBuilder) value(builder);
        });

        if (_playerEventRegistered) return;

        var binding = new EventBinding<PlayerEvent>(OnPlayerEvent);
        
        EventBus.AddListener(binding);
        
        _playerEventRegistered = true;
    }

    private EventResult OnPlayerEvent(PlayerEvent e)
    {
        if (e.Type != PlayerEvent.EventType.Disconnected ||
            e.ServerSide == false) return EventResult.Continue;

        foreach (var world in GetWorlds(GameType.Server))
        {
            var playerEntities = world.EntityManager.GetEntitiesByOwner(e.Player);
                
            foreach (var entity in playerEntities)
            {
                world.EntityManager.EnqueueDestroyEntity(entity);
            }
        }
        
        return EventResult.Continue;
    }

    public void Clear()
    {
        DestroyWorlds(GameType.Local);

        OnWorldCreate = delegate { };
        OnWorldDestroy = delegate { };
        BeforeWorldUpdate = delegate { };
        AfterWorldUpdate = delegate { };
    }

    /// <summary>
    /// Try get a specific world
    /// </summary>
    /// <param name="worldType">Type of the world. Has to be <see cref="GameType.Server"/> or <see cref="GameType.Client"/></param>
    /// <param name="worldId"><see cref="Identification"/> of the world</param>
    /// <param name="world">The fetched world. Null if not found</param>
    /// <returns>True if found</returns>
    public bool TryGetWorld(GameType worldType, Identification worldId, [MaybeNullWhen(false)] out IWorld world)
    {
        if (worldType is not (GameType.Client or GameType.Server))
        {
            throw new MintyCoreException(
                $"{nameof(TryGetWorld)} must be invoked with {nameof(GameType.Server)} or {nameof(GameType.Client)}");
        }

        switch (worldType)
        {
            case GameType.Client:
            {
                return _clientWorlds.TryGetValue(worldId, out world);
            }
            case GameType.Server:
            {
                return _serverWorlds.TryGetValue(worldId, out world);
            }
        }

        world = null;
        return false;
    }

    /// <summary>
    /// Get an enumeration with all worlds for the given game type
    /// </summary>
    /// <param name="worldType">GameType of the world. Has to be <see cref="GameType.Server"/> or <see cref="GameType.Client"/></param>
    /// <returns>Enumerable containing all worlds</returns>
    public IEnumerable<IWorld> GetWorlds(GameType worldType)
    {
        if (worldType is not (GameType.Client or GameType.Server))
            throw new MintyCoreException(
                $"{nameof(GetWorlds)} must be invoked with {nameof(GameType.Server)} or {nameof(GameType.Client)}");

        return worldType == GameType.Client ? _clientWorlds.Values : _serverWorlds.Values;
    }

    /// <summary>
    /// Create all available worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get created</param>
    public void CreateWorlds(GameType worldType)
    {
        foreach (var worldId in _worldContainerBuilder.Keys) CreateWorld(worldType, worldId);
    }

    /// <summary>
    /// Create the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get created</param>
    /// <param name="worlds">Enumerable containing the worlds to create</param>
    public void CreateWorlds(GameType worldType, IEnumerable<Identification> worlds)
    {
        foreach (var worldId in worlds) CreateWorld(worldType, worldId);
    }

    /// <summary>
    /// Create the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get created</param>
    /// <param name="worlds">Worlds to create</param>
    public void CreateWorlds(GameType worldType, params Identification[] worlds)
    {
        foreach (var worldId in worlds) CreateWorld(worldType, worldId);
    }

    /// <summary>
    /// Create a specific world
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get created</param>
    /// <param name="worldId">The id of the world to create</param>
    public void CreateWorld(GameType worldType, Identification worldId)
    {
        if (_worldLifetimeScope is null)
        {
            throw new MintyCoreException("WorldLifetimeScope is null");
        }

        var clientWorld = MathHelper.IsBitSet((int)worldType, (int)GameType.Client);
        var serverWorld = MathHelper.IsBitSet((int)worldType, (int)GameType.Server);

        if (clientWorld)
        {
            if (_clientWorlds.ContainsKey(worldId))
            {
                Log.Warning("A client world with id {WorldId} is already created", worldId);
            }
            else
            {
                Log.Information("Create client world with id {WorldId}", worldId);
                var world = _worldLifetimeScope.ResolveKeyed<IWorld>((worldId, GameType.Client));

                _clientWorlds.Add(worldId, world);
                OnWorldCreate(world);
            }
        }

        // ReSharper disable once InvertIf; keep it in the same style as above
        if (serverWorld)
        {
            if (_serverWorlds.ContainsKey(worldId))
            {
                Log.Warning("A server world with id {WorldId} is already created", worldId);
            }
            else
            {
                Log.Information("Create server world with id {WorldId}", worldId);
                var world = _worldLifetimeScope.ResolveKeyed<IWorld>((worldId, GameType.Server));
                _serverWorlds.Add(worldId, world);
                OnWorldCreate(world);
            }
        }
    }

    /// <summary>
    /// Destroy all worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    public void DestroyWorlds(GameType worldType)
    {
        foreach (var worldId in _worldContainerBuilder.Keys) DestroyWorld(worldType, worldId);
    }

    /// <summary>
    /// Destroy the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    /// <param name="worlds">Enumerable containing the worlds to destroy</param>
    public void DestroyWorlds(GameType worldType, IEnumerable<Identification> worlds)
    {
        foreach (var worldId in worlds) DestroyWorld(worldType, worldId);
    }

    /// <summary>
    /// Destroy the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    /// <param name="worlds">Worlds to destroy</param>
    public void DestroyWorlds(GameType worldType, params Identification[] worlds)
    {
        foreach (var worldId in worlds) DestroyWorld(worldType, worldId);
    }

    /// <summary>
    /// Destroy a specific world
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    /// <param name="worldId">The id of the world to destroy</param>    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DestroyWorld(GameType worldType, Identification worldId)
    {
        // ReSharper disable once InlineOutVariableDeclaration; A inline declaration prevents null checking
        IWorld world;

        var isClientWorld = MathHelper.IsBitSet((int)worldType, (int)GameType.Client);
        var isServerWorld = MathHelper.IsBitSet((int)worldType, (int)GameType.Server);

        if (isClientWorld)
        {
            if (!_clientWorlds.Remove(worldId, out world!))
                Log.Warning("No client world with id {WorldId} present to destroy", worldId);
            else
                DestroyWorld(world);
        }

        // ReSharper disable once InvertIf; Keep consistency between both blocks
        if (isServerWorld)
        {
            if (!_serverWorlds.Remove(worldId, out world!))
                Log.Warning("No server world with id {WorldId} present to destroy", worldId);
            else
                DestroyWorld(world);
        }
    }

    /// <summary>
    /// Destroys a specific world
    /// </summary>
    /// <param name="world">World to destroy</param>
    private void DestroyWorld(IWorld world)
    {
        Log.Information("Destroy {ServerClient} world with id {WorldIdentification}",
            world.IsServerWorld ? "server" : "client", world.Identification);
        OnWorldDestroy(world);
        world.Dispose();
    }

    /// <summary>
    /// Send all entities of all worlds to the specified player
    /// </summary>
    /// <param name="player"></param>
    public void SendEntitiesToPlayer(Player player)
    {
        foreach (var worldId in _serverWorlds.Keys) SendEntitiesToPlayer(player, worldId);
    }

    /// <summary>
    /// Send all entities of the given worlds to the specified player
    /// </summary>
    /// <param name="player">Player to send entities to</param>
    /// <param name="worlds">Ids of worlds to send entities from</param>
    public void SendEntitiesToPlayer(Player player, IEnumerable<Identification> worlds)
    {
        foreach (var worldId in worlds) SendEntitiesToPlayer(player, worldId);
    }

    /// <summary>
    /// Send all entities of the given worlds to the specified player
    /// </summary>
    /// <param name="player">Player to send entities to</param>
    /// <param name="worlds">Ids of worlds to send entities from</param>
    public void SendEntitiesToPlayer(Player player, params Identification[] worlds)
    {
        foreach (var worldId in worlds) SendEntitiesToPlayer(player, worldId);
    }

    /// <summary>
    /// Send all entities of the given worlds to the specified player
    /// </summary>
    /// <param name="player">Player to send entities to</param>
    /// <param name="worldId">Id of the world to send entities from</param>
    public void SendEntitiesToPlayer(Player player, Identification worldId)
    {
        if(!_serverWorlds.TryGetValue(worldId, out var world))
        {
            Log.Error("Cant send entities to player, server world {WorldId} does not exist", worldId);
            return;
        }

        using var sendEntityData = NetworkHandler.CreateMessage<SendEntityData>();
        foreach (var entity in world.EntityManager.Entities)
        {
            sendEntityData.Entity = entity;
            sendEntityData.EntityOwner = world.EntityManager.GetEntityOwner(entity);
            sendEntityData.WorldId = worldId;

            sendEntityData.Send(player);
        }
    }

    /// <summary>
    /// Send entity updates for all worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"><see cref="GameType"/> worlds to send entity updates</param>
    public void SendEntityUpdates(GameType worldTypeToUpdate = GameType.Local)
    {
        foreach (var worldId in _worldContainerBuilder.Keys) SendEntityUpdate(worldTypeToUpdate, worldId);
    }

    /// <summary>
    /// Send entity updates for the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    public void SendEntityUpdates(GameType worldTypeToUpdate, IEnumerable<Identification> worldsToUpdate)
    {
        foreach (var worldId in worldsToUpdate) SendEntityUpdate(worldTypeToUpdate, worldId);
    }

    /// <summary>
    /// Send entity updates for the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    public void SendEntityUpdates(GameType worldTypeToUpdate, params Identification[] worldsToUpdate)
    {
        foreach (var worldId in worldsToUpdate) SendEntityUpdate(worldTypeToUpdate, worldId);
    }

    /// <summary>
    /// Send entity updates for the given world
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldToUpdate"></param>
    public void SendEntityUpdate(GameType worldTypeToUpdate, Identification worldToUpdate)
    {
        if (MathHelper.IsBitSet((int)worldTypeToUpdate, (int)GameType.Client) &&
            _clientWorlds.TryGetValue(worldToUpdate, out var world))
            SendEntityUpdate(world);

        if (MathHelper.IsBitSet((int)worldTypeToUpdate, (int)GameType.Server) &&
            _serverWorlds.TryGetValue(worldToUpdate, out world))
            SendEntityUpdate(world);
    }

    /// <summary>
    /// Send entity updates for the given world
    /// </summary>
    public void SendEntityUpdate(IWorld world)
    {
        using var message = NetworkHandler.CreateMessage<ComponentUpdate>();
        message.WorldGameType = world.IsServerWorld ? GameType.Server : GameType.Client;
        message.WorldId = world.Identification;

        var updateDic = message.Components;

        foreach (var archetypeId in ArchetypeManager.GetArchetypes().Keys)
        {
            var storage = world.EntityManager.GetArchetypeStorage(archetypeId);
            foreach (var component in storage.GetDirtyEnumerator())
            {
                var playerControlled = ComponentManager.IsPlayerControlled(component.componentId);

                switch (world.IsServerWorld)
                {
                    //if server world and player controlled; we can skip
                    case true when playerControlled:
                    //if client world but not player controlled; we can skip
                    case false when !playerControlled:
                    //if client world and player controlled but the wrong player locally; we can skip
                    case false when playerControlled && world.EntityManager.GetEntityOwner(component.entity) !=
                        PlayerHandler.LocalPlayer:
                        continue;
                }

                if (!updateDic.ContainsKey(component.entity))
                    updateDic.Add(component.entity, ComponentUpdate.GetComponentsList());

                var componentList = updateDic[component.entity];
                componentList.Add((component.componentId, component.componentPtr));
            }
        }

        if (world.IsServerWorld)
            message.Send(PlayerHandler.GetConnectedPlayers());
        else
            message.SendToServer();

        message.Clear();
    }

    public void UpdateWorlds(GameType worldTypeToUpdate, bool simulationEnable)
    {
        foreach (var worldId in _worldContainerBuilder.Keys)
            UpdateWorld(worldTypeToUpdate, worldId, simulationEnable);
    }

    /// <summary>
    /// Update the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    /// <param name="simulationEnable"></param>
    public void UpdateWorlds(GameType worldTypeToUpdate, bool simulationEnable,
        IEnumerable<Identification> worldsToUpdate)
    {
        foreach (var worldId in worldsToUpdate)
            UpdateWorld(worldTypeToUpdate, worldId, simulationEnable);
    }

    /// <summary>
    /// Update the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    /// <param name="simulationEnable"></param>
    public void UpdateWorlds(GameType worldTypeToUpdate, bool simulationEnable, params Identification[] worldsToUpdate)
    {
        foreach (var worldId in worldsToUpdate)
            UpdateWorld(worldTypeToUpdate, worldId, simulationEnable);
    }

    /// <summary>
    /// Update a specific world
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldToUpdate"></param>
    /// <param name="simulationEnable"></param>
    public void UpdateWorld(GameType worldTypeToUpdate, Identification worldToUpdate, bool simulationEnable)
    {
        if (MathHelper.IsBitSet((int)worldTypeToUpdate, (int)GameType.Client) &&
            _clientWorlds.TryGetValue(worldToUpdate, out var world))
            UpdateWorld(world, simulationEnable);

        if (MathHelper.IsBitSet((int)worldTypeToUpdate, (int)GameType.Server) &&
            _serverWorlds.TryGetValue(worldToUpdate, out world))
            UpdateWorld(world, simulationEnable);
    }

    /// <summary>
    /// Updates a specific world
    /// </summary>
    /// <param name="world">World to update</param>
    /// <param name="simulationEnable"></param>
    private void UpdateWorld(IWorld world, bool simulationEnable)
    {
        BeforeWorldUpdate(world);

        //Disable simulation for one tick
        var reenableSimulation = world.SystemManager.GetSystemActive(SystemIDs.SimulationGroup);
        if (!simulationEnable) world.SystemManager.SetSystemActive(SystemIDs.SimulationGroup, false);

        world.EntityManager.Update();
        world.Tick();

        if (reenableSimulation) world.SystemManager.SetSystemActive(SystemIDs.SimulationGroup, true);

        AfterWorldUpdate(world);
    }

    /// <inheritdoc />
    public void RemoveWorld(Identification objectId)
    {
        _worldContainerBuilder.Remove(objectId);
        InvalidateLifetimeScope();
    }
}