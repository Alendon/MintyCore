using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

    private readonly IEventBus _eventBus;
    private readonly NetworkHandler _networkHandler;

    private NetManager? _manager;
    private NetPeer? _peer;

    internal ConcurrentClient(NetworkHandler networkHandler, string address, int port, IEventBus eventBus)
    {
        _address = address;
        _port = port;

        _eventBus = eventBus;
        _networkHandler = networkHandler;

        Start();
    }

    /// <summary>
    /// Indicates if the client is connected to a server
    /// </summary>
    public bool IsConnected => _peer?.ConnectionState is ConnectionState.Connected;

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
        if (_peer is not null || _manager is not null)
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

        //TODO connection request message
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
        if (peer.Id != _peer?.Id)
        {
            Log.Error("Received disconnect {DisconnectInfo} from unknown peer {PeerAddress}", disconnectInfo,
                peer.Address);
            return;
        }

        Log.Information("Disconnected from server ({DisconnectReason})", disconnectInfo.Reason);
        _eventBus.InvokeEvent(new DisconnectedFromServerEvent());

        _peer = null;
        _manager?.Stop();
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Log.Error("Network error: {SocketError}; at {EndPoint}", socketError, endPoint);
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader packetReader, byte channelNumber,
        DeliveryMethod deliveryMethod)
    {
        _networkHandler.ReceiveDataClient(new DataReader(packetReader));
        
        packetReader.Recycle();
        
        throw new NotImplementedException();

        var reader = new DataReader(packetReader.RawData);
        var magic = reader.MagicSequence;

        if (magic.SequenceEqual(NetworkUtils.ConnectedMessageHeader))
        {
            if (!reader.TryGetBool(out var multiThreaded))
            {
                Log.Error("Failed to get multi threaded indication");
                return;
            }

            if (multiThreaded)
                _onMultithreadedReceiveCb(Constants.ServerId, reader, true);
            else
                _receivedData.Enqueue(reader);

            return;
        }

        if (magic.SequenceEqual(NetworkUtils.UnconnectedMessageHeader))
        {
            throw new NotImplementedException("Unconnected messages are not implemented yet");
        }

        Log.Error("Received unknown message type {MagicString}", Encoding.UTF8.GetString(magic));
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        throw new NotImplementedException();
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        Log.Warning("Received connection request from {EndPoint}. This is a client", request.RemoteEndPoint);
        request.Reject();
    }
}

[RegisterEvent("disconnected_from_server")]
public struct DisconnectedFromServerEvent : IEvent
{
    public static Identification Identification => EventIDs.DisconnectedFromServer;
    public static bool ModificationAllowed => false;
}