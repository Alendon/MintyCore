using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
///   Message to tell the server that the player is ready
/// </summary>
[RegisterMessage("player_ready")]
public partial class PlayerReady : IMessage
{
    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.PlayerReady;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

    /// <inheritdoc />
    public ushort Sender { get; set; }

    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; init; }
    /// <summary/>
    public required INetworkHandler NetworkHandler { get; init; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    { 
        PlayerHandler.TriggerPlayerReady(PlayerHandler.GetPlayer(Sender));

        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
    }
}