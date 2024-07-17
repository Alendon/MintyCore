using System;
using LiteNetLib;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Network.Messages;

[RegisterMessage("add_entity")]
internal class AddEntity : Message
{
    internal Entity Entity;
    internal IEntitySetup? EntitySetup;
    internal Player? Owner;
    internal Identification WorldId;

    public override bool ReceiveMultiThreaded => false;

    public override Identification MessageId => MessageIDs.AddEntity;
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;

    public required IArchetypeManager ArchetypeManager { private get; init; }
    public required IWorldHandler WorldHandler { private get; init; }
    public required IPlayerHandler PlayerHandler { private get; init; }


    public override void Serialize(DataWriter writer)
    {
        if (Owner is null)
        {
            throw new InvalidOperationException("Owner is not set");
        }
        
        writer.Put(WorldId);
        Entity.Serialize(writer);
        writer.Put(Owner.GameId);


        //Check if a entity setup is set and is available for the archetype id
        if (EntitySetup is null || !ArchetypeManager.TryGetEntitySetup(Entity.ArchetypeId, out _))
        {
            writer.Put(false);
            return;
        }

        writer.Put(true);
        EntitySetup.Serialize(writer);
    }

    public override bool Deserialize(DataReader reader)
    {
        if (IsServer ||
            !reader.TryGetIdentification(out var worldId) ||
            !WorldHandler.TryGetWorld(GameType.Client, worldId, out var world) ||
            !Entity.Deserialize(reader, out var entity) ||
            !reader.TryGetUShort(out var ownerId) ||
            !reader.TryGetBool(out var hasSetup))
            return false;

        Entity = entity;

        if (!PlayerHandler.TryGetPlayer(ownerId, out var owner))
        {
            Log.Error("Failed to get player with id {PlayerId}", ownerId);
            return false;
        }
        
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

    public override void Clear()
    {
        Entity = default;
        Owner = default;
        EntitySetup = null;
    }
}