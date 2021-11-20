using System;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    public partial class PlayerLeft : IMessage
    {
        internal ushort PlayerGameId;

        public ushort[] Receivers { set; get; }
        public bool IsServer { get; set; }
        public bool ReceiveMultiThreaded => true;

        public Identification MessageId => MessageIDs.PlayerLeft;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

        public void Serialize(DataWriter writer)
        {
            writer.Put(PlayerGameId);
        }

        public void Deserialize(DataReader reader)
        {
            PlayerGameId = reader.GetUShort();

            //Check if its not a local game, as there the method was already called before
            if (Engine.GameType == GameType.CLIENT)
            {
                Engine.DisconnectPlayer(PlayerGameId, IsServer);
            }
        }
        

        public void Clear()
        {
            PlayerGameId = 0;
            Receivers = Array.Empty<ushort>();
        }
    }
}