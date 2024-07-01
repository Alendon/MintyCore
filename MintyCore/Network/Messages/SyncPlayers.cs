using System;
using LiteNetLib;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message to sync player information
/// </summary>
[RegisterMessage("sync_players")]
public class SyncPlayers : Message
{
    internal Player[] Players = [];


    /// <inheritdoc />
    public override bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public override Identification MessageId => MessageIDs.SyncPlayers;

    /// <inheritdoc />
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;


    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; init; }


    /// <inheritdoc />
    public override void Serialize(DataWriter writer)
    {
        writer.Put(Players.Length);
        foreach (var player in Players)
        {
            writer.Put(player.GameId);
            writer.Put(player.Name);
            writer.Put(player.GlobalId);
        }
    }

    /// <inheritdoc />
    public override bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetInt(out var playerCount)) return false;

        for (var i = 0; i < playerCount; i++)
        {
            if (!reader.TryGetUShort(out var gameId) ||
                !reader.TryGetString(out var name) ||
                !reader.TryGetULong(out var globalId))
            {
                Log.Error("Failed to deserialize player information's");
                return false;
            }

            PlayerHandler.AddPlayer(gameId, name, globalId, IsServer);
        }

        return true;
    }

    /// <inheritdoc />
    public override void Clear()
    {
        Players = [];
    }
}