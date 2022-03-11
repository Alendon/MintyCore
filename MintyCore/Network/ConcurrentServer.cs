using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ENet;
using MintyCore.Modding;
using MintyCore.Network.Messages;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network;

internal class ConcurrentServer : IDisposable
{
    private readonly Address _address;

    private readonly int _maxActiveConnections;

    private readonly
        ConcurrentQueue<(ushort[] receivers, byte[] data, int dataLength, DeliveryMethod deliveryMethod)>
        _multiReceiverPackets = new();

    private readonly Action<ushort, DataReader, bool> _onReceiveCb;

    private readonly Dictionary<Peer, ushort> _peersWithId = new();

    private readonly Dictionary<Peer, DateTime> _pendingPeers = new();

    private readonly ConcurrentQueue<(ushort sender, DataReader data)> _receivedData = new();
    private readonly Dictionary<ushort, Peer> _reversedPeers = new();

    private readonly
        ConcurrentQueue<(ushort receiver, byte[] data, int dataLength, DeliveryMethod deliveryMethod)>
        _singleReceiverPackets = new();

    private volatile bool _hostShouldClose;
    private Thread? _networkThread;

    public ConcurrentServer(ushort port, int maxConnections,
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

                Logger.AssertAndThrow(reader.TryGetInt(out var messageType), "Failed to get message type", "Network");
                switch ((MessageType)messageType)
                {
                    case MessageType.REGISTERED_MESSAGE:
                    {
                        if (!_peersWithId.TryGetValue(@event.Peer, out var id)) break;

                        Logger.AssertAndThrow(reader.TryGetBool(out var multiThreaded),
                            "Failed to get multi threaded indication", "Network");
                        if (multiThreaded)
                        {
                            _onReceiveCb(id, reader, true);
                            break;
                        }

                        _receivedData.Enqueue((id, reader));
                        break;
                    }
                    //TODO with the introduction of root mods. Change this to properly registered messages
                    case MessageType.CONNECTION_SETUP:
                    {
                        HandleConnectionSetup(reader, @event.Peer);
                        break;
                    }
                }

                break;
            }
            case EventType.Connect:
            {
                Logger.WriteLog("New peer connected", LogImportance.INFO, "Network");

                _pendingPeers.Add(@event.Peer, DateTime.Now);
                break;
            }
            case EventType.Timeout:
            case EventType.Disconnect:
            {
                var reason = (DisconnectReasons)@event.Data;
                if (_peersWithId.TryGetValue(@event.Peer, out var gameId))
                {
                    Logger.WriteLog($"Client {gameId} disconnected ({reason})", LogImportance.INFO, "Network");
                    PlayerHandler.DisconnectPlayer(gameId, true);

                    _peersWithId.Remove(@event.Peer);
                    _reversedPeers.Remove(gameId);
                    break;
                }

                if (_pendingPeers.TryGetValue(@event.Peer, out var tempId))
                {
                    Logger.WriteLog($"Peer {tempId} disconnected ({reason})", LogImportance.INFO, "Network");
                    _pendingPeers.Remove(@event.Peer);
                    break;
                }

                Logger.WriteLog("Unknown Peer disconnected", LogImportance.INFO, "Network");

                break;
            }
        }
    }

    private void HandleConnectionSetup(DataReader reader, Peer peer)
    {
        Logger.AssertAndThrow(reader.TryGetInt(out var messageType), "Failed to get connection setup type", "Network");

        if ((ConnectionSetupMessageType)messageType != ConnectionSetupMessageType.PLAYER_INFORMATION ||
            !_pendingPeers.ContainsKey(peer)) return;

        PlayerInformation information = default;
        information.Deserialize(reader);
        _pendingPeers.Remove(peer);

        if (!ModManager.ModsCompatible(information.AvailableMods ?? throw new NullReferenceException()) ||
            !PlayerHandler.AddPlayer(information.PlayerName ?? throw new NullReferenceException(), information.PlayerId,
                out var id, true))
        {
            peer.DisconnectNow((uint)DisconnectReasons.REJECT);
            return;
        }

        _peersWithId.Add(peer, id);
        _reversedPeers.Add(id, peer);

        #region loadMods

        {
            LoadMods loadModsMessage = new()
            {
                Mods = from info in ModManager.GetLoadedMods() select (info.modId, info.modVersion),
                CategoryIDs = RegistryManager.GetCategoryIDs(),
                ModIDs = RegistryManager.GetModIDs(),
                ObjectIDs = RegistryManager.GetObjectIDs()
            };

            DataWriter writer = new();
            writer.Put((int)MessageType.CONNECTION_SETUP);
            writer.Put((int)ConnectionSetupMessageType.LOAD_MODS);
            loadModsMessage.Serialize(writer);

            Packet loadModsPacket = default;
            loadModsPacket.Create(writer.ConstructBuffer(), writer.Length, PacketFlags.Reliable);
            peer.Send(NetworkHelper.GetChannel(DeliveryMethod.RELIABLE), ref loadModsPacket);
        }

        #endregion

        #region playerConnected

        {
            PlayerConnected playerConnectedMessage = new()
            {
                PlayerGameId = id
            };

            DataWriter writer = new();
            writer.Put((int)MessageType.CONNECTION_SETUP);
            writer.Put((int)ConnectionSetupMessageType.PLAYER_CONNECTED);
            playerConnectedMessage.Serialize(writer);

            Packet playerConnectedPacket = default;
            playerConnectedPacket.Create(writer.ConstructBuffer(), writer.Length, PacketFlags.Reliable);
            peer.Send(NetworkHelper.GetChannel(DeliveryMethod.RELIABLE), ref playerConnectedPacket);
        }

        #endregion

        if (Engine.ServerWorld is not null)
            foreach (var entity in Engine.ServerWorld.EntityManager.Entities)
            {
                SendEntityData sendEntity = new()
                {
                    Entity = entity,
                    EntityOwner = Engine.ServerWorld.EntityManager.GetEntityOwner(entity)
                };
                sendEntity.Send(id);
            }

        Logger.WriteLog($"Player {information.PlayerName} with id: '{information.PlayerId}' joined the game",
            LogImportance.INFO,
            "Network");


        SyncPlayers syncPlayers = new()
        {
            Players = (from playerId in PlayerHandler.GetConnectedPlayers()
                where playerId != id
                select (playerId, PlayerHandler.GetPlayerName(playerId), PlayerHandler.GetPlayerId(playerId))).ToArray()
        };
        syncPlayers.Send(id);

        PlayerJoined playerJoined = new()
        {
            GameId = id,
            PlayerId = information.PlayerId,
            PlayerName = information.PlayerName
        };
        playerJoined.Send(PlayerHandler.GetConnectedPlayers());
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

    public void Update()
    {
        while (_receivedData.TryDequeue(out var readerSender))
            _onReceiveCb(readerSender.sender, readerSender.data, true);
    }

    public void SendMessage(ushort[] receivers, byte[] data, int dataLength, DeliveryMethod deliveryMethod)
    {
        _multiReceiverPackets.Enqueue((receivers, data, dataLength, deliveryMethod));
    }

    public void SendMessage(ushort receiver, byte[] data, int dataLength, DeliveryMethod deliveryMethod)
    {
        _singleReceiverPackets.Enqueue((receiver, data, dataLength, deliveryMethod));
    }
}