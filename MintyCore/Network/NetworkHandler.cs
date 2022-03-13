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

    public static ConcurrentServer? _server { get; private set; }
    public static ConcurrentClient? _client { get; private set; }

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
    public static void SendToServer(byte[] data, int dataLength, DeliveryMethod deliveryMethod)
    {
        _client?.SendMessage(data, dataLength, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified clients. Do not use
    /// </summary>
    public static void Send(IEnumerable<ushort> receivers, byte[] data, int dataLength,
        DeliveryMethod deliveryMethod)
    {
        Send(receivers.ToArray(), data, dataLength, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified client. Do not use
    /// </summary>
    public static void Send(ushort receiver, byte[] data, int dataLength, DeliveryMethod deliveryMethod)
    {
        _server?.SendMessage(receiver, data, dataLength, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified clients. Do not use
    /// </summary>
    public static void Send(ushort[] receivers, byte[] data, int dataLength, DeliveryMethod deliveryMethod)
    {
        _server?.SendMessage(receivers, data, dataLength, deliveryMethod);
    }

    /// <summary>
    ///     Update the server and or client (processing all received messages)
    /// </summary>
    public static void Update()
    {
        _server?.Update();
        _client?.Update();
    }


    internal static bool StartServer(ushort port, int maxActiveConnections)
    {
        if (_server is not null) return false;

        _server = new ConcurrentServer(port, maxActiveConnections, ReceiveData);
        return true;
    }

    internal static bool ConnectToServer(Address target)
    {
        if (_client is not null) return false;

        _client = new ConcurrentClient(target, ReceiveData);
        return true;
    }

    private static void ReceiveData(ushort sender, DataReader data, bool server)
    {
        if (!Identification.Deserialize(data, out var messageId))
        {
            Logger.WriteLog("Failed to deserialize message id", LogImportance.ERROR, "Network");
            return;
        }

        var message = GetMessageObject(messageId);
        message.IsServer = server;
        if (!message.Deserialize(data))
            Logger.WriteLog($"Failed to deserialize message {messageId}", LogImportance.ERROR, "Network");
        ReturnMessageObject(message);
    }

    internal static void StopServer()
    {
        _server?.Dispose();
        _server = null;
    }

    internal static void StopClient()
    {
        _client?.Dispose();
        _client = null;
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
}