using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    //TODO we probably need to completly rewrite this as this just sends the updates for all entities to all players
    public class ComponentUpdate : IMessage
    {
        private Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>> _components = new();
        
        public bool IsServer { get; set; }
        public ushort[] Receivers { get; private set; }
        public bool AutoSend => false;
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.ComponentUpdate;
        public MessageDirection MessageDirection => MessageDirection.BOTH;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;
        public void Serialize(DataWriter writer)
        {
            writer.Put(_components.Count);
            foreach (var (entity, components) in _components)
            {
                entity.ArchetypeId.Serialize(writer);
                writer.Put(entity.Id);
                
                writer.Put(components.Count);
                foreach (var (componentId, componentData) in components)
                {
                    componentId.Serialize(writer);
                    ComponentManager.SerializeComponent(componentData, componentId, writer);
                }
            }
        }

        public void Deserialize(DataReader reader)
        {
            var world = IsServer ? MintyCore.ServerWorld : MintyCore.ClientWorld;
            if (world is null) return;
            
            int entityCount = reader.GetInt();
            for (int i = 0; i < entityCount; i++)
            {
                Identification archetypeId = Identification.Deserialize(reader);
                Entity entity = new Entity(archetypeId, reader.GetUInt());
                
                if(!world.EntityManager.EntityExists(entity)) continue;

                int componentCount = reader.GetInt();

                for (int j = 0; j < componentCount; j++)
                {
                    Identification componentId = Identification.Deserialize(reader);
                    var componentPtr = world.EntityManager.GetComponentPtr(entity, componentId);
                    ComponentManager.DeserializeComponent(componentPtr, componentId, reader);
                }
            }
        }

        public void PopulateMessage(object? data = null)
        {
            if (data is not ComponentData componentData) return;

            _components = componentData.components;
            Receivers = MintyCore._playerIDs.Keys.ToArray();
        }

        public void Clear()
        {
            _components.Clear();
        }

        internal class ComponentData
        {
            public Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>> components = new();
        }
    }
}