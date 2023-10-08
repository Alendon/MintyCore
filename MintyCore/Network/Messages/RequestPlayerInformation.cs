using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
/// Request message to send player information
/// </summary>
[RegisterMessage("request_player_info")]
public partial class RequestPlayerInformation : IMessage
{
    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.RequestPlayerInfo;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

    /// <inheritdoc />
    public ushort Sender { get; set; }

    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; init; }

    /// <summary/>
    public required INetworkHandler NetworkHandler { private get; init; }

    /// <summary/>
    public required IModManager ModManager { private get; init; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
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
    public void Clear()
    {
    }
}