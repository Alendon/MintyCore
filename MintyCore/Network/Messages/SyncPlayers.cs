using System;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

public partial class SyncPlayers : IMessage
{
    internal (ushort playerGameId, string playerName, ulong playerId)[] Players =
        Array.Empty<(ushort playerGameId, string playerName, ulong playerId)>();


    public ushort[]? Receivers { private set; get; }
    public bool IsServer { get; set; }
    public bool ReceiveMultiThreaded => false;

    public Identification MessageId => MessageIDs.SyncPlayers;
    public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

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

    public void Clear()
    {
        Players = Array.Empty<(ushort playerGameId, string playerName, ulong playerId)>();
    }
}