using System;
using JetBrains.Annotations;
using LiteNetLib;

namespace MintyCore.Network;

/// <summary>
///  Represents a server that can send and receive messages concurrently
/// </summary>
[PublicAPI]
public interface IConcurrentServer : IDisposable
{
    /// <summary>
    /// Update the server
    /// </summary>
    void Update();

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="Message"/> interface messages
    /// </summary>
    void SendMessage(Player[] receivers, Span<byte> data, DeliveryMethod deliveryMethod);

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="Message"/> interface messages
    /// </summary>
    void SendMessage(Player receiver, Span<byte> data, DeliveryMethod deliveryMethod);

    /// <summary>
    /// Check if an id is in a pending state
    /// </summary>
    bool IsPending(int tempId);

    /// <summary>
    /// Reject a pending id
    /// </summary>
    /// <param name="tempId"></param>
    void RejectPending(int tempId);

    /// <summary>
    /// Accept a pending id and replace it with a proper game id
    /// </summary>
    void AcceptPending(int tempId, Player player);

    void SendToPending(int tempId, Span<byte> data, DeliveryMethod deliveryMethod);
}