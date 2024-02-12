using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;

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

    /// <summary/>
    public required IWorldHandler WorldHandler { private get; init; }
    /// <summary/>
    public required IArchetypeManager ArchetypeManager { private get; init; }
    /// <summary/>
    public required IComponentManager ComponentManager { private get; init; }
    /// <summary/>
    public required INetworkHandler NetworkHandler { get; init; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        if (!WorldHandler.TryGetWorld(GameType.Server, WorldId, out var world))
        {
            Log.Error("Can not serialize entity data; server world {WorldId} does not exists", WorldId);
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
            Log.Error("Failed to deserialize {Header} header", nameof(SendEntityData));
            return false;
        }

        WorldId = worldId;
        if (!WorldHandler.TryGetWorld(GameType.Client, WorldId, out var world))
        {
            Log.Error("No client world {WorldId} available", WorldId);
            return false;
        }

        Entity = entity;
        EntityOwner = entityOwner;

        world.EntityManager.AddEntity(Entity, EntityOwner);

        if (!reader.TryGetInt(out var componentCount))
        {
            Log.Error("Failed to deserialize the component count for {Entity}", Entity);
            return false;
        }

        for (var i = 0; i < componentCount; i++)
        {
            reader.EnterRegion();
            if (!Identification.Deserialize(reader, out var componentId))
            {
                Log.Error("Failed to deserialize component id");
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

            Log.Error("Failed to deserialize component {ComponentId} from {Entity}", componentId, entity);
            reader.ExitRegion();
            return false;
        }

        if (!reader.TryGetBool(out var hasSetup))
        {
            Log.Error("Failed to get 'hasSetup' indication");
            return false;
        }

        if (!hasSetup || !ArchetypeManager.TryGetEntitySetup(Entity.ArchetypeId, out var setup)) return true;

        if (!setup.Deserialize(reader))
        {
            Log.Error("Entity setup deserialization failed");
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