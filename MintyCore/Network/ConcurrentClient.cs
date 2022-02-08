using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using ENet;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network;

internal class ConcurrentClient : IDisposable
{
    private readonly Address _address;
    private readonly Action<ushort, DataReader, bool> _onReceiveCb;

    private readonly ConcurrentQueue<(byte[] data, int dataLength, DeliveryMethod deliveryMethod)> _packets = new();
    private readonly ConcurrentQueue<DataReader> _receivedData = new();

    private Peer _connection;
    private volatile bool _hostShouldClose;
    private Thread? _networkThread;

    public ConcurrentClient(Address target, Action<ushort, DataReader, bool> onReceiveCallback)
    {
        _address = target;
        _onReceiveCb = onReceiveCallback;
        Start();
    }

    public bool IsConnected => _connection.IsSet && NetworkHelper.CheckConnected(_connection.State);

    public void Dispose()
    {
        _hostShouldClose = true;
        _networkThread?.Join();
    }

    private void Start()
    {
        var start = new ThreadStart(Worker);
        _networkThread = new Thread(start);
        _networkThread.Start();
        _networkThread.Name = "Client Network Thread";
    }

    private void Worker()
    {
        var host = new Host();
        host.Create();
        host.Connect(_address, Constants.ChannelCount);

        while (!_hostShouldClose)
        {
            while (_packets.TryDequeue(out var toSend))
            {
                var packet = new Packet();
                packet.Create(toSend.data, toSend.dataLength, (PacketFlags)toSend.deliveryMethod);
                if (_connection.IsSet)
                    _connection.Send(NetworkHelper.GetChannel(toSend.deliveryMethod), ref packet);
            }

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
            case EventType.Connect:
            {
                _connection = @event.Peer;
                Logger.WriteLog("Connected to server", LogImportance.INFO, "Network");
                SendPlayerInformation();
                break;
            }

            case EventType.Receive:
            {
                var reader = new DataReader(@event.Packet);
                var messageType = (MessageType)reader.GetInt();
                switch (messageType)
                {
                    case MessageType.CONNECTION_SETUP:
                    {
                        HandleConnectionSetup(reader);
                        break;
                    }
                    case MessageType.REGISTERED_MESSAGE:
                    {
                        var multiThreaded = reader.GetBool();
                        if (multiThreaded)
                            _onReceiveCb(Constants.ServerId, reader, false);
                        else
                            _receivedData.Enqueue(reader);

                        break;
                    }
                }

                break;
            }

            case EventType.Timeout:
            case EventType.Disconnect:
            {
                var reason = (DisconnectReasons)@event.Data;
                Logger.WriteLog($"Disconnected from server ({reason})", LogImportance.INFO, "Network");
                _connection = default;
                Engine.ShouldStop = true;
                _hostShouldClose = true;
                break;
            }
        }
    }

    private void SendPlayerInformation()
    {
        var writer = new DataWriter();

        var availableMods = from mods in ModManager.GetAvailableMods()
            select (mods.ModId, mods.ModVersion);

        PlayerInformation info = new()
        {
            PlayerId = PlayerHandler.LocalPlayerId, PlayerName = PlayerHandler.LocalPlayerName,
            AvailableMods = availableMods
        };

        writer.Put((int)MessageType.CONNECTION_SETUP);
        writer.Put((int)ConnectionSetupMessageType.PLAYER_INFORMATION);
        info.Serialize(writer);

        _packets.Enqueue((writer.Buffer, writer.Length, DeliveryMethod.RELIABLE));
    }

    private void HandleConnectionSetup(DataReader reader)
    {
        var connectionSetupType = (ConnectionSetupMessageType)reader.GetInt();
        switch (connectionSetupType)
        {
            case ConnectionSetupMessageType.LOAD_MODS:
            {
                LoadMods loadMods = default;
                loadMods.Deserialize(reader);

                if (Engine.GameType != GameType.CLIENT) break;

                RegistryManager.SetModIDs(loadMods.ModIDs);
                RegistryManager.SetCategoryIDs(loadMods.CategoryIDs);
                RegistryManager.SetObjectIDs(loadMods.ObjectIDs);

                var modInfosToLoad =
                    from modInfos in ModManager.GetAvailableMods()
                    from modsToLoad in loadMods.Mods
                    where modInfos.ModId.Equals(modsToLoad.modId) &&
                          modInfos.ModVersion.Compatible(modsToLoad.modVersion)
                    select modInfos;

                ModManager.LoadGameMods(modInfosToLoad);
                break;
            }
            case ConnectionSetupMessageType.PLAYER_CONNECTED:
            {
                PlayerConnected message = default;
                message.Deserialize(reader);

                PlayerHandler.LocalPlayerGameId = message.PlayerGameId;

                Engine.CreateClientWorld();
                break;
            }
        }
    }

    public void SendMessage(byte[] data, int dataLength, DeliveryMethod deliveryMethod)
    {
        _packets.Enqueue((data, dataLength, deliveryMethod));
    }

    public void Update()
    {
        while (_receivedData.TryDequeue(out var reader)) _onReceiveCb(Constants.ServerId, reader, false);
    }
}