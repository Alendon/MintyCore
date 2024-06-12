using LiteNetLib;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message which get sends when a player joins the game
/// </summary>
[RegisterMessage("player_joined")]
internal partial class PlayerJoined : IMessage
{
    internal ushort GameId;
    internal ulong PlayerId;
    internal string PlayerName = string.Empty;


    public bool IsServer { get; set; }
    public bool ReceiveMultiThreaded => true;

    public Identification MessageId => MessageIDs.PlayerJoined;
    public DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;
    
    public required IPlayerHandler PlayerHandler { private get; init; }
    public required INetworkHandler NetworkHandler { get; init; }

    /// <inheritdoc />
    public ushort Sender { get; set; }

    public void Serialize(DataWriter writer)
    {
        writer.Put(GameId);
        writer.Put(PlayerName);
        writer.Put(PlayerId);
    }

    public bool Deserialize(DataReader reader)
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


    public void Clear()
    {
        GameId = default;
        PlayerName = string.Empty;
        PlayerId = default;
    }
}