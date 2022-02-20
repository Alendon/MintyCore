using System;
using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

//TODO we probably need to completely rewrite this as this just sends the updates for all entities to all players
/// <summary>
///     Message to update components of entities
/// </summary>
public partial class ComponentUpdate : IMessage
{
    /// <summary>
    ///     Collection of components to update
    /// </summary>
    public Dictionary<Entity, List<(Identification componentId, IntPtr componentData)>> Components = new();

    /// <summary>
    ///     The world the components live in
    /// </summary>
    public World? World;

    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.ComponentUpdate;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        writer.Put(Components.Count);

        if (Components.Count == 0 || World is null) return;

        foreach (var (entity, components) in Components)
        {
            entity.Serialize(writer);

            writer.Put(components.Count);
            foreach (var (componentId, componentData) in components)
            {
                componentId.Serialize(writer);
                ComponentManager.SerializeComponent(componentData, componentId, writer, World, entity);
            }
        }
    }

    /// <inheritdoc />
    public void Deserialize(DataReader reader)
    {
        var world = IsServer ? Engine.ServerWorld : Engine.ClientWorld;
        if (world is null) return;

        var entityCount = reader.GetInt();
        for (var i = 0; i < entityCount; i++)
        {
            var entity = Entity.Deserialize(reader);
            
            //TODO Fix this. Here is a potential breaking bug, if the entity does not exists, the loop get skipped, BUT any remaining data of the entity remains in the deserializer which leads to undefined behaviour
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

    /// <inheritdoc />
    public void Clear()
    {
        Components.Clear();
    }
}