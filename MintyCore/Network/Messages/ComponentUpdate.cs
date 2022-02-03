using System;
using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

//TODO we probably need to completely rewrite this as this just sends the updates for all entities to all players
internal partial class ComponentUpdate : IMessage
{
    internal Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>> Components = new();
    internal World? World;

    public bool IsServer { get; set; }
    public bool ReceiveMultiThreaded => false;

    public Identification MessageId => MessageIDs.ComponentUpdate;
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    public void Serialize(DataWriter writer)
    {
        writer.Put(Components.Count);

        if (Components.Count == 0 && World is null) return;

        foreach (var (entity, components) in Components)
        {
            entity.ArchetypeId.Serialize(writer);
            writer.Put(entity.Id);

            writer.Put(components.Count);
            foreach (var (componentId, componentData) in components)
            {
                componentId.Serialize(writer);
                ComponentManager.SerializeComponent(componentData, componentId, writer, World, entity);
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

            if (!world.EntityManager.EntityExists(entity)) continue;

            var componentCount = reader.GetInt();

            for (var j = 0; j < componentCount; j++)
            {
                var componentId = Identification.Deserialize(reader);
                var componentPtr = world.EntityManager.GetComponentPtr(entity, componentId);
                ComponentManager.DeserializeComponent(componentPtr, componentId, reader, world, entity);
            }
        }
    }

    public void Clear()
    {
        Components.Clear();
    }
}