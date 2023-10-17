using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Network;

/// <summary>
///     Interface for all messages.
/// </summary>
[PublicAPI]
public interface IMessage
{
    /// <summary>
    ///     Whether or not the current execution is server side (used for serialization/deserialization)
    /// </summary>
    public bool IsServer { get; set; }

    /// <summary>
    ///     Whether or not the message will be executed on the main thread or not
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    public bool ReceiveMultiThreaded { get; }

    /// <summary>
    ///     <see cref="Identification" /> of the message
    /// </summary>
    Identification MessageId { get; }

    /// <summary>
    ///     How the message should be delivered
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    DeliveryMethod DeliveryMethod { get; }

    /// <summary>
    /// Id of the sender of this message. Might be a temporary id for pending clients
    /// </summary>
    ushort Sender { get; set; }

    /// <summary>
    ///     Serialize the data
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    public void Serialize(DataWriter writer);

    /// <summary>
    ///     Deserialize the data
    /// </summary>
    /// <returns>True if deserialization was successful</returns>
    public bool Deserialize(DataReader reader);

    /// <summary>
    ///     Clear all internal message data
    /// </summary>
    public void Clear();


    /// <summary>
    /// Send this message to the server
    /// Implementation is provided by a source generator
    /// </summary>
    public void SendToServer();

    /// <summary>
    /// Send this message to the specified receivers
    /// Implementation is provided by a source generator
    /// </summary>
    public void Send(IEnumerable<ushort> receivers);

    /// <summary>
    /// Send this message to the specified receiver
    /// Implementation is provided by a source generator
    /// </summary>
    public void Send(ushort receiver);

    /// <summary>
    /// Send this message to the specified receivers
    /// Implementation is provided by a source generator
    /// </summary>
    public void Send(ushort[] receivers);

    public INetworkHandler NetworkHandler { protected get; init; }
}