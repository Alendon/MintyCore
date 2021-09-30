using System;
using System.Collections.Generic;
using System.Linq;
using ENet;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network
{
    public class Client
    {
        private Host _client = new();
        private Peer _serverConnection;

        public readonly MessageHandler MessageHandler;

        public Client()
        {
            MessageHandler = new(this);
        }

        public void Connect(string targetAddress, ushort port)
        {
            Address address = default;
            address.SetHost(targetAddress);
            address.Port = port;
            Connect(address);
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

            if (_client.Service(10, out @event) == 1)
            {
                do
                {
                    HandleEvent(@event);
                } while (_client.CheckEvents(out @event) == 1);
            }
        }

        internal void SendMessage(Packet packet, DeliveryMethod deliveryMethod)
        {
            if (NetworkHelper.CheckConnected(_serverConnection.State))
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
                    MintyCore.ShouldStop = true;
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
                case MessageType.REGISTERED_MESSAGE:
                    MessageHandler.HandleMessage(reader);
                    break;
                case MessageType.CONNECTION_SETUP:
                {
                    ConnectionSetupMessageType connectionMessage = (ConnectionSetupMessageType)reader.GetInt();
                    switch (connectionMessage)
                    {
                        case ConnectionSetupMessageType.PLAYER_CONNECTED:
                            OnPlayerConnectedMessage(reader);
                            break;
                        case ConnectionSetupMessageType.LOAD_MODS:
                        {
                            OnLoadMods(reader);
                            break;
                        }
                        
                        case ConnectionSetupMessageType.INVALID:
                        case ConnectionSetupMessageType.PLAYER_INFORMATION:
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

        private void OnLoadMods(DataReader reader)
        {
            LoadMods loadMods = default;
            loadMods.Deserialize(reader);

            if (MintyCore.GameType == GameType.LOCAL) return;
            
            RegistryManager.SetModIDs(loadMods.ModIDs);
            RegistryManager.SetCategoryIDs(loadMods.CategoryIDs);
            RegistryManager.SetObjectIDs(loadMods.ObjectIDs);

            var modInfosToLoad = 
                from modInfos in ModManager.GetAvailableMods()
                from modsToLoad in loadMods.Mods
                where modInfos.ModId.Equals(modsToLoad.modId) && modInfos.ModVersion.Compatible(modsToLoad.modVersion) 
                select modInfos;
            
            ModManager.LoadMods(modInfosToLoad);
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

            var availableMods = from mods in ModManager.GetAvailableMods()
                select (mods.ModId, mods.ModVersion);

            PlayerInformation info = new()
            {
                PlayerId = MintyCore.LocalPlayerId, PlayerName = MintyCore.LocalPlayerName,
                AvailableMods = availableMods
            };

            writer.Put((int)MessageType.CONNECTION_SETUP);
            writer.Put((int)ConnectionSetupMessageType.PLAYER_INFORMATION);
            info.Serialize(writer);

            Packet packet = new Packet();
            packet.Create(new IntPtr(writer.OriginBytePointer), writer.Length, PacketFlags.Reliable);

            _serverConnection.Send(NetworkHelper.GetChannel(DeliveryMethod.Reliable), ref packet);
            writer.Dispose();
        }

        public void Disconnect()
        {
            if (Connected)
                _serverConnection.DisconnectNow((uint)DisconnectReasons.PLAYER_DISCONNECT);
            _client.Dispose();
        }
    }
}