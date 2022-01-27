using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

public partial class SendEntityData : IMessage
{
    internal Entity Entity;
    internal ushort EntityOwner;

    public ushort[] Receivers { private set; get; }
    public bool IsServer { get; set; }
    public bool ReceiveMultiThreaded => false;

    public Identification MessageId => MessageIDs.SendEntityData;
    public MessageDirection MessageDirection => MessageDirection.SERVER_TO_CLIENT;
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    public void Serialize(DataWriter writer)
    {
        if (Engine.ServerWorld is null) return;

        Entity.Serialize(writer);
        writer.Put(EntityOwner);

        var componentIDs = ArchetypeManager.GetArchetype(Entity.ArchetypeId).ArchetypeComponents;

        writer.Put(componentIDs.Count);
        foreach (var componentId in componentIDs)
        {
            var componentPtr = Engine.ServerWorld.EntityManager.GetComponentPtr(Entity, componentId);

            componentId.Serialize(writer);
            ComponentManager.SerializeComponent(componentPtr, componentId, writer, Engine.ServerWorld, Entity);
        }

        if (!EntityManager.EntitySetups.TryGetValue(Entity.ArchetypeId, out var setup))
        {
            writer.Put((byte)0);
            return;
        }

        writer.Put((byte)1);
        setup.GatherEntityData(Engine.ServerWorld, Entity);
        setup.Serialize(writer);
    }

    public void Deserialize(DataReader reader)
    {
        if (Engine.ClientWorld is null) return;

        Entity = Entity.Deserialize(reader);
        EntityOwner = reader.GetUShort();

        Engine.ClientWorld.EntityManager.AddEntity(Entity, EntityOwner);

        var componentCount = reader.GetInt();

        for (var i = 0; i < componentCount; i++)
        {
            var componentId = Identification.Deserialize(reader);

            var componentPtr = Engine.ClientWorld.EntityManager.GetComponentPtr(Entity, componentId);
            ComponentManager.DeserializeComponent(componentPtr, componentId, reader, Engine.ClientWorld, Entity);
        }

        var hasSetup = reader.GetByte();
        if (hasSetup == 0) return;

        var setup = EntityManager.EntitySetups[Entity.ArchetypeId];
        setup.Deserialize(reader);
        setup.SetupEntity(Engine.ClientWorld, Entity);
    }

    public void Clear()
    {
        Entity = default;
        EntityOwner = default;
    }
}