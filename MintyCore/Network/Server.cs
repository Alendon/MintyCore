using System.Collections.Generic;
using ENet;
using MintyCore.Utils;

namespace MintyCore.Network
{
    public class Server
    {
        private Host _server = new();
        private MessageHandler _messageHandler;
        
        private Dictionary<Peer, ushort> _clients = new();
        private Dictionary<ushort, Peer> _reversedClients = new();

        private HashSet<Peer> _pendingClients = new();


        public Server()
        {
            _messageHandler = new(this);
        }

        public void Start(ushort port, int maxConnections)
        {
            if (_server.IsSet)
            {
                Logger.WriteLog("Tried to start an already running server", LogImportance.EXCEPTION, "Network");
                return;
            }

            _server = new Host();
            Address address = new Address() { Port = port };
            _server.Create(address, maxConnections);
        }

        public void Update()
        {
            Event @event = default;

            if (_server.Service(10, out @event) == 1)
            {
                do
                {
                    HandleEvent(@event);
                } while (_server.CheckEvents(out @event) == 1);
            }
        }

        internal void SendMessage(ushort playerGameId, Packet packet, DeliveryMethod deliveryMethod)
        {
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
                        ushort playerId = _clients[@event.Peer];
                        _clients.Remove(@event.Peer);
                        _reversedClients.Remove(playerId);
                        MintyCore.RemovePlayer(playerId);
                    }

                    @event.Packet.Dispose();
                    break;
                }
                case EventType.Receive:
                {
                    DataReader reader = new DataReader(@event.Packet.Data, @event.Packet.Length);
                    OnReceive(@event.Peer, reader);
                    reader.DisposeKeepData();
                    @event.Packet.Dispose();

                    break;
                }

                case EventType.None: break;
            }
        }

        private void OnReceive(Peer peer, DataReader reader)
        {
            MessageType messageType = (MessageType)reader.GetInt();
            switch (messageType)
            {
                case MessageType.REGISTERED_MESSAGE:
                    _messageHandler.HandleMessage(reader);
                    break;
                case MessageType.CONNECTION_SETUP:
                {
                    ConnectionSetupMessageType connectionMessage = (ConnectionSetupMessageType)reader.GetInt();
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
            
            if (!MintyCore.AddPlayer(info.PlayerName, info.PlayerId, out var id))
            {
                peer.Disconnect((uint)DisconnectReasons.REJECT);
                return;
            }

            PlayerConnected message = new() { PlayerGameId = id };
            
            _clients.Add(peer, id);
            _reversedClients.Add(id, peer);

            DataWriter writer = default;
            writer.Initialize();
            writer.Put((int)MessageType.CONNECTION_SETUP);
            writer.Put((int)ConnectionSetupMessageType.PLAYER_CONNECTED);
            message.Serialize(writer);

            Packet packet = default;
            packet.Create(writer.Data, writer.Length, (PacketFlags)DeliveryMethod.Reliable);
            peer.Send(NetworkHelper.GetChannel(DeliveryMethod.Reliable), ref packet);
            writer.Dispose();

            MintyCore.SpawnPlayer(id);
        }
    }
}