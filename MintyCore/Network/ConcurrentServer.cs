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

namespace MintyCore.Network
{
    internal class ConcurrentServer : IDisposable
    {
        private Thread? _networkThread;
        private volatile bool _hostShouldClose;
        private readonly Address _address;

        private readonly int _maxActiveConnections;

        private readonly Dictionary<Peer, ushort> _peersWithId = new();
        private readonly Dictionary<ushort, Peer> _reversedPeers = new();

        private readonly Dictionary<Peer, DateTime> _pendingPeers = new();

        private readonly
            ConcurrentQueue<(ushort[] receivers, byte[] data, int dataLength, DeliveryMethod deliveryMethod)>
            _multiReceiverPackets = new();

        private readonly
            ConcurrentQueue<(ushort receiver, byte[] data, int dataLength, DeliveryMethod deliveryMethod)>
            _singleReceiverPackets = new();

        private readonly ConcurrentQueue<(ushort sender, DataReader data)> _receivedData = new();
        private readonly Action<ushort, DataReader, bool> _onReceiveCb;

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

        private void Start()
        {
            _networkThread = new Thread(Worker);
            _networkThread.Name = "Server Network Thread";
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

                    var messageType = (MessageType)reader.GetInt();
                    switch (messageType)
                    {
                        case MessageType.REGISTERED_MESSAGE:
                        {
                            if (!_peersWithId.TryGetValue(@event.Peer, out var id)) break;

                            var multiThreaded = reader.GetBool();
                            if (multiThreaded)
                            {
                                _onReceiveCb(id, reader, true);
                                break;
                            }

                            _receivedData.Enqueue((id, reader));
                            break;
                        }
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
                        Engine.DisconnectPlayer(gameId, true);

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
            var messageType = (ConnectionSetupMessageType)reader.GetInt();

            if (messageType != ConnectionSetupMessageType.PLAYER_INFORMATION) return;

            PlayerInformation information = default;
            information.Deserialize(reader);
            _pendingPeers.Remove(peer);

            if (!ModManager.ModsCompatible(information.AvailableMods) ||
                !Engine.AddPlayer(information.PlayerName, information.PlayerId, out var id, true))
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
                    Mods = ModManager.GetLoadedMods(),
                    CategoryIDs = RegistryManager.GetCategoryIDs(),
                    ModIDs = RegistryManager.GetModIDs(),
                    ObjectIDs = RegistryManager.GetObjectIDs()
                };

                DataWriter writer = new();
                writer.Put((int)MessageType.CONNECTION_SETUP);
                writer.Put((int)ConnectionSetupMessageType.LOAD_MODS);
                loadModsMessage.Serialize(writer);

                Packet loadModsPacket = default;
                loadModsPacket.Create(writer.Buffer, writer.Length, PacketFlags.Reliable);
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
                playerConnectedPacket.Create(writer.Buffer, writer.Length, PacketFlags.Reliable);
                peer.Send(NetworkHelper.GetChannel(DeliveryMethod.RELIABLE), ref playerConnectedPacket);
            }
            #endregion

            if (Engine.ServerWorld is not null)
            {
                foreach (var entity in Engine.ServerWorld.EntityManager.Entities)
                {
                    SendEntityData sendEntity = new()
                    {
                        Entity = entity,
                        EntityOwner = Engine.ServerWorld.EntityManager.GetEntityOwner(entity)
                    };
                    sendEntity.Send(id);
                }
            }

            Logger.WriteLog($"Player {information.PlayerName} with id: '{information.PlayerId}' joined the game", LogImportance.INFO,
                "Network");
            
            

            List<(ushort playerGameId, string playerName, ulong playerId)> playersToSync = new();

            foreach (var playerId in Engine.GetConnectedPlayers())
            {
                if (playerId == id) continue;
                playersToSync.Add((playerId, Engine.GetPlayerName(playerId), Engine.GetPlayerId(playerId)));
            }

            SyncPlayers syncPlayers = new()
            {
                Players = playersToSync.ToArray()
            };
            syncPlayers.Send(id);

            PlayerJoined playerJoined = new()
            {
                GameId = id,
                PlayerId = information.PlayerId,
                PlayerName = information.PlayerName
            };
            playerJoined.Send(Engine.GetConnectedPlayers());
        }

        private void SendPackets()
        {
            foreach (var (receiver, data, length, deliveryMethod) in _singleReceiverPackets)
            {
                Packet packet = default;
                packet.Create(data, length, (PacketFlags)deliveryMethod);
                if (_reversedPeers.TryGetValue(receiver, out var peer))
                    peer.Send(NetworkHelper.GetChannel(deliveryMethod), ref packet);
            }

            foreach (var (receivers, data, length, deliveryMethod) in _multiReceiverPackets)
            {
                Packet packet = default;
                packet.Create(data, length, (PacketFlags)deliveryMethod);
                foreach (var receiver in receivers)
                {
                    if (_reversedPeers.TryGetValue(receiver, out var peer))
                        peer.Send(NetworkHelper.GetChannel(deliveryMethod), ref packet);
                }
            }
        }

        private void Disconnect(ushort id, DisconnectReasons disconnectReason)
        {
            if (!_reversedPeers.TryGetValue(id, out var peer)) return;

            _reversedPeers.Remove(id);
            _peersWithId.Remove(peer);

            peer.Disconnect((uint)disconnectReason);
            Engine.DisconnectPlayer(id, true);
        }

        public void Update()
        {
            while (_receivedData.TryDequeue(out var readerSender))
            {
                _onReceiveCb(readerSender.sender, readerSender.data, true);
            }
        }

        public void SendMessage(ushort[] receivers, byte[] data, int dataLength, DeliveryMethod deliveryMethod)
        {
            _multiReceiverPackets.Enqueue((receivers, data, dataLength, deliveryMethod));
        }

        public void SendMessage(ushort receiver, byte[] data, int dataLength, DeliveryMethod deliveryMethod)
        {
            _singleReceiverPackets.Enqueue((receiver, data, dataLength, deliveryMethod));
        }


        public void Dispose()
        {
            _hostShouldClose = true;
            _networkThread?.Join();
        }
    }
}