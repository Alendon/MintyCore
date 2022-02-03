using System;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
/// Message to sync player information
/// </summary>
public partial class SyncPlayers : IMessage
{
    internal (ushort playerGameId, string playerName, ulong playerId)[] Players =
        Array.Empty<(ushort playerGameId, string playerName, ulong playerId)>();

    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.SyncPlayers;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        writer.Put(Players.Length);
        foreach (var (playerGameId, playerName, playerId) in Players)
        {
            writer.Put(playerGameId);
            writer.Put(playerName);
            writer.Put(playerId);
        }
    }

    /// <inheritdoc />
    public void Deserialize(DataReader reader)
    {
        var playerCount = reader.GetInt();

        for (var i = 0; i < playerCount; i++)
        {
            var gameId = reader.GetUShort();
            var name = reader.GetString();
            var id = reader.GetULong();

            Engine.AddPlayer(gameId, name, id, IsServer);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        Players = Array.Empty<(ushort playerGameId, string playerName, ulong playerId)>();
    }
}