/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/


namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    /// <summary>
    /// Indicates the type of the low-level protocol message.
    /// </summary>
    public enum EProtocolMessageType
    {
        /// <summary>
        /// Unknown message.
        /// </summary>
        Unknown,

        /// <summary>
        /// Open connection request message.
        /// </summary>
        OpenConnectionRequest,

        /// <summary>
        /// Close connection request message.
        /// </summary>
        CloseConnectionRequest,

        /// <summary>
        /// Message or reaponse message.
        /// </summary>
        MessageReceived
    }

    /// <summary>
    /// Represents decoded low-level protocol message.
    /// </summary>
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
        /// The content of the message or response message.
        /// </summary>
        public object Message { get; set; }
    }
}
