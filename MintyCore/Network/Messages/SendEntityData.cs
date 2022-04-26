using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using MintyCore.Registries;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message to send entity data to clients
/// </summary>
[RegisterMessage("send_entity_data")]
public partial class SendEntityData : IMessage
{
    internal Entity Entity;
    internal ushort EntityOwner;
    internal Identification WorldId;


    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.SendEntityData;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

    /// <inheritdoc />
    public ushort Sender { get; set; }


    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        if (!WorldHandler.TryGetWorld(GameType.Server, WorldId, out var world))
        {
            Logger.WriteLog($"Cant serialize entity data; server world {WorldId} does not exists", LogImportance.Error,
                "Network");
            return;
        }

        WorldId.Serialize(writer);
        Entity.Serialize(writer);
        writer.Put(EntityOwner);

        var componentIDs = ArchetypeManager.GetArchetype(Entity.ArchetypeId).ArchetypeComponents;

        writer.Put(componentIDs.Count);
        foreach (var componentId in componentIDs)
        {
            writer.EnterRegion($"{Entity}:{componentId}");

            componentId.Serialize(writer);
            if (ComponentManager.IsPlayerControlled(componentId))
            {
                writer.ExitRegion();
                continue;
            }

            var componentPtr = world.EntityManager.GetComponentPtr(Entity, componentId);
            ComponentManager.SerializeComponent(componentPtr, componentId, writer, world, Entity);
            writer.ExitRegion();
        }

        if (!ArchetypeManager.TryGetEntitySetup(Entity.ArchetypeId, out var setup))
        {
            writer.Put(false);
            return;
        }

        writer.Put(true);
        setup.GatherEntityData(world, Entity);
        setup.Serialize(writer);
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (IsServer) return false;
        
        if (!Identification.Deserialize(reader, out var worldId) || 
            !Entity.Deserialize(reader, out var entity) ||
            !reader.TryGetUShort(out var entityOwner))
        {
            Logger.WriteLog($"Failed to deserialize {nameof(SendEntityData)} header", LogImportance.Error, "Network");
            return false;
        }

        WorldId = worldId;
        if (!WorldHandler.TryGetWorld(GameType.Client, WorldId, out var world))
        {
            Logger.WriteLog($"No client world {WorldId} available", LogImportance.Error, "Network");
            return false;
        }

        Entity = entity;
        EntityOwner = entityOwner;

        world.EntityManager.AddEntity(Entity, EntityOwner);

        if (!reader.TryGetInt(out var componentCount))
        {
            Logger.WriteLog($"Failed to deserialize the component count for {Entity}", LogImportance.Error, "Network");
            return false;
        }

        for (var i = 0; i < componentCount; i++)
        {
            reader.EnterRegion();
            if (!Identification.Deserialize(reader, out var componentId))
            {
                Logger.WriteLog("Failed to deserialize component id", LogImportance.Error, "Network");
                reader.ExitRegion();
                return false;
            }

            if (ComponentManager.IsPlayerControlled(componentId))
            {
                reader.ExitRegion();
                continue;
            }

            var componentPtr = world.EntityManager.GetComponentPtr(Entity, componentId);

            if (ComponentManager.DeserializeComponent(componentPtr, componentId, reader, world, Entity))
            {
                reader.ExitRegion();
                continue;
            }


            Logger.WriteLog($"Failed to deserialize component {componentId} from {entity}", LogImportance.Error,
                "Network");
            reader.ExitRegion();
            return false;
        }

        if (!reader.TryGetBool(out var hasSetup))
        {
            Logger.WriteLog("Failed to get 'hasSetup' indication", LogImportance.Error, "Network");
            return false;
        }

        if (!hasSetup || !ArchetypeManager.TryGetEntitySetup(Entity.ArchetypeId, out var setup)) return true;

        if (!setup.Deserialize(reader))
        {
            Logger.WriteLog("Entity setup deserialization failed", LogImportance.Error, "Network");
            return false;
        }

        setup.SetupEntity(world, Entity);

        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
        Entity = default;
        EntityOwner = default;
    }
}