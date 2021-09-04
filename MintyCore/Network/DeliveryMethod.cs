namespace MintyCore.Network
{
    /// <summary>
    /// How a packet should be delivered. The values are equal to <see cref="ENet.PacketFlags"/>, a cast conversion is possible
    /// </summary>
    public enum DeliveryMethod
    {
        Unreliable = 0,
        Reliable = ENet.PacketFlags.Reliable,
        Unsequenced = ENet.PacketFlags.Unsequenced,
        UnreliableFragment = ENet.PacketFlags.UnreliableFragmented
    }
}