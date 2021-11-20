namespace MintyCore.Network
{
    internal enum DisconnectReasons : uint
    {
        UNKNOWN = 0,
        PLAYER_DISCONNECT,
        KICK,
        BAN,
        SERVER_FULL,
        REJECT,
        SERVER_CLOSING
    }
}