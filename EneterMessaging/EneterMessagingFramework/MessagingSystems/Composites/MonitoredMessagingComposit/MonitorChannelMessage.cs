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
    /// Type of the message.
    /// </summary>
    public enum MonitorChannelMessageType
    {
        /// <summary>
        /// Indicates, it is the ping message or ping response.
        /// </summary>
        Ping = 10,

        /// <summary>
        /// Indicates, it is a regular data message between output and input channel. 
        /// </summary>
        Message = 20
    }

    /// <summary>
    /// Internal message used for the communication between output and input channels in monitored messaging.
    /// </summary>
    [Serializable]
    [DataContract]
    public class MonitorChannelMessage
    {
        /// <summary>
        /// Constructs the message.
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
        /// Serialized data message.
        /// </summary>
        /// <remarks>
        /// In case of 'ping', this property is null.
        /// </remarks>
        [DataMember]
        public object MessageContent { get; set; }
    }
}
