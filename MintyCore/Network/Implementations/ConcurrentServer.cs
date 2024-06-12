using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;
using MintyCore.Identifications;
using MintyCore.Utils;
using Serilog;
using Silk.NET.Vulkan;

namespace MintyCore.Network.Implementations;

/// <summary>
/// Server which runs concurrently
/// </summary>
public sealed class ConcurrentServer : IConcurrentServer, INetEventListener
{
    private readonly NetManager _netManager;

    private readonly int _maxActiveConnections;

    private readonly Action<ushort, DataReader, bool> _onMultiThreadedReceiveCallback;

    private readonly Dictionary<NetPeer, ushort> _peersWithId = new();

    private readonly HashSet<ushort> _pendingPeers = new();

    private readonly ConcurrentQueue<(ushort sender, DataReader data)> _receivedData = new();
    private readonly Dictionary<ushort, NetPeer> _reversedPeers = new();

    private readonly Action<ushort, DataReader, bool> _onReceiveCb;

    private IPlayerHandler PlayerHandler { get; }
    private INetworkHandler NetworkHandler { get; }

    internal ConcurrentServer(ushort port, int maxConnections,
        Action<ushort, DataReader, bool> onMultiThreadedReceiveCallback, Action<ushort, DataReader, bool> onReceiveCb,
        IPlayerHandler playerHandler,
        INetworkHandler networkHandler)
    {
        _maxActiveConnections = maxConnections;
        _onMultiThreadedReceiveCallback = onMultiThreadedReceiveCallback;
        _onReceiveCb = onReceiveCb;
        PlayerHandler = playerHandler;
        NetworkHandler = networkHandler;
        
        _netManager = new NetManager(this)
        {
            AutoRecycle = false,
            IPv6Enabled = true,
            UnsyncedEvents = true,
            UnsyncedDeliveryEvent = true,
            UnsyncedReceiveEvent = true
        };
        
        _netManager.Start(port);
    }


    /// <inheritdoc />
    public void Dispose()
    {
        _netManager.Stop(true);
    }

    private void HandleEvent(Event @event)
    {
        switch (@event.Type)
        {
            case PlayerEvent.EventType.Receive:
            {
                var reader = new DataReader(@event.Packet);

                if (!_peersWithId.TryGetValue(@event.Peer, out var id)) break;

                if (!reader.TryGetBool(out var multiThreaded))
                {
                    Log.Error("Failed to get multi threaded indication");
                    break;
                }

                if (multiThreaded)
                {
                    _onMultiThreadedReceiveCallback(id, reader, true);
                    break;
                }

                _receivedData.Enqueue((id, reader));
                break;
            }
            case PlayerEvent.EventType.Connect:
            {
                Log.Information("New peer connected");
                var tempId = ushort.MaxValue;
                while (_pendingPeers.Contains(tempId)) tempId--;

                _pendingPeers.Add(tempId);
                _peersWithId.Add(@event.Peer, tempId);
                _reversedPeers.Add(tempId, @event.Peer);

                var request = NetworkHandler.CreateMessage(MessageIDs.RequestPlayerInfo);
                request.Send(tempId);

                break;
            }
            case PlayerEvent.EventType.Timeout:
            case PlayerEvent.EventType.Disconnect:
            {
                var reason = (DisconnectReasons)@event.Data;

                if (_peersWithId.Remove(@event.Peer, out var peerId))
                {
                    _reversedPeers.Remove(peerId);

                    if (_pendingPeers.Remove(peerId))
                    {
                        Log.Information("Pending peer {PeerId} disconnected", peerId);
                        break;
                    }

                    var player = PlayerHandler.GetPlayerName(peerId);
                    Log.Information("Player {Player}:{PeerId} disconnected ({DisconnectReason})",
                        player, peerId, reason);
                    PlayerHandler.DisconnectPlayer(peerId, true);

                    break;
                }

                Log.Information("Unknown Peer disconnected");
                break;
            }
        }
    }

    /// <summary>
    /// Update the server
    /// </summary>
    public void Update()
    {
        while (_receivedData.TryDequeue(out var readerSender))
            _onReceiveCb(readerSender.sender, readerSender.data, true);
    }

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="IMessage"/> interface messages
    /// </summary>
    public void SendMessage(ushort[] receivers, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        foreach (var receiver in receivers)
        {
            _reversedPeers[receiver].Send(data, deliveryMethod);
        }
    }

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="IMessage"/> interface messages
    /// </summary>
    public void SendMessage(ushort receiver, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        _reversedPeers[receiver].Send(data, deliveryMethod);
    }

    /// <summary>
    /// Check if an id is in a pending state
    /// </summary>
    public bool IsPending(ushort tempId)
    {
        return _pendingPeers.Contains(tempId);
    }

    /// <summary>
    /// Reject a pending id
    /// </summary>
    /// <param name="tempId"></param>
    public void RejectPending(ushort tempId)
    {
        _pendingPeers.Remove(tempId);
        _reversedPeers.Remove(tempId, out var peer);
        _peersWithId.Remove(peer);
        peer.Disconnect((uint)DisconnectReasons.Reject);
    }

    /// <summary>
    /// Accept a pending id and replace it with a proper game id
    /// </summary>
    public void AcceptPending(ushort tempId, ushort gameId)
    {
        _pendingPeers.Remove(tempId);
        _reversedPeers.Remove(tempId, out var peer);
        _peersWithId.Remove(peer);

        _reversedPeers.Add(gameId, peer);
        _peersWithId.Add(peer, gameId);
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