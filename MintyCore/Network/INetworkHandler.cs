using System;
using System.Collections.Generic;
using ENet;
using MintyCore.Utils;

namespace MintyCore.Network;

public interface INetworkHandler : IDisposable
{
    /// <summary>
    /// The internal server instance
    /// </summary>
    IConcurrentServer? Server { get; }

    /// <summary>
    /// The internal client instance
    /// </summary>
    IConcurrentClient? Client { get; }

    void AddMessage<TMessage>(Identification messageId) where TMessage : class, IMessage;

    TMessage CreateMessage<TMessage>() where TMessage : class, IMessage;
    IMessage CreateMessage(Identification messageId);
    void RemoveMessage(Identification objectId);
    void UpdateMessages();

    /// <summary>
    ///     Send a byte array directly to the server. Do not use
    /// </summary>
    void SendToServer(Span<byte> data, DeliveryMethod deliveryMethod);

    /// <summary>
    ///     Send a byte array directly to the specified clients. Do not use
    /// </summary>
    void Send(IEnumerable<ushort> receivers, Span<byte> data, DeliveryMethod deliveryMethod);

    /// <summary>
    ///     Send a byte array directly to the specified client. Do not use
    /// </summary>
    void Send(ushort receiver, Span<byte> data, DeliveryMethod deliveryMethod);

    /// <summary>
    ///     Send a byte array directly to the specified clients. Do not use
    /// </summary>
    void Send(ushort[] receivers, Span<byte> data, DeliveryMethod deliveryMethod);

    /// <summary>
    ///     Update the server and or client (processing all received messages)
    /// </summary>
    void Update();

    bool StartServer(ushort port, int maxActiveConnections);
    bool ConnectToServer(Address target);
    void StopServer();
    void StopClient();
    void ClearMessages();

    
}