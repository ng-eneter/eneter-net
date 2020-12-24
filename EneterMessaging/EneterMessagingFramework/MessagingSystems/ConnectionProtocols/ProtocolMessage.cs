


namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    /// <summary>
    /// Type of the message sent between channels.
    /// </summary>
    public enum EProtocolMessageType
    {
        /// <summary>
        /// Open connection message.
        /// </summary>
        OpenConnectionRequest,

        /// <summary>
        /// Close connection message.
        /// </summary>
        CloseConnectionRequest,

        /// <summary>
        /// Request message or response message.
        /// </summary>
        MessageReceived
    }

    /// <summary>
    /// Message decoded by the protocol formatter.
    /// </summary>
    /// <remarks>
    /// The protocol formatter is used for the internal communication between output and input channel.
    /// When the channel receives a message it uses the protocol formatter to figure out if is is 'Open Connection',
    /// 'Close Connection' or 'Data Message'.<br/>
    /// Protocol formatter decodes the message and returns ProtocolMessage.
    /// </remarks>
    public class ProtocolMessage
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ProtocolMessage()
        {
        }

        /// <summary>
        /// Constructs the protocol message from the given parameters.
        /// </summary>
        /// <param name="messageType">type of the message</param>
        /// <param name="responseReceiverId">client id</param>
        /// <param name="message">message content</param>
        public ProtocolMessage(EProtocolMessageType messageType, string responseReceiverId, object message)
        {
            MessageType = messageType;
            ResponseReceiverId = responseReceiverId;
            Message = message;
        }

        /// <summary>
        /// Type of the message.
        /// </summary>
        public EProtocolMessageType MessageType { get; set; }

        /// <summary>
        /// Client id.
        /// </summary>
        public string ResponseReceiverId { get; set; }

        /// <summary>
        /// If message type is MessageReceived the it contains the serialized message data.
        /// Otherwise it is null.
        /// </summary>
        public object Message { get; set; }
    }
}
