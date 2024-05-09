using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyCore.Utils.Events;

namespace MintyCore;

/// <summary>
///   Interface for handling players
/// </summary>
[PublicAPI]
public interface IPlayerHandler
{
    /// <summary>
    ///     The game id of the local player
    /// </summary>
    ushort LocalPlayerGameId { get; set; }

    /// <summary>
    ///     The global id of the local player
    /// </summary>
    ulong LocalPlayerId { get; set; }

    /// <summary>
    ///     The name of the local player
    /// </summary>
    string LocalPlayerName { get; set; }

    /// <summary>
    ///     Get all connected players
    /// </summary>
    /// <returns>IEnumerable containing the player game ids</returns>
    IEnumerable<ushort> GetConnectedPlayers();

    /// <summary>
    ///     Get the name of a player
    /// </summary>
    /// <param name="gameId">The player game id</param>
    /// <returns>Player name</returns>
    string GetPlayerName(ushort gameId);

    /// <summary>
    ///     Get the player global id
    /// </summary>
    /// <param name="gameId">The player game id</param>
    /// <returns></returns>
    ulong GetPlayerId(ushort gameId);

    /// <summary>
    /// Get a player object by the player id
    /// </summary>
    /// <param name="gameId">Game id of the player</param>
    /// <returns></returns>
    Player GetPlayer(ushort gameId);

    /// <summary>
    ///     Gets called when a player disconnects
    /// </summary>
    /// <param name="player"></param>
    /// <param name="serverSide"></param>
    void DisconnectPlayer(ushort player, bool serverSide);

    ///<summary/>
    void AddPlayer(ushort gameId, string playerName, ulong playerId, bool serverSide);

    ///<summary/>
    bool AddPlayer(string playerName, ulong playerId, out ushort id, bool serverSide);
}

[RegisterEvent("player_event")]
public struct PlayerEvent : IEvent
{
    public static Identification Identification => EventIDs.PlayerEvent;
    public static bool ModificationAllowed => false;

    public required Player Player { get; init; }
    public required bool ServerSide { get; init; }
    public required EventType Type { get; init; }

    public enum EventType
    {
        Connected,
        Disconnected,
        Ready
    }
}