using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    internal partial class AddEntity : IMessage
    {
        internal Entity Entity;
        internal ushort Owner;
        internal IEntitySetup? EntitySetup;

        public ushort[] Receivers { get; private set; }
        public bool IsServer { get; set; }
        public bool ReceiveMultiThreaded => false;

        public Identification MessageId => MessageIDs.AddEntity;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;
        public void Serialize(DataWriter writer)
        {
            Entity.Serialize(writer);
            writer.Put(Owner);
            if (EntitySetup is null)
            {
                writer.Put((byte)0);
                return;
            }
            
            writer.Put((byte)1);
            EntitySetup.Serialize(writer);
        }

        public void Deserialize(DataReader reader)
        {
            Entity = Entity.Deserialize(reader);
            Owner = reader.GetUShort();
            var hasSetup = reader.GetByte();
            if (hasSetup == 1)
            {
                EntitySetup = EntityManager.EntitySetups[Entity.ArchetypeId];
                EntitySetup.Deserialize(reader);
            }
            
            if(IsServer) return;
            Engine.ClientWorld?.EntityManager.AddEntity(Entity, Owner, EntitySetup);
        }

        public void Clear()
        {
            Entity = default;
            Owner = default;
        }
    }
}