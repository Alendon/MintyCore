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
    ///     The world id the components live in
    /// </summary>
    public Identification WorldId;

    /// <summary>
    ///     The world game type (client or server)
    /// </summary>
    public GameType WorldGameType;

    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.ComponentUpdate;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    /// <inheritdoc />
    public ushort Sender { get; set; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        WorldId.Serialize(writer);

        writer.Put(Components.Count);

        if (Components.Count == 0 || !WorldHandler.TryGetWorld(WorldGameType, WorldId, out var world)) return;

        foreach (var (entity, components) in Components)
        {
            writer.EnterRegion(Engine.TestingModeActive ? entity.ToString() : null);
            entity.Serialize(writer);

            writer.Put(components.Count);
            foreach (var (componentId, componentData) in components)
            {
                writer.EnterRegion(Engine.TestingModeActive ? componentId.ToString() : null);
                componentId.Serialize(writer);
                ComponentManager.SerializeComponent(componentData, componentId, writer, world, entity);
                writer.ExitRegion();
            }

            writer.ExitRegion();
        }
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (!Identification.Deserialize(reader, out var worldId))
        {
            Logger.WriteLog("Failed to deserialize world id", LogImportance.ERROR, "Network");
            return false;
        }

        var worldType = IsServer ? GameType.SERVER : GameType.CLIENT;
        if (!WorldHandler.TryGetWorld(worldType, worldId, out var world))
        {
            Logger.WriteLog($"Failed to fetch {(IsServer ? "server" : "client")} world {worldId}", LogImportance.ERROR,
                "Network");
            return false;
        }

        if (!reader.TryGetInt(out var entityCount))
        {
            Logger.WriteLog("Failed to deserialize entity count", LogImportance.ERROR, "Network");
            return false;
        }


        for (var i = 0; i < entityCount; i++)
        {
            reader.EnterRegion();
            if (!Entity.Deserialize(reader, out var entity))
            {
                Logger.WriteLog("Failed to deserialize entity identification", LogImportance.ERROR, "Network");

                reader.ExitRegion();
                continue;
            }

            if (!world.EntityManager.EntityExists(entity))
            {
                Logger.WriteLog($"Entity {entity} to deserialize does not exists locally", LogImportance.INFO,
                    "Network");

                reader.ExitRegion();
                continue;
            }

            if (!reader.TryGetInt(out var componentCount))
            {
                Logger.WriteLog($"Failed to deserialize component count for Entity {entity}", LogImportance.ERROR,
                    "Network");

                reader.ExitRegion();
                continue;
            }

            for (var j = 0; j < componentCount; j++)
            {
                reader.EnterRegion();
                if (!Identification.Deserialize(reader, out var componentId))
                {
                    Logger.WriteLog("Failed to deserialize component id", LogImportance.ERROR, "Network");
                    reader.ExitRegion();
                    continue;
                }

                switch (IsServer)
                {
                    case true when !ComponentManager.IsPlayerControlled(componentId):
                    case false when ComponentManager.IsPlayerControlled(componentId):
                    case true when ComponentManager.IsPlayerControlled(componentId) &&
                        world.EntityManager.GetEntityOwner(entity) != Sender:
                        reader.ExitRegion();
                        continue;
                }

                var componentPtr = world.EntityManager.GetComponentPtr(entity, componentId);
                if (!ComponentManager.DeserializeComponent(componentPtr,
                        componentId, reader, world, entity))
                {
                    Logger.WriteLog($"Failed to deserialize component {componentId} from {entity}", LogImportance.ERROR,
                        "Network");
                }
                
                reader.ExitRegion();
            }

            reader.ExitRegion();
        }

        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
        Components.Clear();
    }
}