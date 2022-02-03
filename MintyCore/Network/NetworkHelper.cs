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
        switch (deliveryMethod)
        {
            case DeliveryMethod.UNRELIABLE: return 0;
            case DeliveryMethod.RELIABLE: return 1;
            case DeliveryMethod.UNSEQUENCED: return 2;
            case DeliveryMethod.UNRELIABLE_FRAGMENT: return 3;
            default: return 0;
        }
    }
}