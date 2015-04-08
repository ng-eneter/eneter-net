/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    /// <summary>
    /// Type of the message (if it is ping or a data message).
    /// </summary>
    public enum MonitorChannelMessageType
    {
        /// <summary>
        /// Indicates, it is the ping message or ping response.
        /// </summary>
        Ping = 10,

        /// <summary>
        /// Indicates, it is a message or a response message containing data. 
        /// </summary>
        Message = 20
    }

    /// <summary>
    /// The message internally used to monitor the connection and also to transfer message data.
    /// </summary>
    /// <remarks>
    /// The message is supposed to be serialized/deserialized by a custom serializer.
    /// Therefore it does not have to be public and does not have to be labeled by attributes.
    /// </remarks>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class MonitorChannelMessage
    {
        /// <summary>
        /// Constructs the message. This constructor is used by the Xml serializer for the deserialization.
        /// </summary>
        public MonitorChannelMessage()
        {
        }

        /// <summary>
        /// Constructs the message from specified parameters.
        /// </summary>
        /// <param name="messageType">type of the message, ping or regular message</param>
        /// <param name="messageContent">message content, in case of ping this parameter is not used</param>
        public MonitorChannelMessage(MonitorChannelMessageType messageType, object messageContent)
        {
            MessageType = messageType;
            MessageContent = messageContent;
        }

        /// <summary>
        /// Type of the message. Ping or regular message.
        /// </summary>
        [DataMember]
        public MonitorChannelMessageType MessageType { get; set; }

        /// <summary>
        /// Message. In case of the 'ping', this property is null.
        /// </summary>
        [DataMember]
        public object MessageContent { get; set; }
    }
}
