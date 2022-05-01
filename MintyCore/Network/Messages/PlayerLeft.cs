using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message which is send if a player left the server
/// </summary>
[RegisterMessage("player_left")]
public partial class PlayerLeft : IMessage
{
    internal ushort PlayerGameId;

    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.PlayerLeft;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

    /// <inheritdoc />
    public ushort Sender { get; set; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        writer.Put(PlayerGameId);
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetUShort(out var playerGameId)) return false;
        PlayerGameId = playerGameId;

        //Check if its not a local game, as there the method was already called before
        if (Engine.GameType == GameType.Client) PlayerHandler.DisconnectPlayer(PlayerGameId, IsServer);

        return true;
    }


    /// <inheritdoc />
    public void Clear()
    {
        PlayerGameId = 0;
    }
}