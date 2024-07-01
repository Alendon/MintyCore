﻿using JetBrains.Annotations;
using LiteNetLib;
using MintyCore;
using MintyCore.Input;
using MintyCore.Network;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;
using Silk.NET.GLFW;

namespace TestMod;

[RegisterMessage("ping_pong")]
public partial class PingPong : Message
{
    public required IPlayerHandler PlayerHandler { [UsedImplicitly] init; private get; }

    public override bool ReceiveMultiThreaded => true;
    public override Identification MessageId => Identifications.MessageIDs.PingPong;
    public override DeliveryMethod DeliveryMethod => DeliveryMethod.ReliableOrdered;

    public override void Serialize(DataWriter writer)
    {
        writer.Put(DateTime.UtcNow.Ticks);
    }

    public override bool Deserialize(DataReader reader)
    {
        if (!reader.TryGetLong(out var ticks)) return false;
        var send = new DateTime(ticks);
        var receive = DateTime.UtcNow;
        var delay = receive - send;

        Log.Information("Received ping from {PlayerName} with {DelayInMs}ms delay",
            (Sender)?.Name, delay.TotalMilliseconds);
        return true;
    }

    public override void Clear()
    {
        //Nothing to do here
    }

    [RegisterInputAction("ping_pong")]
    public static InputActionDescription GetPingPongKeyActionInfo(INetworkHandler networkHandler) =>
        new()
        {
            ActionCallback = (parameters) =>
            {
                if (parameters.InputAction != InputAction.Press) return InputActionResult.Stop;

                Log.Information("Sending ping");
                var message = networkHandler.CreateMessage<PingPong>();
                message.SendToServer();

                return InputActionResult.Stop;
            },
            DefaultInput = Keys.P
        };
}