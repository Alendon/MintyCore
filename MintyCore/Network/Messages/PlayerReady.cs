using LiteNetLib;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyCore.Utils.Events;

namespace MintyCore.Network.Messages;

/// <summary>
///   Message to tell the server that the player is ready
/// </summary>
[RegisterMessage("player_ready")]
public class PlayerReady : Message
{
    /// <inheritdoc />
    public override bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public override Identification MessageId => MessageIDs.PlayerReady;

    /// <inheritdoc />
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;


    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; init; }

    public required IEventBus EventBus { get; init; }

    /// <inheritdoc />
    public override void Serialize(DataWriter writer)
    {
    }

    /// <inheritdoc />
    public override bool Deserialize(DataReader reader)
    {
        EventBus.InvokeEvent(new PlayerEvent()
        {
            Player = PlayerHandler.GetPlayer(Sender),
            Type = PlayerEvent.EventType.Ready,
            ServerSide = IsServer
        });

        return true;
    }

    /// <inheritdoc />
    public override void Clear()
    {
    }
}