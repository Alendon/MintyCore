using System.Collections.Generic;
using ENet;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Network.Messages;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network
{
    public class Server
    {
        private Host _server = new();
        public readonly MessageHandler MessageHandler;

        private readonly Dictionary<Peer, ushort> _clients = new();
        private readonly Dictionary<ushort, Peer> _reversedClients = new();

        private readonly HashSet<Peer> _pendingClients = new();


        public Server()
        {
            MessageHandler = new MessageHandler(this);
        }

        public void Start(ushort port, int maxConnections)
        {
            if (_server.IsSet)
            {
                Logger.WriteLog("Tried to start an already running server", LogImportance.EXCEPTION, "Network");
                return;
            }

            _server = new Host();
            var address = new Address { Port = port };
            _server.Create(address, maxConnections);
        }

        public void Update()
        {
            Event @event = default;

            if (_server.Service(1, out @event) != 1) return;
            do
            {
                HandleEvent(@event);
            } while (_server.CheckEvents(out @event) == 1);
        }

        internal void SendMessage(ushort playerGameId, Packet packet, DeliveryMethod deliveryMethod)
        {
            var peer = _reversedClients[playerGameId];
            if (NetworkHelper.CheckConnected(peer.State))
                _reversedClients[playerGameId].Send(NetworkHelper.GetChannel(deliveryMethod), ref packet);
        }

        private void HandleEvent(Event @event)
        {
            switch (@event.Type)
            {
                case EventType.Connect:
                {
                    Logger.WriteLog("Connected to server", LogImportance.INFO, "Network");
                    _pendingClients.Add(@event.Peer);
                    @event.Packet.Dispose();
                    break;
                }

                case EventType.Timeout:
                case EventType.Disconnect:
                {
                    Logger.WriteLog("Disconnected from server", LogImportance.INFO, "Network");

                    if (_clients.ContainsKey(@event.Peer))
                    {
                        var playerId = _clients[@event.Peer];
                        Engine.RaiseOnPlayerDisconnected(playerId, true);
                        _clients.Remove(@event.Peer);
                        _reversedClients.Remove(playerId);
                        Engine.RemovePlayerEntities(playerId);
                        Engine.RemovePlayer(playerId);
                        MessageHandler.SendMessage(MessageIDs.PlayerLeft, new PlayerLeft.Data(playerId));
                    }

                    @event.Packet.Dispose();
                    break;
                }
                case EventType.Receive:
                {
                    var reader = new DataReader(@event.Packet.Data, @event.Packet.Length);
                    OnReceive(@event.Peer, reader);
                    @event.Packet.Dispose();

                    break;
                }

                case EventType.None: break;
            }
        }

        private void OnReceive(Peer peer, DataReader reader)
        {
            var messageType = (MessageType)reader.GetInt();
            switch (messageType)
            {
                case MessageType.REGISTERED_MESSAGE:
                    MessageHandler.HandleMessage(reader);
                    break;
                case MessageType.CONNECTION_SETUP:
                {
                    var connectionMessage = (ConnectionSetupMessageType)reader.GetInt();
                    switch (connectionMessage)
                    {
                        case ConnectionSetupMessageType.PLAYER_INFORMATION:
                            OnPlayerInformationMessage(peer, reader);
                            break;
                        case ConnectionSetupMessageType.INVALID:
                        case ConnectionSetupMessageType.PLAYER_CONNECTED:
                            Logger.WriteLog("Unexpected connection setup message received", LogImportance.WARNING,
                                "Network");
                            break;
                        default:
                            Logger.WriteLog("Unexpected connection setup message received", LogImportance.WARNING,
                                "Network");
                            break;
                    }

                    break;
                }
                case MessageType.ENGINE_MESSAGE: break;
            }
        }

        private void OnPlayerInformationMessage(Peer peer, DataReader reader)
        {
            PlayerInformation info = default;
            info.Deserialize(reader);

            _pendingClients.Remove(peer);

            if (!ModManager.ModsCompatible(info.AvailableMods) ||
                !Engine.AddPlayer(info.PlayerName, info.PlayerId, out var id))
            {
                peer.Disconnect((uint)DisconnectReasons.REJECT);
                return;
            }

            LoadMods loadModsMessage = new()
            {
                Mods = ModManager.GetLoadedMods(), ModIDs = RegistryManager.GetModIDs(),
                CategoryIDs = RegistryManager.GetCategoryIDs(), ObjectIDs = RegistryManager.GetObjectIDs()
            };
            
            DataWriter writer = new DataWriter();
            writer.Put((int)MessageType.CONNECTION_SETUP);
            writer.Put((int)ConnectionSetupMessageType.LOAD_MODS);
            loadModsMessage.Serialize(writer);
            
            Packet loadModsPacket = default;
            loadModsPacket.Create(writer.Buffer, (PacketFlags)DeliveryMethod.RELIABLE);
            peer.Send(NetworkHelper.GetChannel(DeliveryMethod.RELIABLE), ref loadModsPacket);

            PlayerConnected message = new() { PlayerGameId = id };
            _clients.Add(peer, id);
            _reversedClients.Add(id, peer);

            writer.Reset();
            writer.Put((int)MessageType.CONNECTION_SETUP);
            writer.Put((int)ConnectionSetupMessageType.PLAYER_CONNECTED);
            message.Serialize(writer);

            Packet packet = default;
            packet.Create(writer.Buffer, (PacketFlags)DeliveryMethod.RELIABLE);
            peer.Send(NetworkHelper.GetChannel(DeliveryMethod.RELIABLE), ref packet);

            if (Engine.ServerWorld is not null)
                foreach (var entity in Engine.ServerWorld.EntityManager.Entities)
                {
                    var data = new SendEntityData.Data
                    {
                        ToSend = entity, PlayerId = id,
                        EntityOwner = Engine.ServerWorld.EntityManager.GetEntityOwner(entity)
                    };

                    MessageHandler.SendMessage(MessageIDs.SendEntityData, data);
                }

            Logger.WriteLog($"Player {info.PlayerName} with id: '{info.PlayerId}' joined the game", LogImportance.INFO,
                "Network");

            List<(ushort playerGameId, string playerName, ulong playerId)> playersToSync = new();

            foreach (var (playerId, name) in Engine.PlayerNames)
            {
                if (playerId == id) continue;

                var gameId = Engine.PlayerIDs[playerId];
                playersToSync.Add((playerId, name, gameId));
            }

            var syncData = new SyncPlayers.PlayerData(playersToSync.ToArray(), id);

            MessageHandler.SendMessage(MessageIDs.SyncPlayers, syncData);

            MessageHandler.SendMessage(MessageIDs.PlayerJoined,
                new PlayerJoined.PlayerData(id, info.PlayerName, info.PlayerId));
            Engine.RaiseOnPlayerConnected(id, true);
        }

        public void Stop()
        {
            foreach (var peer in _clients.Keys)
            {
                peer.DisconnectNow((uint)DisconnectReasons.SERVER_CLOSING);
            }

            _clients.Clear();
            _reversedClients.Clear();

            foreach (var peer in _pendingClients)
            {
                peer.DisconnectNow((uint)DisconnectReasons.SERVER_CLOSING);
            }

            _pendingClients.Clear();

            _server.Dispose();
        }
    }
}