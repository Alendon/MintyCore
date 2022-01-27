using System;

namespace MintyCore.Network;

[Flags]
public enum MessageDirection
{
    CLIENT_TO_SERVER = 1,
    SERVER_TO_CLIENT = 2,
    BOTH = CLIENT_TO_SERVER | SERVER_TO_CLIENT
}