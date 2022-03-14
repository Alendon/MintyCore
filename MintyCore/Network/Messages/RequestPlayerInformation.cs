using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
/// Request message to send player information
/// </summary>
public partial class RequestPlayerInformation : IMessage
{
    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.RequestPlayerInformation;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    /// <inheritdoc />
    public ushort Sender { get; set; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (IsServer) return false;
        
        var availableMods = from mods in ModManager.GetAvailableMods()
            select (mods.ModId, mods.ModVersion);
        
        PlayerInformation playerInformation = new()
        {
            PlayerId = PlayerHandler.LocalPlayerId,
            PlayerName = PlayerHandler.LocalPlayerName,
            AvailableMods = availableMods
        };
        
        playerInformation.SendToServer();
        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
    }
}