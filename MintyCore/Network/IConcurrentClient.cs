using System;

namespace MintyCore.Network;

public interface IConcurrentClient : IDisposable
{
    /// <summary>
    /// Indicates if the client is connected to a server
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Send a message to the server
    /// </summary>
    /// <param name="data">Span containing the data</param>
    /// <param name="deliveryMethod">How to deliver the message</param>
    void SendMessage(Span<byte> data, DeliveryMethod deliveryMethod);

    /// <summary>
    ///     Update the client
    /// </summary>
    void Update();
}