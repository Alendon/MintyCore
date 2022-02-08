﻿using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

internal partial class PlayerJoined : IMessage
{
    internal ushort GameId;
    internal ulong PlayerId;
    internal string PlayerName = string.Empty;


    public bool IsServer { get; set; }
    public bool ReceiveMultiThreaded => true;

    public Identification MessageId => MessageIDs.PlayerJoined;
    public DeliveryMethod DeliveryMethod => DeliveryMethod.RELIABLE;

    public void Serialize(DataWriter writer)
    {
        writer.Put(GameId);
        writer.Put(PlayerName);
        writer.Put(PlayerId);
    }

    public void Deserialize(DataReader reader)
    {
        GameId = reader.GetUShort();
        PlayerName = reader.GetString();
        PlayerId = reader.GetULong();

        PlayerHandler.AddPlayer(GameId, PlayerName, PlayerId, false);
    }


    public void Clear()
    {
        GameId = default;
        PlayerName = string.Empty;
        PlayerId = default;
    }
}