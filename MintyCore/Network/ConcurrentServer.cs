using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ENet;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Network;

/// <summary>
/// Server which runs concurrently
/// </summary>
public class ConcurrentServer : IDisposable
{
    private readonly Address _address;

    private readonly int _maxActiveConnections;

    private readonly
        ConcurrentQueue<(ushort[] receivers, byte[] data, int dataLength, DeliveryMethod deliveryMethod)>
        _multiReceiverPackets = new();

    private readonly Action<ushort, DataReader, bool> _onReceiveCb;

    private readonly Dictionary<Peer, ushort> _peersWithId = new();

    private readonly HashSet<ushort> _pendingPeers = new();

    private readonly ConcurrentQueue<(ushort sender, DataReader data)> _receivedData = new();
    private readonly Dictionary<ushort, Peer> _reversedPeers = new();

    private readonly
        ConcurrentQueue<(ushort receiver, byte[] data, int dataLength, DeliveryMethod deliveryMethod)>
        _singleReceiverPackets = new();

    private volatile bool _hostShouldClose;
    private Thread? _networkThread;

    internal ConcurrentServer(ushort port, int maxConnections,
        Action<ushort, DataReader, bool> onMultiThreadedReceiveCallback)
    {
        _address = new Address
        {
            Port = port
        };
        _address.SetHost("localhost");

        _maxActiveConnections = maxConnections;
        _onReceiveCb = onMultiThreadedReceiveCallback;
        Start();
    }


    /// <inheritdoc />
    public void Dispose()
    {
        _hostShouldClose = true;
        _networkThread?.Join();
    }

    private void Start()
    {
        _networkThread = new Thread(Worker)
        {
            Name = "Server Network Thread"
        };
        _networkThread.Start();
    }

    private void Worker()
    {
        Host host = new();
        host.Create(_address, _maxActiveConnections, Constants.ChannelCount);

        while (!_hostShouldClose)
        {
            SendPackets();
            if (host.Service(1, out var @event) != 1) continue;
            do
            {
                HandleEvent(@event);
                @event.Packet.Dispose();
            } while (host.CheckEvents(out @event) == 1);
        }

        host.Dispose();
    }

    private void HandleEvent(Event @event)
    {
        switch (@event.Type)
        {
            case EventType.Receive:
            {
                var reader = new DataReader(@event.Packet);
                
                if (!_peersWithId.TryGetValue(@event.Peer, out var id)) break;

                if (!Logger.AssertAndLog(reader.TryGetBool(out var multiThreaded),
                        "Failed to get multi threaded indication", "Network", LogImportance.ERROR)) break;
                if (multiThreaded)
                {
                    _onReceiveCb(id, reader, true);
                    break;
                }

                _receivedData.Enqueue((id, reader));
                break;
            }
            case EventType.Connect:
            {
                Logger.WriteLog("New peer connected", LogImportance.INFO, "Network");

                ushort tempId = ushort.MaxValue;
                while (_pendingPeers.Contains(tempId)) tempId--;

                _pendingPeers.Add(tempId);
                _peersWithId.Add(@event.Peer, tempId);
                _reversedPeers.Add(tempId, @event.Peer);

                IMessage request = NetworkHandler.GetMessage(MessageIDs.RequestPlayerInformation);
                request.Send(tempId);
                
                break;
            }
            case EventType.Timeout:
            case EventType.Disconnect:
            {
                var reason = (DisconnectReasons)@event.Data;

                if (_peersWithId.Remove(@event.Peer, out var peerId))
                {
                    _reversedPeers.Remove(peerId);

                    if (_pendingPeers.Remove(peerId))
                    {
                        Logger.WriteLog($"Pending peer {peerId} disconnected", LogImportance.INFO, "Network");
                        break;
                    }

                    var player = PlayerHandler.GetPlayerName(peerId);
                    Logger.WriteLog($"Player {player}:{peerId} disconnected ({reason})", LogImportance.INFO, "Network");
                    PlayerHandler.DisconnectPlayer(peerId, true);
                    
                    break;
                }
                
                Logger.WriteLog("Unknown Peer disconnected", LogImportance.INFO, "Network");
                break;
            }
        }
    }
    
    private void SendPackets()
    {
        while (_singleReceiverPackets.TryDequeue(out var toSend))
        {
            Packet packet = default;
            packet.Create(toSend.data, toSend.dataLength, (PacketFlags)toSend.deliveryMethod);
            if (_reversedPeers.TryGetValue(toSend.receiver, out var peer))
                peer.Send(NetworkHelper.GetChannel(toSend.deliveryMethod), ref packet);
        }

        while (_multiReceiverPackets.TryDequeue(out var toSend))
        {
            Packet packet = default;
            packet.Create(toSend.data, toSend.dataLength, (PacketFlags)toSend.deliveryMethod);
            foreach (var receiver in toSend.receivers)
                if (_reversedPeers.TryGetValue(receiver, out var peer))
                    peer.Send(NetworkHelper.GetChannel(toSend.deliveryMethod), ref packet);
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
    public void SendMessage(ushort[] receivers, byte[] data, int dataLength, DeliveryMethod deliveryMethod)
    {
        _multiReceiverPackets.Enqueue((receivers, data, dataLength, deliveryMethod));
    }

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="IMessage"/> interface messages
    /// </summary>
    public void SendMessage(ushort receiver, byte[] data, int dataLength, DeliveryMethod deliveryMethod)
    {
        _singleReceiverPackets.Enqueue((receiver, data, dataLength, deliveryMethod));
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
        peer.Disconnect((uint)DisconnectReasons.REJECT);
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
}