using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    public partial class RemoveEntity : IMessage
    {
        public Entity Entity;
        public ushort[] Receivers { private set; get; }
        public bool IsServer { get; set; }
        public bool ReceiveMultiThreaded => false;

        public Identification MessageId => MessageIDs.RemoveEntity;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;
        public void Serialize(DataWriter writer)
        {
            Entity.Serialize(writer);
        }

        public void Deserialize(DataReader reader)
        {
            if(IsServer) return;
            
            Entity = Entity.Deserialize(reader);
            Engine.ClientWorld?.EntityManager.RemoveEntity(Entity);
        }

        public void Clear()
        {
            Entity = default;
        }
    }
}