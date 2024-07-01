using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Network.UnconnectedMessages;
using MintyCore.Utils;
using Serilog;
using Silk.NET.Vulkan;
using static MintyCore.Network.NetworkUtils;

namespace MintyCore.Network.Implementations;

/// <summary>
/// Server which runs concurrently
/// </summary>
public sealed class ConcurrentServer : IConcurrentServer, INetEventListener
{
    private readonly NetManager _netManager;

    private readonly int _maxActiveConnections;


    private readonly Dictionary<NetPeer, Player> _peerPlayers = new();
    private readonly Dictionary<Player, NetPeer> _playerPeers = new();
    
    //TODO add time tracking/other measures to limit the amount of pending connections
    private HashSet<int> _pendingPeers = new();
    
    private IPlayerHandler PlayerHandler { get; }
    private NetworkHandler NetworkHandler { get; }
    private IModManager ModManager { get; }

    internal ConcurrentServer(ushort port, int maxConnections,
        IPlayerHandler playerHandler,
        NetworkHandler networkHandler)
    {
        _maxActiveConnections = maxConnections;
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

                if (!_peerPlayers.TryGetValue(@event.Peer, out var id)) break;

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
                _peerPlayers.Add(@event.Peer, tempId);
                _playerPeers.Add(tempId, @event.Peer);

                var request = NetworkHandler.CreateMessage(MessageIDs.RequestPlayerInfo);
                request.Send(tempId);

                break;
            }
            case PlayerEvent.EventType.Timeout:
            case PlayerEvent.EventType.Disconnect:
            {
                var reason = (DisconnectReasons)@event.Data;

                if (_peerPlayers.Remove(@event.Peer, out var peerId))
                {
                    _playerPeers.Remove(peerId);

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
        
    }

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="Message"/> interface messages
    /// </summary>
    public void SendMessage(Player[] receivers, Span<byte> data, DeliveryMethod deliveryMethod)
    {
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
        _playerPeers[receiver].Send(data, deliveryMethod);
    }

    

    public void SendToPending(int tempId, Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Debug.Assert(IsPending(tempId), "Trying to send to a non pending client");
        var peer = _netManager.GetPeerById(tempId);
        
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
        if (!_pendingPeers.Remove(tempId))
        {
            Log.Error("Trying to accept a non pending client");
            return;
        }
        
        if(_playerPeers.ContainsKey(player))
            throw new InvalidOperationException("Player is already marked as connected");
        
        var peer = _netManager.GetPeerById(tempId);
        
        _playerPeers[player] = peer;
        _peerPlayers[peer] = player;
    }

    public void RejectPending(int tempId)
    {
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
        var dataReader = new DataReader(request.Data);
        var magic = dataReader.MagicSequence;

        if (ConnectionRequestHeader != magic ||
            !ConnectionRequestData.TryDeserialize(dataReader, out var requestData))
        {
            Log.Information("Received connection request with malformed connection request data");
            request.Reject(NetDataWriter.FromString("Malformed connection request data"));
            return;
        }

        List<ModManifest> serverMods;
        var requiredMods = serverMods.Where(x => x.IsRootMod);

        List<(string, Version)> missingMods = [];
        foreach (var requiredMod in requiredMods)
        {
            if (requestData.loadedRootMods.Any(x => x.modId == requiredMod.Identifier))
                continue;
            missingMods.Add((requiredMod.Identifier, requiredMod.Version));
        }

        if (missingMods.Count != 0)
        {
            Log.Information("Connection request contains missing root mods");
            request.Reject(NetDataWriter.FromString(
                $"Missing root mods: {string.Join(',', missingMods.Select(x => $"[{x.Item1}:{x.Item2}]"))}"));

            return;
        }
        
        

        throw new NotImplementedException();
    }
}