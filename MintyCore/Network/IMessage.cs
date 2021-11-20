using System;
using MintyCore.Utils;

namespace MintyCore.Network
{
    public interface IMessage
    {
        /// <summary>
        /// Array of Player game ids who should receive the message. Leave empty if the message is only from client to server. <seealso cref="Engine.PlayerIDs"/>
        /// </summary>
        public ushort[]? Receivers { get; }
        
        public bool IsServer { set; }
        
        /// <summary>
        /// Whether or not the message will be executed on the main thread or not
        /// </summary>
        public bool ReceiveMultiThreaded { get; }

        /// <summary>
        /// <see cref="Identification"/> of the message
        /// </summary>
        Identification MessageId { get; }
        
        /// <summary>
        /// Which direction the message should be sent
        /// </summary>
        MessageDirection MessageDirection { get; }
        
        /// <summary>
        /// How the message should be delivered
        /// </summary>
        DeliveryMethod DeliveryMethod { get; }

        /// <summary>
        /// Serialize the data
        /// </summary>
        public void Serialize(DataWriter writer);
        
        /// <summary>
        /// Deserialize the data
        /// </summary>
        public void Deserialize(DataReader reader);

        /// <summary>
        /// Clear all internal message data
        /// </summary>
        public void Clear();
    }
}