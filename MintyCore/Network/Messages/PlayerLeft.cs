using JetBrains.Annotations;
using LiteNetLib;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message which is send if a player left the server
/// </summary>
[RegisterMessage("player_left")]
public class PlayerLeft(IEngineConfiguration engineConfiguration) : Message
{
    internal ushort PlayerGameId;


    /// <inheritdoc />
    public override bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public override Identification MessageId => MessageIDs.PlayerLeft;

    /// <inheritdoc />
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;


    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; [UsedImplicitly] init; }


    /// <inheritdoc />
    public override void Serialize(DataWriter writer)
    {
        writer.Put(PlayerGameId);
    }

    /// <inheritdoc />
    public override bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetUShort(out var playerGameId)) return false;
        PlayerGameId = playerGameId;
        if (!PlayerHandler.TryGetPlayer(playerGameId, out var player))
        {
            Log.Warning("Player with id {PlayerGameId} not found", playerGameId);
            return true;
        }

        //Check if its not a local game, as there the method was already called before
        if (engineConfiguration.GameType == GameType.Client) PlayerHandler.DisconnectPlayer(player, IsServer);

        return true;
    }


    /// <inheritdoc />
    public override void Clear()
    {
        PlayerGameId = 0;
    }
}