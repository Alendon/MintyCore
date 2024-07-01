using System;
using LiteNetLib;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Network;

public abstract class MessageBase
{
    public Action<MessageBase>? RecycleCallback { private get; set; }

    /// <summary>
    ///     Whether the current execution is server side (used for serialization/deserialization)
    /// </summary>
    public bool IsServer { get; set; }
    
    /// <summary>
    ///     <see cref="Identification" /> of the message
    /// </summary>
    public abstract Identification MessageId { get; }

    /// <summary>
    ///     Whether the message will be executed on the main thread or not
    /// </summary>
// ReSharper disable once UnusedMemberInSuper.Global
    public abstract bool ReceiveMultiThreaded { get; }

    /// <summary>
    ///     How the message should be delivered
    /// </summary>
// ReSharper disable once UnusedMemberInSuper.Global
    public abstract DeliveryMethod DeliveryMethod { get; }

    /// <summary/>
    public required INetworkHandler NetworkHandler { protected get; init; }

    /// <summary>
    ///     Serialize the data
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    public abstract void Serialize(DataWriter writer);

    /// <summary>
    ///     Deserialize the data
    /// </summary>
    /// <returns>True if deserialization was successful</returns>
    public abstract bool Deserialize(DataReader reader);

    /// <summary>
    ///     Clear all internal message data
    /// </summary>
    public abstract void Clear();

    protected virtual DataWriter ConstructWriter(MagicHeader magic)
    {
        var writer = new DataWriter(magic);
        writer.Put(ReceiveMultiThreaded);
        return writer;
    }

    public void Recycle()
    {
        if (RecycleCallback is null)
        {
            Log.Error("Message was already recycled or not acquired from the NetworkHandler");
            return;
        }
        
        Clear();
        RecycleCallback.Invoke(this);
        
        RecycleCallback = null;
    }
}