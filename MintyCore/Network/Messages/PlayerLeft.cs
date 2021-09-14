using System;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    public class PlayerLeft : IMessage
    {
        private ushort _playerGameId;

        public ushort[] Receivers { set; get; }
        public bool AutoSend => false;
        public bool IsServer { get; set; }
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.PlayerLeft;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

        public void Serialize(DataWriter writer)
        {
            writer.Put(_playerGameId);
        }

        public void Deserialize(DataReader reader)
        {
            _playerGameId = reader.GetUShort();

            //Check if its not a local game, as there the method was already called before
            if (MintyCore.GameType == GameType.CLIENT)
                MintyCore.RemovePlayer(_playerGameId);
        }

        public void PopulateMessage(object? data = null)
        {
            if (data is not Data parsedData) return;
            _playerGameId = parsedData.PlayerGameId;
        }

        public void Clear()
        {
            _playerGameId = 0;
            Receivers = Array.Empty<ushort>();
        }

        public class Data
        {
            public ushort PlayerGameId;

            public Data(ushort playerGameId)
            {
                PlayerGameId = playerGameId;
            }
        }
    }
}