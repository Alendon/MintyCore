using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    private BackgroundWorker? _serverBackgroundWorker;

    /// <summary>
    /// The internal client instance
    /// </summary>
    public IConcurrentClient? Client { get; private set; }

    private BackgroundWorker? _clientBackgroundWorker;

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

        _messageScope = ModManager.ModLifetimeScope.BeginLifetimeScope("messages", builder =>
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

        _serverBackgroundWorker = BackgroundWorker.Run(this);
        Server = new ConcurrentServer(port, maxActiveConnections, _serverBackgroundWorker.AddData, ReceiveData, PlayerHandler, this);
        return true;
    }

    public bool ConnectToServer(Address target)
    {
        if (Client is not null) return false;

        _clientBackgroundWorker = BackgroundWorker.Run(this);
        Client = new ConcurrentClient(target, _clientBackgroundWorker.AddData, ReceiveData, EventBus);
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
        _serverBackgroundWorker?.Stop();
        Server?.Dispose();
        Server = null;
    }

    public void StopClient()
    {
        _clientBackgroundWorker?.Stop();
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

    class BackgroundWorker
    {
        private readonly NetworkHandler _parent;

        private readonly ConcurrentQueue<(ushort sender, DataReader data, bool server)> _dataQueue = new();
        
        private readonly AutoResetEvent _newItemEvent = new(false);
        private readonly ManualResetEvent _stopEvent = new(false);
        private volatile bool _running = true;

        private BackgroundWorker(NetworkHandler parent)
        {
             _parent = parent;
        }

        public static BackgroundWorker Run(NetworkHandler parent)
        {
            var worker = new BackgroundWorker(parent);
            var thread = new Thread(worker.Worker);
            thread.Start();
            return worker;
        }
        
        public void Stop()
        {
            _running = false;
            _stopEvent.Set();
        }
        
        public void AddData(ushort sender, DataReader data, bool server)
        {
            _dataQueue.Enqueue((sender, data, server));
            _newItemEvent.Set();
        }
        
        private void Worker()
        {
            WaitHandle[] waitHandles = [_newItemEvent, _stopEvent];

            while (true)
            {
                WaitHandle.WaitAny(waitHandles);
                if (!_running) return;

                while (_dataQueue.TryDequeue(out var items))
                {
                    ReceiveData(items.sender, items.data, items.server);
                    
                    if (!_running) return;
                }
            }
        }

        private void ReceiveData(ushort sender, DataReader data, bool server)
        {
            if (!Identification.Deserialize(data, out var messageId))
            {
                Log.Error("Failed to deserialize message id");
                return;
            }

            var message = _parent.CreateMessage(messageId);
            message.IsServer = server;
            message.Sender = sender;

            if (!message.Deserialize(data))
                Log.Error("Failed to deserialize message {MessageId}", messageId);

            data.Dispose();
        }
    }
}