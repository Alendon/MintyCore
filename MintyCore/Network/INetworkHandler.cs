using System;
using System.Collections.Generic;
using ENet;
using MintyCore.Utils;

namespace MintyCore.Network;

/// <summary>
/// Defines the interface for a network handler in the game.
/// This interface is responsible for managing network connections and message sending/receiving.
/// </summary>
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

    /// <summary>
    /// Adds a message to the network handler.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="messageId">The identification of the message.</param>
    void AddMessage<TMessage>(Identification messageId) where TMessage : class, IMessage;

    /// <summary>
    /// Creates a message of the specified type.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <returns>The created message.</returns>
    TMessage CreateMessage<TMessage>() where TMessage : class, IMessage;

    /// <summary>
    /// Creates a message with the specified identification.
    /// </summary>
    /// <param name="messageId">The identification of the message.</param>
    /// <returns>The created message.</returns>
    IMessage CreateMessage(Identification messageId);

    /// <summary>
    /// Removes a message from the network handler.
    /// </summary>
    /// <param name="objectId">The identification of the message to remove.</param>
    void RemoveMessage(Identification objectId);

    /// <summary>
    /// Updates the messages in the network handler.
    /// </summary>
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

    /// <summary>
    /// Starts the server with the specified port and maximum active connections.
    /// </summary>
    /// <param name="port">The port to start the server on.</param>
    /// <param name="maxActiveConnections">The maximum number of active connections.</param>
    /// <returns>True if the server started successfully, false otherwise.</returns>
    bool StartServer(ushort port, int maxActiveConnections);

    /// <summary>
    /// Connects to the server at the specified address.
    /// </summary>
    /// <param name="target">The address of the server to connect to.</param>
    /// <returns>True if the connection was successful, false otherwise.</returns>
    bool ConnectToServer(Address target);

    /// <summary>
    /// Stops the server.
    /// </summary>
    void StopServer();

    /// <summary>
    /// Stops the client.
    /// </summary>
    void StopClient();

    /// <summary>
    /// Clears all messages from the network handler.
    /// </summary>
    void ClearMessages();
}