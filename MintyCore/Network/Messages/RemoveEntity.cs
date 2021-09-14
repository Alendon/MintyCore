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
        public IEnumerable<Entity> Entities { get; private set; } = Array.Empty<Entity>();
        public ushort[] Receivers { private set; get; }
        public bool AutoSend => false;
        public bool IsServer { get; set; }
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.RemoveEntity;
        public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;
        public void Serialize(DataWriter writer)
        {
            writer.Put(Entities.Count());
            foreach (var entity in Entities)
            {
                entity.Serialize(writer);
            }
        }

        public void Deserialize(DataReader reader)
        {
            if(IsServer) return;
            
            var entityCount = reader.GetInt();
            Entities = new Entity[entityCount];

            for (int i = 0; i < entityCount; i++)
            {
                Entities[i] = Entity.Deserialize(reader);
            }

            foreach (var entity in Entities)
            {
                MintyCore.ClientWorld?.EntityManager.RemoveEntity(entity);   
            }
        }

        public void PopulateMessage(object? data = null)
        {
            if (!(data is Data entityData)) return;
            Entities = entityData.Entities;
            Receivers = MintyCore._playerIDs.Keys.ToArray();
        }

        public void Clear()
        {
            Entities = Array.Empty<Entity>();
        }

        public class Data
        {
            public IEnumerable<Entity> Entities;

            public Data(IEnumerable<Entity> entities)
            {
                Entities = entities;
            }
        }
    }
}