using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Autofac;
using JetBrains.Annotations;
using LiteNetLib;
using MintyCore.Modding;
using MintyCore.Utils;
using MintyCore.Utils.Events;
using Serilog;
using OneOf;

namespace MintyCore.Network.Implementations;

/// <summary>
///     Class which handles network connections and message sending / receiving
/// </summary>
[PublicAPI]
[Singleton<INetworkHandler>]
internal sealed class NetworkHandler : INetworkHandler
{
    private ILifetimeScope? _messageScope;

    private Dictionary<Identification, Action<ContainerBuilder>> _messageCreation = new();
    private Dictionary<Identification, Type> _messageTypes = new();
    private Dictionary<MagicHeader, Identification> _unconnectedMessageIds = new();

    private Dictionary<Identification, ConcurrentBag<MessageBase>> _messagePoolById = new();
    private Dictionary<Type, ConcurrentBag<MessageBase>> _messagePoolByType = new();
    private const int MaxPooledMessages = 64;

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

    private ConcurrentQueue<MessageBase> _receivedMessages = new();

    /// <summary/>
    public required IPlayerHandler PlayerHandler { init; private get; }

    /// <summary/>
    public required IModManager ModManager { init; private get; }

    public required IEventBus EventBus { init; private get; }


    /// <inheritdoc />
    public void AddMessage<TMessage>(Identification messageId) where TMessage : Message
    {
        _messageCreation.Add(messageId,
            builder => builder.RegisterType<TMessage>().AsSelf().Keyed<Message>(messageId));

        _messageTypes.Add(messageId, typeof(TMessage));

        InvalidateMessageScope();
    }

    public void AddUnconnectedMessage<TMessage>(Identification id) where TMessage : UnconnectedMessage
    {
        _messageCreation.Add(id,
            builder => builder.RegisterType<TMessage>().AsSelf().Keyed<UnconnectedMessage>(id));
        
        _messageTypes.Add(id, typeof(TMessage));
        
        var instance = Activator.CreateInstance<TMessage>();
        _unconnectedMessageIds.Add(instance.MagicSequence, id);
        
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

        foreach (var (id, type) in _messageTypes)
        {
            ConcurrentBag<MessageBase> bag = [];

            _messagePoolById.Add(id, bag);
            _messagePoolByType.Add(type, bag);
        }
    }

    private void InvalidateMessageScope()
    {
        _messageScope?.Dispose();
        _messageScope = null;

        _messagePoolById.Clear();
        _messagePoolByType.Clear();
    }

    /// <inheritdoc />
    public TMessage CreateMessage<TMessage>() where TMessage : Message
    {
        if (_messageScope is null)
            throw new MintyCoreException("Message scope is null");

        _messagePoolByType[typeof(TMessage)].TryTake(out var genericMessage);
        if (genericMessage is not TMessage message)
        {
            message = _messageScope.Resolve<TMessage>();
        }

        message.RecycleCallback = RecyleMessage;

        return message;
    }

    public Message CreateMessage(Identification messageId)
    {
        if (_messageScope is null)
            throw new MintyCoreException("Message scope is null");

        _messagePoolById[messageId].TryTake(out var genericMessage);
        if (genericMessage is not Message message)
        {
            message = _messageScope.ResolveKeyed<Message>(messageId);
        }

        message.RecycleCallback = RecyleMessage;
        return message;
    }

    public bool TryGetUnconnectedMessage(Identification id, [MaybeNullWhen(false)] out UnconnectedMessage message)
    {
        throw new NotImplementedException();
    }

    private bool TryGetUnconnectedMessageId(MagicHeader magic, out Identification id)
    {
        throw new NotImplementedException();
    }

    private void RecyleMessage(MessageBase message)
    {
        var bag = _messagePoolById[message.MessageId];
        if (bag.Count < MaxPooledMessages)
            bag.Add(message);
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
    public void Send(IEnumerable<Player> receivers, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Send(receivers.ToArray(), data, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified client. Do not use
    /// </summary>
    public void Send(Player receiver, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Server?.SendMessage(receiver, data, deliveryMethod);
    }

    /// <summary>
    ///     Send a byte array directly to the specified clients. Do not use
    /// </summary>
    public void Send(Player[] receivers, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Server?.SendMessage(receivers, data, deliveryMethod);
    }

    public void SendToPending(int pendingId, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Server?.SendToPending(pendingId, data, deliveryMethod);
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

        _serverBackgroundWorker = BackgroundWorker.Run(this, "Net Server Background Worker");
        Server = new ConcurrentServer(port, maxActiveConnections, PlayerHandler, this);
        return true;
    }

    public bool ConnectToServer(string address, int port)
    {
        if (Client is not null) return false;

        _clientBackgroundWorker = BackgroundWorker.Run(this, "Net Client Background Worker");
        Client = new ConcurrentClient(this, address, port, EventBus);
        return true;
    }

    internal void ReceiveDataClient(DataReader reader)
    {
        ReceiveDataRaw(null, reader);
    }

    internal void ReceiveDataServer(Player sender, DataReader reader)
    {
        ReceiveDataRaw(sender, reader);
    }

    private void ReceiveDataRaw(OneOf<Player, int>? sender, DataReader reader)
    {
        var magic = reader.MagicSequence;


        if (magic == NetworkUtils.ConnectedMessageHeader)
        {
            ReceiveConnectedMessage(sender, reader);
            return;
        }

        if (TryGetUnconnectedMessage(magic, out var message))
        {
        }
    }

    private void ReceiveConnectedMessage(Player? sender, DataReader data)
    {
        if (!data.TryGetBool(out var receiveMultiThreaded))
        {
            Log.Error("Failed to get multi threaded indication");
            return;
        }

        if (receiveMultiThreaded)
        {
            var backgroundWorker = sender is null ? _clientBackgroundWorker : _serverBackgroundWorker;
            backgroundWorker?.AddData(sender, data);

            return;
        }

        _receivedMessages.Enqueue((sender, data));
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

        private readonly ConcurrentQueue<(MessageBase, DataReader)>
            _dataQueue = new();

        private readonly AutoResetEvent _newItemEvent = new(false);
        private readonly ManualResetEvent _stopEvent = new(false);
        private volatile bool _running = true;

        private BackgroundWorker(NetworkHandler parent)
        {
            _parent = parent;
        }

        public static BackgroundWorker Run(NetworkHandler parent, string threadName)
        {
            var worker = new BackgroundWorker(parent);
            var thread = new Thread(worker.Worker)
            {
                Name = threadName
            };
            thread.Start();
            return worker;
        }

        public void Stop()
        {
            _running = false;
            _stopEvent.Set();
        }

        public void AddMessageToProcess(MessageBase message, DataReader reader)
        {
            _dataQueue.Enqueue((message, reader));
            _newItemEvent.Set();
        }

        private void Worker()
        {
            WaitHandle[] waitHandles = [_newItemEvent, _stopEvent];

            while (true)
            {
                WaitHandle.WaitAny(waitHandles);
                if (!_running) return;

                while (_dataQueue.TryDequeue(out var tuple))
                {
                    ReceiveData(tuple.Item1, tuple.Item2);

                    if (!_running) return;
                }
            }
        }

        private void ReceiveData(MessageBase message, DataReader data)
        {
            if (!message.Deserialize(data))
                Log.Error("Failed to deserialize message {MessageId}", message.MessageId);

            data.Dispose();
            message.Recycle();
        }
    }
}