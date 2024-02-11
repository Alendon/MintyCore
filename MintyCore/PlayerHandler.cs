using System.Collections.Generic;
using MintyCore.Network;
using MintyCore.Network.Messages;
using MintyCore.Utils;

namespace MintyCore;

/// <summary>
///     Class to handle the connected players
/// </summary>
[Singleton<IPlayerHandler>]
internal class PlayerHandler : IPlayerHandler
{
    private readonly object _lock = new();

    private readonly Dictionary<ushort, Player> _players = new();

    public INetworkHandler NetworkHandler { set; private get; } = null!;

    /// <summary>
    ///     The game id of the local player
    /// </summary>
    public ushort LocalPlayerGameId { get; set; } = Constants.InvalidId;

    /// <summary>
    ///     The global id of the local player
    /// </summary>
    public ulong LocalPlayerId { get; set; } = Constants.InvalidId;

    /// <summary>
    ///     The name of the local player
    /// </summary>
    public string LocalPlayerName { get; set; } = "Player";

    /// <summary>
    ///     Event which gets fired when a player connects. May not be fired from the main thread!
    /// </summary>
    public event IPlayerHandler.PlayerEvent OnPlayerConnected = delegate { };

    /// <summary>
    ///     Event which gets fired when a player disconnects. May not be fired from the main thread!
    /// </summary>
    public event IPlayerHandler.PlayerEvent OnPlayerDisconnected = delegate { };
    
    public event IPlayerHandler.PlayerEvent OnPlayerReady = delegate { };

    /// <summary>
    ///     Get all connected players
    /// </summary>
    /// <returns>IEnumerable containing the player game ids</returns>
    public IEnumerable<ushort> GetConnectedPlayers()
    {
        Dictionary<ushort, Player>.KeyCollection players;
        lock (_lock)
        {
            players = _players.Keys;
        }

        return players;
    }

    /// <summary>
    ///     Get the name of a player
    /// </summary>
    /// <param name="gameId">The player game id</param>
    /// <returns>Player name</returns>
    public string GetPlayerName(ushort gameId)
    {
        string name;
        lock (_lock)
        {
            name = _players[gameId].Name;
        }

        return name;
    }

    /// <summary>
    ///     Get the player global id
    /// </summary>
    /// <param name="gameId">The player game id</param>
    /// <returns></returns>
    public ulong GetPlayerId(ushort gameId)
    {
        ulong id;
        lock (_lock)
        {
            id = _players[gameId].GlobalId;
        }

        return id;
    }

    /// <summary>
    /// Get a player object by the player id
    /// </summary>
    /// <param name="gameId">Game id of the player</param>
    /// <returns></returns>
    public Player GetPlayer(ushort gameId)
    {
        Player player;
        lock (_lock)
        {
            player = _players[gameId];
        }

        return player;
    }

    /// <summary>
    ///     Gets called when a player disconnects
    /// </summary>
    /// <param name="player"></param>
    /// <param name="serverSide"></param>
    public void DisconnectPlayer(ushort player, bool serverSide)
    {
        lock (_lock)
        {
            OnPlayerDisconnected(_players[player], serverSide);
        }

        RemovePlayer(player);
        if (!serverSide || Engine.GameType == GameType.Local) return;

        RemovePlayerEntities(player);

        var message = NetworkHandler.CreateMessage<PlayerLeft>();
        message.PlayerGameId = player;
        
        message.Send(GetConnectedPlayers());
    }

    private void RemovePlayer(ushort playerId)
    {
        lock (_lock)
        {
            _players.Remove(playerId, out var player);

            if (player is not null) player.IsConnected = false;
        }
    }

    private static void RemovePlayerEntities(ushort playerId)
    {
        Engine.RemoveEntitiesByPlayer(playerId);
    }

    public void AddPlayer(ushort gameId, string playerName, ulong playerId, bool serverSide)
    {
        lock (_lock)
        {
            if (_players.ContainsKey(gameId)) return;

            var player = new Player(gameId, playerId, playerName);
            _players.Add(gameId, player);
            OnPlayerConnected(player, serverSide);
        }
    }

    public bool AddPlayer(string playerName, ulong playerId, out ushort id, bool serverSide)
    {
        lock (_lock)
        {
            id = Constants.ServerId + 1;
            while (_players.ContainsKey(id)) id++;

            var player = new Player(id, playerId, playerName);

            _players.Add(id, player);
            OnPlayerConnected(player, serverSide);
        }

        return true;
    }

    public void TriggerPlayerReady(Player player)
    {
        OnPlayerReady(player, true);
    }
}