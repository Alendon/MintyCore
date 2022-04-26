namespace MintyCore.Network;

internal enum DisconnectReasons : uint
{
    Unknown = 0,
    PlayerDisconnect,
    Kick,
    Ban,
    ServerFull,
    Reject,
    ServerClosing,
    TimeOut
}