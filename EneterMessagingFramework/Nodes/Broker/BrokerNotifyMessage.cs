/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// The data representing the message sent to the broker to notify subscribed clients.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class BrokerNotifyMessage
    {
        /// <summary>
        /// Default constructor used for the deserialization.
        /// </summary>
        public BrokerNotifyMessage()
        {
        }

        /// <summary>
        /// Constructs the message data from the input parameters.
        /// </summary>
        /// <param name="messageTypeId">Type of the notified message.</param>
        /// <param name="message">Message content.</param>
        public BrokerNotifyMessage(string messageTypeId, object message)
        {
            MessageTypeId = messageTypeId;
            Message = message;
        }

        /// <summary>
        /// Type of the notified message.
        /// </summary>
        [DataMember]
        public string MessageTypeId { get; set; }

        /// <summary>
        /// Serialized message that shall be notified to subscribers.
        /// </summary>
        [DataMember]
        public object Message { get; set; }
    }
}
