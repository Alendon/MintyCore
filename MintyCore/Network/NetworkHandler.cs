using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ENet;
using MintyCore.Utils;

namespace MintyCore.Network;

/// <summary>
///     Class which handles network connections and message sending / receiving
/// </summary>
public static class NetworkHandler
{
    private static readonly Dictionary<Identification, ConcurrentQueue<IMessage>> _messages = new();
    private static readonly Dictionary<Identification, Func<IMessage>> _messageCreation = new();

    /// <summary>
    /// The internal server instance
    /// </summary>
    public static ConcurrentServer? Server { get; private set; }

    /// <summary>
    /// The internal client instance
    /// </summary>
    public static ConcurrentClient? Client { get; private set; }

    internal static void SetMessage<T>(Identification messageId) where T : class, IMessage, new()
    {
        _messageCreation.Remove(messageId);
        _messages.Remove(messageId);
        AddMessage<T>(messageId);
    }

    internal static void AddMessage<TMessage>(Identification messageId) where TMessage : class, IMessage, new()
    {
        _messages.Add(messageId, new ConcurrentQueue<IMessage>());
        _messageCreation.Add(messageId, () => new TMessage());
    }

    /// <summary>
    ///     Send a byte array directly to the server. Do not use
    /// </summary>
    public static void SendToServer(Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Client?.SendMessage(data, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified clients. Do not use
    /// </summary>
    public static void Send(IEnumerable<ushort> receivers, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Send(receivers.ToArray(), data, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified client. Do not use
    /// </summary>
    public static void Send(ushort receiver, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Server?.SendMessage(receiver, data, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified clients. Do not use
    /// </summary>
    public static void Send(ushort[] receivers, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Server?.SendMessage(receivers, data, deliveryMethod);
    }

    /// <summary>
    ///     Update the server and or client (processing all received messages)
    /// </summary>
    public static void Update()
    {
        Server?.Update();
        Client?.Update();
    }


    internal static bool StartServer(ushort port, int maxActiveConnections)
    {
        if (Server is not null) return false;

        Server = new ConcurrentServer(port, maxActiveConnections, ReceiveData);
        return true;
    }

    internal static bool ConnectToServer(Address target)
    {
        if (Client is not null) return false;

        Client = new ConcurrentClient(target, ReceiveData);
        return true;
    }

    private static void ReceiveData(ushort sender, DataReader data, bool server)
    {
        if (!Identification.Deserialize(data, out var messageId))
        {
            Logger.WriteLog("Failed to deserialize message id", LogImportance.Error, "Network");
            return;
        }

        var message = GetMessageObject(messageId);
        message.IsServer = server;
        message.Sender = sender;
        Logger.AssertAndLog(message.Deserialize(data), $"Failed to deserialize message {messageId}", "Network",
            LogImportance.Error);

        ReturnMessageObject(message);
        data.Dispose();
    }

    internal static void StopServer()
    {
        Server?.Dispose();
        Server = null;
    }

    internal static void StopClient()
    {
        Client?.Dispose();
        Client = null;
    }

    private static IMessage GetMessageObject(Identification messageId)
    {
        return _messages[messageId].TryDequeue(out var message) ? message : _messageCreation[messageId]();
    }

    private static void ReturnMessageObject(IMessage message)
    {
        message.Clear();
        _messages[message.MessageId].Enqueue(message);
    }

    internal static void ClearMessages()
    {
        _messages.Clear();
        _messageCreation.Clear();
    }

    /// <summary>
    /// Get a new message object by a id
    /// </summary>
    public static IMessage GetMessage(Identification requestPlayerInformation)
    {
        return _messageCreation[requestPlayerInformation]();
    }

    internal static void RemoveMessage(Identification objectId)
    {
        _messageCreation.Remove(objectId);
        _messages.Remove(objectId);
    }
}