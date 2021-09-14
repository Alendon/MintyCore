using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    public class RemoveEntity : IMessage
    {
        public Entity Entity { get; private set; }
        public ushort[] Receivers { private set; get; }
        public bool AutoSend => false;
        public bool IsServer { get; set; }
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.RemoveEntity;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;
        public void Serialize(DataWriter writer)
        {
            Entity.Serialize(writer);
        }

        public void Deserialize(DataReader reader)
        {
            if(IsServer) return;
            
            Entity = Entity.Deserialize(reader);
            MintyCore.ClientWorld?.EntityManager.RemoveEntity(Entity);
        }

        public void PopulateMessage(object? data = null)
        {
            if (!(data is Data entityData)) return;
            Entity = entityData.Entity;
            Receivers = MintyCore._playerIDs.Keys.ToArray();
        }

        public void Clear()
        {
            Entity = default;
        }

        public class Data
        {
            public Entity Entity;

            public Data(Entity entity)
            {
                Entity = entity;
            }
        }
    }
}