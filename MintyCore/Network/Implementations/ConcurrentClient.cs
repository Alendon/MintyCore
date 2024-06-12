using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyCore.Utils.Events;
using Serilog;
using Silk.NET.Vulkan;

namespace MintyCore.Network.Implementations;

/// <summary>
/// Client which connects to a server concurrently
/// </summary>
public sealed class ConcurrentClient : IConcurrentClient, INetEventListener
{
    /// <summary>
    ///     Address of the server to connect to
    /// </summary>
    private readonly string _address;

    private readonly int _port;

    /// <summary>
    ///     Callback to invoke when a message is received
    ///     <code>void OnReceive(ushort sender, DataReader data, bool isServer);</code>
    /// </summary>
    private readonly Action<ushort, DataReader, bool> _onMultithreadedReceiveCb;

    /// <summary>
    ///     Queue to store packages before sending
    /// </summary>
    private readonly ConcurrentQueue<(Packet packet, DeliveryMethod deliveryMethod)> _packets = new();

    /// <summary>
    ///     Queue to store message data which needs to be processed on the main thread
    /// </summary>
    private readonly ConcurrentQueue<DataReader> _receivedData = new();
    
    private readonly IEventBus _eventBus;
    private readonly Action<ushort,DataReader,bool> _onReceiveCallback;
    
    private NetManager? _manager;
    private NetPeer? _peer;

    internal ConcurrentClient(string address, int port, Action<ushort, DataReader, bool> onMultithreadedReceiveCallback,
        Action<ushort, DataReader, bool> receiveCallback, IEventBus eventBus)
    {
        _address = address;
        _port = port;
        
        _onMultithreadedReceiveCb = onMultithreadedReceiveCallback;
        _onReceiveCallback = receiveCallback;
        _eventBus = eventBus;

        Start();
    }

    /// <summary>
    /// Indicates if the client is connected to a server
    /// </summary>
    public bool IsConnected => NetworkHelper.CheckConnected(_connection.ConnectionState);

    /// <inheritdoc />
    public void Dispose()
    {
        _peer?.Disconnect();
        _manager?.Stop();
        
        _peer = null;
        _manager = null;
    }

    private void Start()
    {
        if(_peer is not null || _manager is not null)
            throw new InvalidOperationException("Client is already started");
        
        _manager = new NetManager(this)
        {
            AutoRecycle = false,
            IPv6Enabled = true,
            UnsyncedEvents = true,
            UnsyncedDeliveryEvent = true,
            UnsyncedReceiveEvent = true
        };
        _manager.Start();

        var writer = NetDataWriter.FromBytes();
        _peer = _manager.Connect(_address, _port, writer);
    }

    private void HandleEvent(Event @event)
    {
        //Enet provides 3(/4) major event types to handle
        switch (@event.Type)
        {
            case PlayerEvent.EventType.Connect:
            {
                _connection = @event.Peer;
                Log.Information("Connected to server");
                break;
            }

            case PlayerEvent.EventType.Receive:
            {
                //create a reader for the received data.
                var reader = new DataReader(@event.Packet);

                if (!reader.TryGetBool(out var multiThreaded))
                {
                    Log.Error("Failed to get multi threaded indication");
                    break;
                }

                if (multiThreaded)
                    _onMultithreadedReceiveCb(Constants.ServerId, reader, false);
                else
                    _receivedData.Enqueue(reader);

                break;
            }

            case PlayerEvent.EventType.Timeout:
            case PlayerEvent.EventType.Disconnect:
            {
                var reason = @event.Type == PlayerEvent.EventType.Disconnect
                    ? (DisconnectReasons) @event.Data
                    : DisconnectReasons.TimeOut;
                Log.Information("Disconnected from server ({DisconnectReason})", reason);
                //TODO implement proper disconnect logic
                _connection = default;
                _eventBus.InvokeEvent(new DisconnectedFromServerEvent());
                _hostShouldClose = true;
                break;
            }
        }
    }

    /// <summary>
    ///     Send a message to the server
    /// </summary>
    /// <param name="data">Span containing the data</param>
    /// <param name="deliveryMethod">How to deliver the message</param>
    public void SendMessage(Span<byte> data, DeliveryMethod deliveryMethod)
    {
        _peer?.Send(data, deliveryMethod);
    }

    /// <summary>
    ///     Update the client
    /// </summary>
    public void Update()
    {
        while (_receivedData.TryDequeue(out var reader)) _onReceiveCallback(Constants.ServerId, reader, false);
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        throw new NotImplementedException();
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        throw new NotImplementedException();
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        throw new NotImplementedException();
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        throw new NotImplementedException();
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        throw new NotImplementedException();
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        throw new NotImplementedException();
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        throw new NotImplementedException();
    }
}

[RegisterEvent("disconnected_from_server")]
public struct DisconnectedFromServerEvent : IEvent
{
    public static Identification Identification => EventIDs.DisconnectedFromServer;
    public static bool ModificationAllowed => false;
}