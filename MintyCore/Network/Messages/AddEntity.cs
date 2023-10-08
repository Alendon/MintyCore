using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

[RegisterMessage("add_entity")]
internal partial class AddEntity : IMessage
{
    internal Entity Entity;
    internal IEntitySetup? EntitySetup;
    internal ushort Owner;
    internal Identification WorldId;

    public bool IsServer { get; set; }
    public bool ReceiveMultiThreaded => false;

    public Identification MessageId => MessageIDs.AddEntity;
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;
    
    public required IArchetypeManager ArchetypeManager { private get; init; }
    public required IWorldHandler WorldHandler { private get; init; }

    /// <inheritdoc />
    public ushort Sender { get; set; }

    public void Serialize(DataWriter writer)
    {
        WorldId.Serialize(writer);
        Entity.Serialize(writer);
        writer.Put(Owner);


        //Check if a entity setup is set and is available for the archetype id
        if (EntitySetup is null || !ArchetypeManager.TryGetEntitySetup(Entity.ArchetypeId, out _))
        {
            writer.Put(false);
            return;
        }

        writer.Put(true);
        EntitySetup.Serialize(writer);
    }

    public bool Deserialize(DataReader reader)
    {
        if (IsServer ||
            !Identification.Deserialize(reader, out var worldId) ||
            !WorldHandler.TryGetWorld(GameType.Client, worldId, out var world) ||
            !Entity.Deserialize(reader, out var entity) ||
            !reader.TryGetUShort(out var owner) ||
            !reader.TryGetBool(out var hasSetup))
            return false;

        Entity = entity;
        Owner = owner;

        //If a setup is stored in the message deserialize it
        if (hasSetup && ArchetypeManager.TryGetEntitySetup(Entity.ArchetypeId, out EntitySetup) &&
            !EntitySetup.Deserialize(reader))
            //return false if the setup failed to deserialize
            return false;

        if (IsServer) return true;

        world.EntityManager.AddEntity(Entity, Owner, EntitySetup);
        return true;
    }

    public void Clear()
    {
        Entity = default;
        Owner = default;
        EntitySetup = null;
    }
}