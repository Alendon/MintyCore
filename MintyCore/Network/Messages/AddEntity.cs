using System.Linq;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    public class AddEntity : IMessage
    {
        private Entity _entity;
        private ushort _owner;
        
        public ushort[] Receivers { get; private set; }
        public bool AutoSend => false;
        public bool IsServer { get; set; }
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.AddEntity;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;
        public void Serialize(DataWriter writer)
        {
            _entity.Serialize(writer);
            writer.Put(_owner);
        }

        public void Deserialize(DataReader reader)
        {
            _entity = Entity.Deserialize(reader);
            _owner = reader.GetUShort();
            if(IsServer) return;
            MintyCore.ClientWorld?.EntityManager.CreateEntity(_entity, _owner);
        }

        public void PopulateMessage(object? data = null)
        {
            Data cData = (Data)data;
            _entity = cData.Entity;
            _owner = cData.Owner;

            Receivers = MintyCore._playerIDs.Keys.ToArray();
        }

        public void Clear()
        {
            _entity = default;
            _owner = default;
        }
        
        public struct Data
        {
            public Entity Entity;
            public ushort Owner;
        }
    }
}