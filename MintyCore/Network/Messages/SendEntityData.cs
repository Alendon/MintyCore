using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message to send entity data to clients
/// </summary>
public partial class SendEntityData : IMessage
{
    internal Entity Entity;
    internal ushort EntityOwner;

    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.SendEntityData;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        if (Engine.ServerWorld is null) return;

        Entity.Serialize(writer);
        writer.Put(EntityOwner);

        var componentIDs = ArchetypeManager.GetArchetype(Entity.ArchetypeId).ArchetypeComponents;

        writer.Put(componentIDs.Count);
        foreach (var componentId in componentIDs)
        {
            writer.EnterRegion($"{Entity}:{componentId}");
            var componentPtr = Engine.ServerWorld.EntityManager.GetComponentPtr(Entity, componentId);

            componentId.Serialize(writer);
            ComponentManager.SerializeComponent(componentPtr, componentId, writer, Engine.ServerWorld, Entity);
            writer.ExitRegion();
        }

        if (!ArchetypeManager.TryGetEntitySetup(Entity.ArchetypeId, out var setup))
        {
            writer.Put(false);
            return;
        }

        writer.Put(true);
        setup.GatherEntityData(Engine.ServerWorld, Entity);
        setup.Serialize(writer);
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (Engine.ClientWorld is null) return false;

        if (!Entity.Deserialize(reader, out var entity) || !reader.TryGetUShort(out var entityOwner))
        {
            Logger.WriteLog($"Failed to deserialize {nameof(SendEntityData)} header", LogImportance.ERROR, "Network");
            return false;
        }

        Entity = entity;
        EntityOwner = entityOwner;

        Engine.ClientWorld.EntityManager.AddEntity(Entity, EntityOwner);

        if (!reader.TryGetInt(out var componentCount))
        {
            Logger.WriteLog($"Failed to deserialize the component count for {Entity}", LogImportance.ERROR, "Network");
            return false;
        }

        for (var i = 0; i < componentCount; i++)
        {
            reader.EnterRegion();
            if (!Identification.Deserialize(reader, out var componentId))
            {
                Logger.WriteLog("Failed to deserialize component id", LogImportance.ERROR, "Network");
                reader.ExitRegion();
                return false;
            }

            var componentPtr = Engine.ClientWorld.EntityManager.GetComponentPtr(Entity, componentId);

            if (ComponentManager.DeserializeComponent(componentPtr, componentId, reader, Engine.ClientWorld, Entity))
            {
                reader.ExitRegion();
                continue;
            }


            Logger.WriteLog($"Failed to deserialize component {componentId} from {entity}", LogImportance.ERROR,
                "Network");
            reader.ExitRegion();
            return false;
        }

        if (!reader.TryGetBool(out var hasSetup))
        {
            Logger.WriteLog("Failed to get 'hasSetup' indication", LogImportance.ERROR, "Network");
            return false;
        }

        if (!hasSetup || !ArchetypeManager.TryGetEntitySetup(Entity.ArchetypeId, out var setup)) return true;

        if (!setup.Deserialize(reader))
        {
            Logger.WriteLog("Entity setup deserialization failed", LogImportance.ERROR, "Network");
            return false;
        }

        setup.SetupEntity(Engine.ClientWorld, Entity);

        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
        Entity = default;
        EntityOwner = default;
    }
}