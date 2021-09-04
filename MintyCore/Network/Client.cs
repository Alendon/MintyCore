using System;
using ENet;
using MintyCore.Utils;

namespace MintyCore.Network
{
    public class Client
    {
        private Host _client = new();
        private Peer _serverConnection = default;

        private MessageHandler _messageHandler;

        public Client()
        {
            _messageHandler = new(this);
        }
        

        public void Connect(Address address)
        {
            if (_serverConnection.IsSet && NetworkHelper.CheckConnected(_serverConnection.State))
            {
                Logger.WriteLog("Already connected to a server", LogImportance.EXCEPTION, "Network");
            }
            _client.Dispose();
            
            _client = new Host();
            _client.Create();
            _client.Connect(address, Constants.ChannelCount);
        }

        public bool Connected => _serverConnection.State == PeerState.Connected;

        public void Update()
        {
            Event @event = default;

            if (Connected && _client.Service(10, out @event) == 1)
            {
                do
                {
                    HandleEvent(@event);
                } while (_client.CheckEvents(out @event) == 1);
            }
        }

        internal void SendMessage(Packet packet, DeliveryMethod deliveryMethod)
        {
            _serverConnection.Send(NetworkHelper.GetChannel(deliveryMethod), ref packet);
        }

        private void HandleEvent(Event @event)
        {
            switch (@event.Type)
            {
                case EventType.Connect:
                {
                    Logger.WriteLog("Connected to server", LogImportance.INFO, "Network");
                    _serverConnection = @event.Peer;
                    @event.Packet.Dispose();

                    SendPlayerInformation();

                    break;
                }
                
                case EventType.Timeout:
                case EventType.Disconnect:
                {
                    Logger.WriteLog("Disconnected from server", LogImportance.INFO, "Network");
                    _serverConnection = default;
                    @event.Packet.Dispose();
                    break;
                }
                case EventType.Receive:
                {
                    var reader = new DataReader(@event.Packet.Data, @event.Packet.Length);
                    OnReceive(reader);
                    reader.DisposeKeepData();
                    @event.Packet.Dispose();
                    
                    break;
                }

                case EventType.None: break;
            }
        }

        private void OnReceive(DataReader reader)
        {
            MessageType messageType = (MessageType)reader.GetInt();
            switch (messageType)
            {
                case MessageType.REGISTERED_MESSAGE: _messageHandler.HandleMessage(reader); break;
                case MessageType.CONNECTION_SETUP:
                {
                    ConnectionSetupMessageType connectionMessage = (ConnectionSetupMessageType)reader.GetInt();
                    switch (connectionMessage)
                    {
                        case ConnectionSetupMessageType.PLAYER_CONNECTED: OnPlayerConnectedMessage(reader); break;
                        case ConnectionSetupMessageType.INVALID:
                        case ConnectionSetupMessageType.PLAYER_INFORMATION: 
                            Logger.WriteLog("Unexpected connection setup message received", LogImportance.WARNING, "Network"); break;
                        default:
                            Logger.WriteLog("Unexpected connection setup message received", LogImportance.WARNING, "Network"); break;
                    }
                    break;
                }
                case MessageType.ENGINE_MESSAGE: break;
            }
        }

        private void OnPlayerConnectedMessage(DataReader reader)
        {
            PlayerConnected message = default;
            message.Deserialize(reader);

            MintyCore.LocalPlayerGameId = message.PlayerGameId;

            MintyCore.CreatePlayerWorld();
        }

        private unsafe void SendPlayerInformation()
        {
            DataWriter writer = new DataWriter();
            writer.Initialize();

            PlayerInformation info = new() { PlayerId = 10001, PlayerName = "Test" };

            writer.Put((int)MessageType.CONNECTION_SETUP);
            writer.Put((int)ConnectionSetupMessageType.PLAYER_INFORMATION);
            info.Serialize(writer);

            Packet packet = new Packet();
            packet.Create(new IntPtr(writer.OriginBytePointer), writer.Length, PacketFlags.Reliable);

            _serverConnection.Send(NetworkHelper.GetChannel(DeliveryMethod.Reliable), ref packet);
            writer.Dispose();
        }
    }
}