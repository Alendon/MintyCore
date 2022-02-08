using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
/// Message which is send if a player left the server
/// </summary>
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
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        writer.Put(PlayerGameId);
    }

    /// <inheritdoc />
    public void Deserialize(DataReader reader)
    {
        PlayerGameId = reader.GetUShort();

        //Check if its not a local game, as there the method was already called before
        if (Engine.GameType == GameType.CLIENT) PlayerHandler.DisconnectPlayer(PlayerGameId, IsServer);
    }


    /// <inheritdoc />
    public void Clear()
    {
        PlayerGameId = 0;
    }
}