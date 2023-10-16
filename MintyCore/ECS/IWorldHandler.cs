using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MintyCore.Utils;

namespace MintyCore.ECS;

public interface IWorldHandler
{
    /// <summary>
    /// Event which gets fired right after a world gets created
    /// The <see cref="IWorld"/> parameter is the world which was created
    /// </summary>
    event Action<IWorld> OnWorldCreate;

    /// <summary>
    /// Event which gets fired right before a world gets destroyed
    /// The <see cref="IWorld"/> parameter is the world which will be destroyed
    /// </summary>
    event Action<IWorld> OnWorldDestroy;

    /// <summary>
    /// Event which gets fired before a specific World gets updated
    /// The <see cref="IWorld"/> parameter is the world which will be updated
    /// </summary>
    event Action<IWorld> BeforeWorldUpdate;

    /// <summary>
    /// Event which gets fired after a specific World was updated
    /// The <see cref="IWorld"/> parameter is the world which was updated
    /// </summary>
    event Action<IWorld> AfterWorldUpdate;

    void AddWorld<TWorld>(Identification worldId) where TWorld : class, IWorld;
    void CreateWorldLifetimeScope();
    void Clear();

    /// <summary>
    /// Try get a specific world
    /// </summary>
    /// <param name="worldType">Type of the world. Has to be <see cref="GameType.Server"/> or <see cref="GameType.Client"/></param>
    /// <param name="worldId"><see cref="Identification"/> of the world</param>
    /// <param name="world">The fetched world. Null if not found</param>
    /// <returns>True if found</returns>
    bool TryGetWorld(GameType worldType, Identification worldId, [MaybeNullWhen(false)] out IWorld world);

    /// <summary>
    /// Get an enumeration with all worlds for the given game type
    /// </summary>
    /// <param name="worldType">GameType of the world. Has to be <see cref="GameType.Server"/> or <see cref="GameType.Client"/></param>
    /// <returns>Enumerable containing all worlds</returns>
    IEnumerable<IWorld> GetWorlds(GameType worldType);

    /// <summary>
    /// Create all available worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get created</param>
    void CreateWorlds(GameType worldType);

    /// <summary>
    /// Create the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get created</param>
    /// <param name="worlds">Enumerable containing the worlds to create</param>
    void CreateWorlds(GameType worldType, IEnumerable<Identification> worlds);

    /// <summary>
    /// Create the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get created</param>
    /// <param name="worlds">Worlds to create</param>
    void CreateWorlds(GameType worldType, params Identification[] worlds);

    /// <summary>
    /// Create a specific world
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get created</param>
    /// <param name="worldId">The id of the world to create</param>
    void CreateWorld(GameType worldType, Identification worldId);

    /// <summary>
    /// Destroy all worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    void DestroyWorlds(GameType worldType);

    /// <summary>
    /// Destroy the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    /// <param name="worlds">Enumerable containing the worlds to destroy</param>
    void DestroyWorlds(GameType worldType, IEnumerable<Identification> worlds);

    /// <summary>
    /// Destroy the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    /// <param name="worlds">Worlds to destroy</param>
    void DestroyWorlds(GameType worldType, params Identification[] worlds);

    /// <summary>
    /// Destroy a specific world
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    /// <param name="worldId">The id of the world to destroy</param>    
    void DestroyWorld(GameType worldType, Identification worldId);

    /// <summary>
    /// Send all entities of all worlds to the specified player
    /// </summary>
    /// <param name="player"></param>
    void SendEntitiesToPlayer(Player player);

    /// <summary>
    /// Send all entities of the given worlds to the specified player
    /// </summary>
    /// <param name="player">Player to send entities to</param>
    /// <param name="worlds">Ids of worlds to send entities from</param>
    void SendEntitiesToPlayer(Player player, IEnumerable<Identification> worlds);

    /// <summary>
    /// Send all entities of the given worlds to the specified player
    /// </summary>
    /// <param name="player">Player to send entities to</param>
    /// <param name="worlds">Ids of worlds to send entities from</param>
    void SendEntitiesToPlayer(Player player, params Identification[] worlds);

    /// <summary>
    /// Send all entities of the given worlds to the specified player
    /// </summary>
    /// <param name="player">Player to send entities to</param>
    /// <param name="worldId">Id of the world to send entities from</param>
    void SendEntitiesToPlayer(Player player, Identification worldId);

    /// <summary>
    /// Send entity updates for all worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"><see cref="GameType"/> worlds to send entity updates</param>
    void SendEntityUpdates(GameType worldTypeToUpdate = GameType.Local);

    /// <summary>
    /// Send entity updates for the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    void SendEntityUpdates(GameType worldTypeToUpdate, IEnumerable<Identification> worldsToUpdate);

    /// <summary>
    /// Send entity updates for the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    void SendEntityUpdates(GameType worldTypeToUpdate, params Identification[] worldsToUpdate);

    /// <summary>
    /// Send entity updates for the given world
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldToUpdate"></param>
    void SendEntityUpdate(GameType worldTypeToUpdate, Identification worldToUpdate);

    /// <summary>
    /// Send entity updates for the given world
    /// </summary>
    void SendEntityUpdate(IWorld world);

    /// <summary>
    /// Update all worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"><see cref="GameType"/> worlds to update</param>
    /// <param name="simulationEnable"></param>
    /// <param name="drawingEnable">Whether or not the <see cref="SystemGroups.PresentationSystemGroup"/> get executed</param>
    void UpdateWorlds(GameType worldTypeToUpdate, bool simulationEnable, bool drawingEnable);

    /// <summary>
    /// Update the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    /// <param name="simulationEnable"></param>
    /// <param name="drawingEnable">Whether or not the <see cref="SystemGroups.PresentationSystemGroup"/> get executed</param>
    void UpdateWorlds(GameType worldTypeToUpdate, bool simulationEnable, bool drawingEnable,
        IEnumerable<Identification> worldsToUpdate);

    /// <summary>
    /// Update the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    /// <param name="simulationEnable"></param>
    /// <param name="drawingEnable">Whether or not the <see cref="SystemGroups.PresentationSystemGroup"/> get executed</param>
    void UpdateWorlds(GameType worldTypeToUpdate, bool simulationEnable, bool drawingEnable,
        params Identification[] worldsToUpdate);

    /// <summary>
    /// Update a specific world
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldToUpdate"></param>
    /// <param name="simulationEnable"></param>
    /// <param name="drawingEnable">Whether or not the <see cref="SystemGroups.PresentationSystemGroup"/> get executed</param>
    void UpdateWorld(GameType worldTypeToUpdate, Identification worldToUpdate, bool simulationEnable,
        bool drawingEnable);

    void RemoveWorld(Identification objectId);
}