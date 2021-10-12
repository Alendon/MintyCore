using System.Linq;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    internal class AddEntity : IMessage
    {
        private Entity _entity;
        private ushort _owner;
        private IEntitySetup? _entitySetup;

        public ushort[] Receivers { get; private set; }
        public bool AutoSend => false;
        public bool IsServer { get; set; }
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.AddEntity;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;
        public void Serialize(DataWriter writer)
        {
            _entity.Serialize(writer);
            writer.Put(_owner);
            if (_entitySetup is null)
            {
                writer.Put((byte)0);
                return;
            }
            
            writer.Put((byte)1);
            _entitySetup.Serialize(writer);
        }

        public void Deserialize(DataReader reader)
        {
            _entity = Entity.Deserialize(reader);
            _owner = reader.GetUShort();
            var hasSetup = reader.GetByte();
            if (hasSetup == 1)
            {
                _entitySetup = EntityManager.EntitySetups[_entity.ArchetypeId];
                _entitySetup.Deserialize(reader);
            }
            
            if(IsServer) return;
            Engine.ClientWorld?.EntityManager.AddEntity(_entity, _owner, _entitySetup);
        }

        public void PopulateMessage(object? data = null)
        {
            var cData = (Data)data;
            _entity = cData.Entity;
            _owner = cData.Owner;
            _entitySetup = cData.EntitySetup;

            Receivers = Engine.PlayerIDs.Keys.ToArray();
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
            public IEntitySetup? EntitySetup { get; set; }
        }
    }
}