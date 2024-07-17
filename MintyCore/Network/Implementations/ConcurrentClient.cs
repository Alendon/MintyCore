using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using MintyCore.Identifications;
using MintyCore.Network.ConnectionSetup;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyCore.Utils.Events;
using Serilog;

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
    private readonly IConnectionSetupManager _connectionSetupManager;

    private NetManager? _manager;
    private NetPeer? _peer;
    private AesEncryptionLayer _encryptionLayer;

    internal ConcurrentClient(NetworkHandler networkHandler, string address, int port, IEventBus eventBus, IConnectionSetupManager connectionSetupManager)
    {
        _address = address;
        _port = port;

        _eventBus = eventBus;
        _connectionSetupManager = connectionSetupManager;
        _networkHandler = networkHandler;
        
        _encryptionLayer = new AesEncryptionLayer();

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

        _manager = new NetManager(this, _encryptionLayer)
        {
            AutoRecycle = false,
            IPv6Enabled = true,
            UnsyncedEvents = true,
            UnsyncedDeliveryEvent = true,
            UnsyncedReceiveEvent = true
        };
        _manager.Start();

        var writer = _connectionSetupManager.CreateConnectionRequest();
        var netWriter = NetDataWriter.FromBytes(writer.ConstructBuffer().ToArray(), false);
        writer.Dispose();
        
        _peer = _manager.Connect(_address, _port, netWriter);
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
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        Log.Information("Peer connected to server");
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
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        Log.Error("Received unconnected message from {EndPoint}", remoteEndPoint);
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        
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