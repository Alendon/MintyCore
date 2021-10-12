using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    internal class PlayerJoined : IMessage
    {
        private ushort _gameId;
        private string _playerName = string.Empty;
        private ulong  _playerId;

        public ushort[] Receivers { get; }
        public bool AutoSend => false;
        public bool IsServer { get; set; }
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.PlayerJoined;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

        public void Serialize(DataWriter writer)
        {
            writer.Put(_gameId);
            writer.Put(_playerName);
            writer.Put(_playerId);
        }

        public void Deserialize(DataReader reader)
        {
            _gameId = reader.GetUShort();
            _playerName = reader.GetString();
            _playerId = reader.GetULong();

            Engine.AddPlayer(_gameId, _playerName, _playerId);
            Engine.RaiseOnPlayerConnected(_gameId, false);
        }

        public void PopulateMessage(object? data = null)
        {
            if(data is not PlayerData playerData) return;
            _gameId = playerData.GameId;
            _playerId = playerData.PlayerId;
            _playerName = playerData.PlayerName;
        }

        public void Clear()
        {
            _gameId = default;
            _playerName = string.Empty;
            _playerId = default;
        }

        internal class PlayerData
        {
            public ushort GameId;
            public string PlayerName;
            public ulong PlayerId;

            public PlayerData(ushort gameId, string playerName, ulong playerId)
            {
                GameId = gameId;
                PlayerName = playerName;
                PlayerId = playerId;
            }
        }
    }
}