using System.Linq;
using LiteNetLib;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
/// Request message to send player information
/// </summary>
[RegisterMessage("request_player_info")]
public class RequestPlayerInformation : Message
{
    /// <inheritdoc />
    public override bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public override Identification MessageId => MessageIDs.RequestPlayerInfo;

    /// <inheritdoc />
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;


    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; init; }


    /// <summary/>
    public required IModManager ModManager { private get; init; }

    /// <inheritdoc />
    public override void Serialize(DataWriter writer)
    {
    }

    /// <inheritdoc />
    public override bool Deserialize(DataReader reader)
    {
        if (IsServer) return false;

        var availableMods = from mods in ModManager.GetAvailableMods(false)
            select (mods.Identifier, mods.Version);

        var playerInformation = NetworkHandler.CreateMessage<PlayerInformation>();
        playerInformation.PlayerId = PlayerHandler.LocalPlayerId;
        playerInformation.PlayerName = PlayerHandler.LocalPlayerName;
        playerInformation.AvailableMods = availableMods;

        playerInformation.SendToServer();
        return true;
    }

    /// <inheritdoc />
    public override void Clear()
    {
    }
}