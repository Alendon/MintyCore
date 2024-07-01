using LiteNetLib;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
/// Send the information that a client is connected including its game id
/// </summary>
[RegisterMessage("player_connected")]
public class PlayerConnected : Message
{
    /// <inheritdoc />
    public override bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public override Identification MessageId => MessageIDs.PlayerConnected;

    /// <inheritdoc />
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;

    /// <summary/>
    public required IWorldHandler WorldHandler { private get; init; }

    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; init; }

    /// <summary>
    /// 
    /// </summary>
    public ushort PlayerGameId { get; set; }

    /// <inheritdoc />
    public override void Serialize(DataWriter writer)
    {
        writer.Put(PlayerGameId);
    }

    /// <inheritdoc />
    public override bool Deserialize(DataReader reader)
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
    public override void Clear()
    {
        PlayerGameId = 0;
    }
}