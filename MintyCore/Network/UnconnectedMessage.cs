using System;
using System.Collections.Generic;
using LiteNetLib;
using MintyCore.Modding;
using MintyCore.Utils;

namespace MintyCore.Network;

public abstract class UnconnectedMessage : MessageBase
{
    /// <summary>
    /// Temporary id of the sender
    /// </summary>
    public int Sender { get; set; }
    
    /// <summary>
    /// 8 byte magic sequence
    /// </summary>
    public abstract MagicHeader MagicSequence { get; }
    
    /// <summary>
    /// Send this message to the server
    /// </summary>
    public void SendToServer()
    {
        using var writer = ConstructWriter(MagicSequence, EmptyIdentificationMap.Instance);
        Serialize(writer);
        
        NetworkHandler.SendToServer(writer.ConstructBuffer(), DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Send this message to the specified receiver
    /// </summary>
    public void Send(int receiver)
    {
        using var writer = ConstructWriter(MagicSequence, EmptyIdentificationMap.Instance);
        Serialize(writer);
        
        NetworkHandler.SendToPending(receiver, writer.ConstructBuffer(), DeliveryMethod.ReliableOrdered);
    }
}