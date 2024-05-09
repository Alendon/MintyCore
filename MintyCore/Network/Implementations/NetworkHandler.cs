using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using ENet;
using JetBrains.Annotations;
using MintyCore.Modding;
using MintyCore.Utils;
using MintyCore.Utils.Events;
using Serilog;

namespace MintyCore.Network.Implementations;

/// <summary>
///     Class which handles network connections and message sending / receiving
/// </summary>
[PublicAPI]
[Singleton<INetworkHandler>]
internal sealed class NetworkHandler : INetworkHandler
{
    private ILifetimeScope? _messageScope;

    private Dictionary<Identification, Action<ContainerBuilder>> _messageCreation =
        new();

    /// <summary>
    /// The internal server instance
    /// </summary>
    public IConcurrentServer? Server { get; private set; }

    /// <summary>
    /// The internal client instance
    /// </summary>
    public IConcurrentClient? Client { get; private set; }

    /// <summary/>
    public required IPlayerHandler PlayerHandler { init; private get; }
    /// <summary/>
    public required IModManager ModManager { init; private get; }
    public required IEventBus EventBus { init; private get; }


    /// <inheritdoc />
    public void AddMessage<TMessage>(Identification messageId) where TMessage : class, IMessage
    {
        _messageCreation.Add(messageId,
            builder => builder.RegisterType<TMessage>().AsSelf().Keyed<IMessage>(messageId));
        
        InvalidateMessageScope();
    }
    
    public void RemoveMessage(Identification objectId)
    {
        _messageCreation.Remove(objectId);
        
        InvalidateMessageScope();
    }

    /// <inheritdoc />
    public void UpdateMessages()
    {
        InvalidateMessageScope();

        _messageScope = ModManager.ModLifetimeScope.BeginLifetimeScope("messages",builder =>
        {
            foreach (var messageCreator in _messageCreation.Values)
            {
                messageCreator(builder);
            }
        });
    }

    private void InvalidateMessageScope()
    {
        _messageScope?.Dispose();
        _messageScope = null;
    }

    /// <inheritdoc />
    public TMessage CreateMessage<TMessage>() where TMessage : class, IMessage
    {
        if (_messageScope is null)
            throw new MintyCoreException("Message scope is null");
        return _messageScope.Resolve<TMessage>();
    }

    public IMessage CreateMessage(Identification messageId)
    {
        if (_messageScope is null)
            throw new MintyCoreException("Message scope is null");
        return _messageScope.ResolveKeyed<IMessage>(messageId);
    }


    /// <summary>
    ///     Send a byte array directly to the server. Do not use
    /// </summary>
    public void SendToServer(Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Client?.SendMessage(data, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified clients. Do not use
    /// </summary>
    public void Send(IEnumerable<ushort> receivers, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Send(receivers.ToArray(), data, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified client. Do not use
    /// </summary>
    public void Send(ushort receiver, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Server?.SendMessage(receiver, data, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified clients. Do not use
    /// </summary>
    public void Send(ushort[] receivers, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Server?.SendMessage(receivers, data, deliveryMethod);
    }

    /// <summary>
    ///     Update the server and or client (processing all received messages)
    /// </summary>
    public void Update()
    {
        Server?.Update();
        Client?.Update();
    }


    public bool StartServer(ushort port, int maxActiveConnections)
    {
        if (Server is not null) return false;

        Server = new ConcurrentServer(port, maxActiveConnections, ReceiveData, PlayerHandler, this);
        return true;
    }

    public bool ConnectToServer(Address target)
    {
        if (Client is not null) return false;

        Client = new ConcurrentClient(target, ReceiveData, EventBus);
        return true;
    }

    private void ReceiveData(ushort sender, DataReader data, bool server)
    {
        if (!Identification.Deserialize(data, out var messageId))
        {
            Log.Error("Failed to deserialize message id");
            return;
        }

        var message = CreateMessage(messageId);
        message.IsServer = server;
        message.Sender = sender;
        
        if (!message.Deserialize(data))
            Log.Error("Failed to deserialize message {MessageId}", messageId);

        data.Dispose();
    }

    public void StopServer()
    {
        Server?.Dispose();
        Server = null;
    }

    public void StopClient()
    {
        Client?.Dispose();
        Client = null;
    }


    public void ClearMessages()
    {
        _messageCreation.Clear();
        InvalidateMessageScope();
    }

    public void Dispose()
    {
        _messageScope?.Dispose();
        _messageScope = null;

        Server?.Dispose();
        Server = null;

        Client?.Dispose();
        Client = null;
    }
}