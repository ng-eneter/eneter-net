/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit
{
    /// <summary>
    /// The message internally used for the reliable communication between reliable duplex output channel
    /// and reliable duplex input channel.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class ReliableMessage
    {
        /// <summary>
        /// Indicates the purpose of the message.
        /// </summary>
        public enum EMessageType
        {
            /// <summary>
            /// The reliable message contains a message that shall be notified via the event.
            /// </summary>
            Message,

            /// <summary>
            /// The reliable message is the acknowledgement, that the message was received.
            /// </summary>
            Acknowledge
        }

        /// <summary>
        /// Default constructor for serializing purposes.
        /// </summary>
        public ReliableMessage()
        {
        }

        /// <summary>
        /// Constructs the acknowledge message.
        /// Acknowledge confirms, the message was delivered.
        /// </summary>
        public ReliableMessage(string messageId)
            : this(messageId, null)
        {
            MessageType = EMessageType.Acknowledge;
        }

        /// <summary>
        /// Constructs the reliable message that contains the message to be sent.
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="message"></param>
        public ReliableMessage(string messageId, object message)
        {
            MessageType = EMessageType.Message;
            MessageId = messageId;
            Message = message;
        }

        /// <summary>
        /// Type of the message.
        /// </summary>
        [DataMember]
        public EMessageType MessageType { get; set; }

        /// <summary>
        /// In case of the message type Message - unique id of the reliable message.
        /// In case of the message type Acknowledge - id of the message, that is acknowledged.
        /// </summary>
        [DataMember]
        public string MessageId { get; set; }

        /// <summary>
        /// In case of the message type Message - the message that shall be sent reliably.
        /// In case of the message type Acknowledge - not used.
        /// </summary>
        [DataMember]
        public object Message { get; set; }
    }
}
