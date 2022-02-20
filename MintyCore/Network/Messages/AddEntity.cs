using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

internal partial class AddEntity : IMessage
{
    internal Entity Entity;
    internal IEntitySetup? EntitySetup;
    internal ushort Owner;

    public bool IsServer { get; set; }
    public bool ReceiveMultiThreaded => false;

    public Identification MessageId => MessageIDs.AddEntity;
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    public void Serialize(DataWriter writer)
    {
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

    public void Deserialize(DataReader reader)
    {
        Entity = Entity.Deserialize(reader);
        Owner = reader.GetUShort();
        var hasSetup = reader.GetBool();
        if (hasSetup && ArchetypeManager.TryGetEntitySetup(Entity.ArchetypeId, out EntitySetup))
        {
            //If a setup is stored in the message deserialize it
            EntitySetup.Deserialize(reader);
        }

        if (IsServer) return;
        //Add a Entity directly to the entity manager of the client world
        Engine.ClientWorld?.EntityManager.AddEntity(Entity, Owner, EntitySetup);
    }

    public void Clear()
    {
        Entity = default;
        Owner = default;
    }
}