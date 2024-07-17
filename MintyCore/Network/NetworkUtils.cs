using System;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyCore.Utils.Events;

namespace MintyCore.Network;

public class NetworkUtils
{
    public static ReadOnlySpan<byte> OriginSpan => "MINT"u8;
    public static MagicHeader ConnectedMessageHeader => MagicHeader.Create(OriginSpan, "CM"u8, 1);
    public static MagicHeader ConnectionRequestHeader => MagicHeader.Create(OriginSpan, "CR"u8, 1);
}

[RegisterEvent("client_connected")]
public struct ClientConnectedEvent : IEvent
{
    public static Identification Identification => EventIDs.ClientConnected;
    public static bool ModificationAllowed => false;
    
    public ushort TemporaryId { get; init; }
}