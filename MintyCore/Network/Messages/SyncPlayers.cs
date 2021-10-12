using System;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    public class SyncPlayers : IMessage
    {
        private (ushort playerGameId, string playerName, ulong playerId)[] _players =
            Array.Empty<(ushort playerGameId, string playerName, ulong playerId)>();


        public ushort[]? Receivers { private set; get; }
        public bool AutoSend => false;
        public bool IsServer { get; set; }
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.SyncPlayers;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;
        public void Serialize(DataWriter writer)
        {
            writer.Put(_players.Length);
            foreach (var (playerGameId, playerName, playerId) in _players)
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

                Engine.AddPlayer(gameId, name, id);
            }
        }

        public void PopulateMessage(object? data = null)
        {
            if (data is not PlayerData playerData) return;
            _players = playerData.Players;
            Receivers = new[] { playerData.Receiver };
        }

        public void Clear()
        {
            _players = Array.Empty<(ushort playerGameId, string playerName, ulong playerId)>();
        }

        public class PlayerData
        {
            public readonly (ushort playerGameId, string playerName, ulong playerId)[] Players;
            public readonly ushort Receiver;

            public PlayerData((ushort playerGameId, string playerName, ulong playerId)[] players, ushort receiver)
            {
                Players = players;
                Receiver = receiver;
            }
        }
    }
}