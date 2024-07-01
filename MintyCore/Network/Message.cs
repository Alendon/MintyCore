using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using JetBrains.Annotations;
using LiteNetLib;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Network;

/// <summary>
///     Interface for all messages.
/// </summary>
[PublicAPI]
public abstract class Message : MessageBase
{
    /// <summary>
    /// Id of the sender of this message. Might be a temporary id for pending clients
    /// </summary>
    public Player? Sender { get; set; }

    protected override DataWriter ConstructWriter(MagicHeader magic)
    {
        var writer = base.ConstructWriter(magic);
        writer.Put(MessageId);
        return writer;
    }

    /// <summary>
    /// Send this message to the server
    /// </summary>
    public void SendToServer(bool recycle)
    {
        using var writer = ConstructWriter(NetworkUtils.ConnectedMessageHeader);
        NetworkHandler.SendToServer(writer.ConstructBuffer(), DeliveryMethod);

        if (recycle) Recycle();
    }

    /// <summary>
    /// Send this message to the specified receivers
    /// </summary>
    public void Send(IEnumerable<Player> receivers, bool recycle)
    {
        using var writer = ConstructWriter(NetworkUtils.ConnectedMessageHeader);
        Serialize(writer);

        NetworkHandler.Send(receivers, writer.ConstructBuffer(), DeliveryMethod);

        if (recycle) Recycle();
    }

    /// <summary>
    /// Send this message to the specified receiver
    /// </summary>
    public void Send(Player receiver, bool recycle)
    {
        using var writer = ConstructWriter(NetworkUtils.ConnectedMessageHeader);
        Serialize(writer);

        NetworkHandler.Send(receiver, writer.ConstructBuffer(), DeliveryMethod);

        if (recycle) Recycle();
    }

    /// <summary>
    /// Send this message to the specified receivers
    /// </summary>
    public void Send(Player[] receivers, bool recycle)
    {
        using var writer = ConstructWriter(NetworkUtils.ConnectedMessageHeader);
        Serialize(writer);

        NetworkHandler.Send(receivers, writer.ConstructBuffer(), DeliveryMethod);

        if (recycle) Recycle();
    }
}