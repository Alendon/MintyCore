using System;
using ENet;

namespace MintyCore.Network;

internal static class NetworkHelper
{
    internal static bool CheckConnected(PeerState state)
    {
        return (int)state > 0 && (int)state < 6;
    }

    internal static byte GetChannel(DeliveryMethod deliveryMethod)
    {
        return deliveryMethod switch
        {
            DeliveryMethod.UNRELIABLE => 0,
            DeliveryMethod.RELIABLE => 1,
            DeliveryMethod.UNSEQUENCED => 2,
            DeliveryMethod.UNRELIABLE_FRAGMENT => 3,
            _ => 0
        };
    }

    internal static unsafe void Create(ref this Packet packet, Span<byte> data, PacketFlags deliveryMethod)
    {
        fixed(byte* ptr = &data.GetPinnableReference())
        {
            packet.Create(new IntPtr(ptr), data.Length, deliveryMethod);
        }
    }
}