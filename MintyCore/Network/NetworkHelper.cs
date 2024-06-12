using System;
using System.Net;
using LiteNetLib;

namespace MintyCore.Network;

public static class NetworkHelper
{
    internal static bool CheckConnected(ConnectionState state)
    {
        return (state & ConnectionState.Connected) != 0;
    }

    internal static byte GetChannel(DeliveryMethod deliveryMethod)
    {
        return deliveryMethod switch
        {
            DeliveryMethod.Unreliable => 0,
            DeliveryMethod.ReliableUnordered => 1,
            DeliveryMethod.Sequenced => 2,
            DeliveryMethod.ReliableOrdered => 3,
            DeliveryMethod.ReliableSequenced => 4,
            _ => 0
        };
    }
}