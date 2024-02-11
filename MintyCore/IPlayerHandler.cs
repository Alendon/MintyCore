using System.Collections.Generic;

namespace MintyCore;

public interface IPlayerHandler
{
    /// <summary>
    ///     Generic delegate for all player events with the player id and whether or not the event was fired server side
    /// </summary>
    public delegate void PlayerEvent(Player player, bool serverSide);
    
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
    ///     Event which gets fired when a player connects. May not be fired from the main thread!
    /// </summary>
    event PlayerEvent OnPlayerConnected;

    /// <summary>
    ///     Event which gets fired when a player disconnects. May not be fired from the main thread!
    /// </summary>
    event PlayerEvent OnPlayerDisconnected;

    event PlayerEvent OnPlayerReady;

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

    void AddPlayer(ushort gameId, string playerName, ulong playerId, bool serverSide);
    bool AddPlayer(string playerName, ulong playerId, out ushort id, bool serverSide);
    void TriggerPlayerReady(Player player);
}