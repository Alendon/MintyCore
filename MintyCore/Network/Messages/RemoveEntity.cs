using LiteNetLib;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message which is sent to issue a entity remove command
/// </summary>
[RegisterMessage("remove_entity")]
public class RemoveEntity : Message
{
    /// <summary>
    ///     Entity to remove
    /// </summary>
    public Entity Entity;

    /// <summary>
    /// World Id to remove entity from
    /// </summary>
    public Identification WorldId;


    /// <inheritdoc />
    public override bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public override Identification MessageId => MessageIDs.RemoveEntity;

    /// <inheritdoc />
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;


    /// <summary/>
    public required IWorldHandler WorldHandler { private get; init; }


    /// <inheritdoc />
    public override void Serialize(DataWriter writer)
    {
        Entity.Serialize(writer);
        writer.Put(WorldId);
    }

    /// <inheritdoc />
    public override bool Deserialize(DataReader reader)
    {
        if (IsServer) return true;

        if (!Entity.Deserialize(reader, out var entity)) return false;
        if (reader.TryGetIdentification(out var worldId)) return false;
        if (!WorldHandler.TryGetWorld(GameType.Client, worldId, out var world)) return false;

        world.EntityManager.RemoveEntity(entity);

        return true;
    }

    /// <inheritdoc />
    public override void Clear()
    {
        Entity = default;
    }
}