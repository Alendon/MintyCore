using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
/// Send the information that a client is connected including its game id
/// </summary>
[RegisterMessage("player_connected")]
public partial class PlayerConnected : IMessage
{
    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.PlayerConnected;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

    /// <inheritdoc />
    public ushort Sender { get; set; }
    
    public required IWorldHandler WorldHandler { private get; init; }
    public required IPlayerHandler PlayerHandler { private get; init; }
    public required INetworkHandler NetworkHandler { private get; init; }

    /// <summary>
    /// 
    /// </summary>
    public ushort PlayerGameId { get; set; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        writer.Put(PlayerGameId);
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (IsServer) return false;

        if (!reader.TryGetUShort(out var playerGameId)) return false;
        PlayerGameId = playerGameId;

        //TODO Not optimal, move this to a seperated method in the Engine class
        PlayerHandler.LocalPlayerGameId = PlayerGameId;

        WorldHandler.CreateWorlds(GameType.Client);
        
        NetworkHandler.CreateMessage<PlayerReady>().SendToServer();
        
        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
        PlayerGameId = 0;
    }
}