using MintyCore.Utils;

namespace MintyCore.Network
{
    public interface IMessage
    {
        /// <summary>
        /// Array of Player game ids who should receive the message. Leave empty if the message is only from client to server. <seealso cref="MintyCore._playerIDs"/>
        /// </summary>
        public ushort[] Receivers { get; }
        
        /// <summary>
        /// Whether or not the message should be automatically send
        /// </summary>
        public bool AutoSend { get; }
        
        
        public bool IsServer { set; }
        
        /// <summary>
        /// The interval how often a message should be send( 1 = every frame, 5 = every fifth frame)
        /// </summary>
        public int AutoSendInterval { get; }
        
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
        /// Populate the message with relevant data.
        /// </summary>
        /// <param name="data">Optional data to pass</param>
        public void PopulateMessage(object? data = null);

        /// <summary>
        /// Clear all internal message data
        /// </summary>
        public void Clear();
    }
}