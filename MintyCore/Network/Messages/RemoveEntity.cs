using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message which is sent to issue a entity remove command
/// </summary>
public partial class RemoveEntity : IMessage
{
    /// <summary>
    ///     Entity to remove
    /// </summary>
    public Entity Entity;

    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.RemoveEntity;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;
    
    /// <inheritdoc />
    public ushort Sender { get; set; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        Entity.Serialize(writer);
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (IsServer) return true;

        if (!Entity.Deserialize(reader, out var entity)) return false;

        Entity = entity;
        Engine.ClientWorld?.EntityManager.RemoveEntity(Entity);

        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
        Entity = default;
    }
}