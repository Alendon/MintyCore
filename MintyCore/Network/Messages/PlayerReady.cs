using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

[RegisterMessage("player_ready")]
public partial class PlayerReady : IMessage
{
    public bool IsServer { get; set; }
    public bool ReceiveMultiThreaded => false;
    public Identification MessageId => MessageIDs.PlayerReady;
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;
    public ushort Sender { get; set; }

    public required IPlayerHandler PlayerHandler { private get; init; }
    public required INetworkHandler NetworkHandler { get; init; }

    public void Serialize(DataWriter writer)
    {
    }

    public bool Deserialize(DataReader reader)
    { 
        PlayerHandler.TriggerPlayerReady(PlayerHandler.GetPlayer(Sender));

        return true;
    }

    public void Clear()
    {
    }
}