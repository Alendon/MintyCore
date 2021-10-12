using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages
{
    //TODO we probably need to completely rewrite this as this just sends the updates for all entities to all players
    public class ComponentUpdate : IMessage
    {
        private Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>> _components = new();
        private World? _world;

        public bool IsServer { get; set; }
        public ushort[] Receivers { get; private set; }
        public bool AutoSend => false;
        public int AutoSendInterval { get; }
        public Identification MessageId => MessageIDs.ComponentUpdate;
        public MessageDirection MessageDirection => MessageDirection.BOTH;
        public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;
        public void Serialize(DataWriter writer)
        {
            writer.Put(_components.Count);
            
            if(_components.Count == 0 && _world is null) return;
            
            foreach (var (entity, components) in _components)
            {
                entity.ArchetypeId.Serialize(writer);
                writer.Put(entity.Id);
                
                writer.Put(components.Count);
                foreach (var (componentId, componentData) in components)
                {
                    componentId.Serialize(writer);
                    ComponentManager.SerializeComponent(componentData, componentId, writer, _world, entity);
                }
            }
        }

        public void Deserialize(DataReader reader)
        {
            var world = IsServer ? Engine.ServerWorld : Engine.ClientWorld;
            if (world is null) return;
            
            var entityCount = reader.GetInt();
            for (var i = 0; i < entityCount; i++)
            {
                var archetypeId = Identification.Deserialize(reader);
                var entity = new Entity(archetypeId, reader.GetUInt());
                
                if(!world.EntityManager.EntityExists(entity)) continue;

                var componentCount = reader.GetInt();

                for (var j = 0; j < componentCount; j++)
                {
                    var componentId = Identification.Deserialize(reader);
                    var componentPtr = world.EntityManager.GetComponentPtr(entity, componentId);
                    ComponentManager.DeserializeComponent(componentPtr, componentId, reader, world, entity);
                }
            }
        }

        public void PopulateMessage(object? data = null)
        {
            if (data is not ComponentData componentData) return;

            _components = componentData.Components;
            Receivers = Engine.PlayerIDs.Keys.ToArray();
            _world = componentData.World;
        }

        public void Clear()
        {
            _components.Clear();
        }

        internal class ComponentData
        {
            public readonly Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>> Components = new();
            public World? World;
        }
    }
}