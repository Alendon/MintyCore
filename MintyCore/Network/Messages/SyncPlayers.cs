using System;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message to sync player information
/// </summary>
[RegisterMessage("sync_players")]
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
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

    /// <inheritdoc />
    public ushort Sender { get; set; }

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
    public bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetInt(out var playerCount)) return false;

        for (var i = 0; i < playerCount; i++)
        {
            if (!reader.TryGetUShort(out var gameId) ||
                !reader.TryGetString(out var name) ||
                !reader.TryGetULong(out var id))
            {
                Logger.WriteLog("Failed to deserialize player information's", LogImportance.Error, "Network");
                return false;
            }

            PlayerHandler.AddPlayer(gameId, name, id, IsServer);
        }

        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
        Players = Array.Empty<(ushort playerGameId, string playerName, ulong playerId)>();
    }
}