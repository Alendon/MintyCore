using LiteNetLib;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message which get sends when a player joins the game
/// </summary>
[RegisterMessage("player_joined")]
internal class PlayerJoined : Message
{
    internal ushort GameId;
    internal ulong PlayerId;
    internal string PlayerName = string.Empty;


    public override bool ReceiveMultiThreaded => true;

    public override Identification MessageId => MessageIDs.PlayerJoined;
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;

    public required IPlayerHandler PlayerHandler { private get; init; }


    public override void Serialize(DataWriter writer)
    {
        writer.Put(GameId);
        writer.Put(PlayerName);
        writer.Put(PlayerId);
    }

    public override bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetUShort(out var gameId) ||
            !reader.TryGetString(out var playerName) ||
            !reader.TryGetULong(out var playerId))
            return false;

        GameId = gameId;
        PlayerName = playerName;
        PlayerId = playerId;

        PlayerHandler.AddPlayer(GameId, PlayerName, PlayerId, false);

        return true;
    }


    public override void Clear()
    {
        GameId = default;
        PlayerName = string.Empty;
        PlayerId = default;
    }
}