using System;
using System.Collections.Concurrent;
using System.Threading;
using ENet;
using MintyCore.Utils;

namespace MintyCore.Network;

/// <summary>
/// Client which connects to a server concurrently
/// </summary>
public class ConcurrentClient : IDisposable
{
    /// <summary>
    ///     Address of the server to connect to
    /// </summary>
    private readonly Address _address;

    /// <summary>
    ///     Callback to invoke when a message is received
    ///     <code>void OnReceive(ushort sender, DataReader data, bool isServer);</code>
    /// </summary>
    private readonly Action<ushort, DataReader, bool> _onReceiveCb;

    /// <summary>
    ///     Queue to store packages before sending
    /// </summary>
    private readonly ConcurrentQueue<(Packet packet, DeliveryMethod deliveryMethod)> _packets = new();

    /// <summary>
    ///     Queue to store message data which needs to be processed on the main thread
    /// </summary>
    private readonly ConcurrentQueue<DataReader> _receivedData = new();

    private Peer _connection;
    private volatile bool _hostShouldClose;
    private Thread? _networkThread;
    
    internal ConcurrentClient(Address target, Action<ushort, DataReader, bool> onReceiveCallback)
    {
        _address = target;
        _onReceiveCb = onReceiveCallback;
        Start();
    }
    
    /// <summary>
    /// Indicates if the client is connected to a server
    /// </summary>
    public bool IsConnected => _connection.IsSet && NetworkHelper.CheckConnected(_connection.State);

    /// <inheritdoc />
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

    /// <summary>
    ///     Worker method for the network thread
    /// </summary>
    private void Worker()
    {
        //Create a new host and connect to the server
        var host = new Host();
        host.Create();
        host.Connect(_address, Constants.ChannelCount);

        while (!_hostShouldClose)
        {
            //Send all queued packages
            while (_packets.TryDequeue(out var toSend))
            {
                if (_connection.IsSet)
                    _connection.Send(NetworkHelper.GetChannel(toSend.deliveryMethod), ref toSend.packet);
            }

            //Process all incoming events
            if (host.Service(1, out var @event) != 1) continue;
            do
            {
                HandleEvent(@event);
                @event.Packet.Dispose();
            } while (host.CheckEvents(out @event) == 1);
        }

        //Destroy the host when not longer needed to clean up
        host.Dispose();
    }

    private void HandleEvent(Event @event)
    {
        //Enet provides 3(/4) major event types to handle
        switch (@event.Type)
        {
            case EventType.Connect:
            {
                _connection = @event.Peer;
                Logger.WriteLog("Connected to server", LogImportance.INFO, "Network");
                break;
            }

            case EventType.Receive:
            {
                //create a reader for the received data.
                var reader = new DataReader(@event.Packet);
                
                if (!Logger.AssertAndLog(reader.TryGetBool(out var multiThreaded),
                        "Failed to get multi threaded indication", "Network", LogImportance.ERROR)) break;
                if (multiThreaded)
                    _onReceiveCb(Constants.ServerId, reader, false);
                else
                    _receivedData.Enqueue(reader);

                break;
            }

            case EventType.Timeout:
            case EventType.Disconnect:
            {
                var reason = @event.Type == EventType.Disconnect
                    ? (DisconnectReasons)@event.Data
                    : DisconnectReasons.TIME_OUT;
                Logger.WriteLog($"Disconnected from server ({reason})", LogImportance.INFO, "Network");
                //TODO implement proper disconnect logic
                _connection = default;
                Engine.ShouldStop = true;
                _hostShouldClose = true;
                break;
            }
        }
    }

    /// <summary>
    ///     Send a message to the server
    /// </summary>
    /// <param name="data">Byte array containing the data</param>
    /// <param name="dataLength">The length of the data to send</param>
    /// <param name="deliveryMethod">How to deliver the message</param>
    public void SendMessage(Span<byte> data, DeliveryMethod deliveryMethod)
    {
        Packet packet = default;
        packet.Create(data, (PacketFlags)deliveryMethod);
        _packets.Enqueue((packet, deliveryMethod));
    }

    /// <summary>
    ///     Update the client
    /// </summary>
    public void Update()
    {
        while (_receivedData.TryDequeue(out var reader)) _onReceiveCb(Constants.ServerId, reader, false);
    }
}