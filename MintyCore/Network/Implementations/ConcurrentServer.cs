using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using DotNext.Threading;
using LiteNetLib;
using MintyCore.Network.ConnectionSetup;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Network.Implementations;

/// <summary>
/// Server which runs concurrently
/// </summary>
public sealed class ConcurrentServer : IConcurrentServer, INetEventListener
{
    private readonly NetManager _netManager;
    private AesEncryptionLayer _encryptionLayer;

    private readonly int _maxActiveConnections;


    private readonly Dictionary<NetPeer, Player> _peerPlayers = new();
    private readonly Dictionary<Player, NetPeer> _playerPeers = new();

    //TODO add time tracking/other measures to limit the amount of pending connections
    private HashSet<int> _pendingPeers = new();

    private IPlayerHandler PlayerHandler { get; }
    private NetworkHandler NetworkHandler { get; }
    private IConnectionSetupManager ConnectionSetupManager { get; }

    internal ConcurrentServer(ushort port, int maxConnections, IPlayerHandler playerHandler,
        NetworkHandler networkHandler, IConnectionSetupManager connectionSetupManager)
    {
        _maxActiveConnections = maxConnections;
        PlayerHandler = playerHandler;
        NetworkHandler = networkHandler;
        ConnectionSetupManager = connectionSetupManager;
        _encryptionLayer = new AesEncryptionLayer();

        _netManager = new NetManager(this, _encryptionLayer)
        {
            AutoRecycle = false,
            IPv6Enabled = true,
            UnsyncedEvents = true,
            UnsyncedDeliveryEvent = true,
            UnsyncedReceiveEvent = true,
        };

        _netManager.Start(port);
    }


    /// <inheritdoc />
    public void Dispose()
    {
        _netManager.Stop(true);
    }

    /// <summary>
    /// Update the server
    /// </summary>
    public void Update()
    {
    }

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="Message"/> interface messages
    /// </summary>
    public void SendMessage(Player[] receivers, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        using var _ = _playerPeers.AcquireReadLock();
        foreach (var receiver in receivers)
        {
            _playerPeers[receiver].Send(data, deliveryMethod);
        }
    }

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="Message"/> interface messages
    /// </summary>
    public void SendMessage(Player receiver, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        using var _ = _playerPeers.AcquireReadLock();
        _playerPeers[receiver].Send(data, deliveryMethod);
    }


    public void SendToPending(int tempId, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Debug.Assert(IsPending(tempId), "Trying to send to a non pending client");
        var peer = _netManager.GetPeerById(tempId);
        peer.Send(data, deliveryMethod);
    }

    /// <summary>
    /// Check if an id is in a pending state
    /// </summary>
    public bool IsPending(int tempId)
    {
        return _pendingPeers.Contains(tempId);
    }

    public void AcceptPending(int tempId, Player player)
    {
        using var pendingLock = _pendingPeers.AcquireWriteLock();   
        if (!_pendingPeers.Remove(tempId))
        {
            Log.Error("Trying to accept a non pending client");
            return;
        }
        
        using var playerLock = _playerPeers.AcquireWriteLock();
        using var peerLock = _peerPlayers.AcquireWriteLock();
        
        if (_playerPeers.ContainsKey(player))
            throw new InvalidOperationException("Player is already marked as connected");

        var peer = _netManager.GetPeerById(tempId);

        _playerPeers[player] = peer;
        _peerPlayers[peer] = player;
    }

    public void RejectPending(int tempId)
    {
        using var pendingLock = _pendingPeers.AcquireWriteLock();
        
        if (!_pendingPeers.Remove(tempId))
        {
            Log.Error("Trying to reject a non pending client");
            return;
        }

        var peer = _netManager.GetPeerById(tempId);

        //TODO send rejection message
        peer.Disconnect();
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

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber,
        DeliveryMethod deliveryMethod)
    {
        using var _ = _peerPlayers.AcquireReadLock();
        
        if (!_peerPlayers.TryGetValue(peer, out var player))
        {
            Log.Error("Received data from unknown peer");
            return;
        }

        NetworkHandler.ReceiveDataServer(player, new DataReader(reader));
        reader.Recycle();
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        Log.Warning("Received unconnected message. Not supported currently");
        reader.Recycle();
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        using var pendingLock = _pendingPeers.AcquireWriteLock();
        if (_pendingPeers.Count >= _maxActiveConnections)
        {
            Log.Warning("Rejecting connection request from {EndPoint} due to server being full",
                request.RemoteEndPoint);
            request.Reject();
            return;
        }

        var dataReader = new DataReader(request.Data);
        var peer = request.Accept();
        
        _pendingPeers.Add(peer.Id);

        if (ConnectionSetupManager.TryAddPendingConnection(peer.Id, dataReader)) return;
        
        Log.Error("Failed to add pending connection");
        //TODO send disconnect info
        peer.Disconnect();
        _pendingPeers.Remove(peer.Id);
    }
}