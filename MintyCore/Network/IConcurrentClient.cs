using System;
using JetBrains.Annotations;
using LiteNetLib;

namespace MintyCore.Network;

/// <summary>
///   Represents a client that can send and receive messages concurrently
/// </summary>
[PublicAPI]
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

    void SetEncryption(byte[] aesKey);
}