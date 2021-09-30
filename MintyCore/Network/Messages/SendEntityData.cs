using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    public class SendEntityData : IMessage
    {
        private Entity _entity;
        private ushort _entityOwner;

        public ushort[] Receivers { private set; get; }
        public bool AutoSend => false;
        public bool IsServer { get; set; }
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.SendEntityData;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

        public void Serialize(DataWriter writer)
        {
            if(MintyCore.ServerWorld is null) return;
            
            _entity.Serialize(writer);
            writer.Put(_entityOwner);
            
            var componentIDs = ArchetypeManager.GetArchetype(_entity.ArchetypeId).ArchetypeComponents;
            
            writer.Put(componentIDs.Count);
            foreach (var componentId in componentIDs)
            {
                var componentPtr = MintyCore.ServerWorld.EntityManager.GetComponentPtr(_entity, componentId);
                
                componentId.Serialize(writer);
                ComponentManager.SerializeComponent(componentPtr, componentId, writer);
            }
        }

        public void Deserialize(DataReader reader)
        {
            if(MintyCore.ClientWorld is null) return;
            
            _entity = Entity.Deserialize(reader);
            _entityOwner = reader.GetUShort();
            
            MintyCore.ClientWorld.EntityManager.AddEntity(_entity, _entityOwner);

            var componentCount = reader.GetInt();

            for (var i = 0; i < componentCount; i++)
            {
                var componentId = Identification.Deserialize(reader);

                var componentPtr = MintyCore.ClientWorld.EntityManager.GetComponentPtr(_entity, componentId);
                ComponentManager.DeserializeComponent(componentPtr, componentId, reader);
            }
        }

        public void PopulateMessage(object? data = null)
        {
            if (!(data is Data passedData)) return;

            Receivers = new[] { passedData.PlayerId };
            _entity = passedData.ToSend;
            _entityOwner = passedData.EntityOwner;
        }

        public void Clear()
        {
            _entity = default;
            _entityOwner = default;
        }

        public class Data
        {
            public ushort PlayerId;
            public Entity ToSend;
            public ushort EntityOwner;
        }
    }
}