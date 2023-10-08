using System;

namespace MintyCore.Network;

public interface IConcurrentServer : IDisposable
{
    /// <summary>
    /// Update the server
    /// </summary>
    void Update();

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="IMessage"/> interface messages
    /// </summary>
    void SendMessage(ushort[] receivers, Span<byte> data, DeliveryMethod deliveryMethod);

    /// <summary>
    /// Send a message to the server. Dont call this manually this is meant to be used by auto generated methods for the <see cref="IMessage"/> interface messages
    /// </summary>
    void SendMessage(ushort receiver, Span<byte> data, DeliveryMethod deliveryMethod);

    /// <summary>
    /// Check if an id is in a pending state
    /// </summary>
    bool IsPending(ushort tempId);

    /// <summary>
    /// Reject a pending id
    /// </summary>
    /// <param name="tempId"></param>
    void RejectPending(ushort tempId);

    /// <summary>
    /// Accept a pending id and replace it with a proper game id
    /// </summary>
    void AcceptPending(ushort tempId, ushort gameId);
}