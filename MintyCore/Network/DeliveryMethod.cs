using ENet;

namespace MintyCore.Network
{
    /// <summary>
    /// How a packet should be delivered. The values are equal to <see cref="ENet.PacketFlags"/>, a cast conversion is possible
    /// </summary>
    public enum DeliveryMethod
    {
        Unreliable = 0,
        Reliable = PacketFlags.Reliable,
        Unsequenced = PacketFlags.Unsequenced,
        UnreliableFragment = PacketFlags.UnreliableFragmented
    }
}