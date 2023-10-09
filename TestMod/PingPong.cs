using JetBrains.Annotations;
using MintyCore;
using MintyCore.Network;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Input;

namespace TestMod;

[RegisterMessage("ping_pong")]
public partial class PingPong : IMessage
{
    public required IPlayerHandler PlayerHandler {[UsedImplicitly] init; private get; }
    
    public bool IsServer { get; set; }
    public bool ReceiveMultiThreaded => true;
    public Identification MessageId => Identifications.MessageIDs.PingPong;
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;
    public ushort Sender { get; set; }
    public void Serialize(DataWriter writer)
    {
        writer.Put(DateTime.UtcNow.Ticks);
    }

    public bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetLong(out var ticks)) return false;
        var send = new DateTime(ticks);
        var receive = DateTime.UtcNow;
        var delay = receive - send;
        
        Logger.WriteLog($"Received ping from {PlayerHandler.GetPlayer(Sender).Name} with {delay.TotalMilliseconds}ms delay", LogImportance.Info, "TestMod");
        return true;
    }

    public void Clear()
    {
        //Nothing to do here
    }

    [RegisterKeyAction("ping_pong")]
    public static KeyActionInfo GetPingPongKeyActionInfo(INetworkHandler networkHandler) =>
        new()
        {
            Action = (keyState, _) =>
            {
                if (keyState != KeyStatus.KeyDown) return;

                Logger.WriteLog("Sending ping", LogImportance.Info, "TestMod");
                var message = networkHandler.CreateMessage<PingPong>();
                message.SendToServer();
            },
            Key = Key.P
        };
}