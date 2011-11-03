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
    /// Specifies the broker request.
    /// </summary>
    /// <remarks>
    /// The request for the broker is the message that is intended for the broker and not for the subscribers.
    /// This message is used by the broker client to subscribe and unsubscribe.
    /// </remarks>
    public enum EBrokerRequest
    {
        /// <summary>
        /// Request to subscribe exactly for the specified message.
        /// </summary>
        Subscribe,

        /// <summary>
        /// Request to subscribe for message type ids that match with the regular expression.
        /// I.e. regular expression is used to identify what message types shall be notified
        /// to the client.
        /// </summary>
        SubscribeRegExp,

        /// <summary>
        /// Request to unsubscribe from exactly specified message.
        /// </summary>
        Unsubscribe,

        /// <summary>
        /// Request to unsubscribe the regular expression.
        /// </summary>
        UnsubscribeRegExp,

        /// <summary>
        /// Request to unsubscribe all messages and regular expressions.
        /// </summary>
        UnsubscribeAll
    }


    /// <summary>
    /// The class represents the data structure used to send requests to the broker.
    /// </summary>
    /// <remarks>
    /// The request for the broker is the message that is intended for the broker and not for the subscribers.
    /// This message is used by the broker client to subscribe and unsubscribe.
    /// </remarks>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class BrokerRequestMessage
    {
        /// <summary>
        /// Default constructor used for serialization/deserialization.
        /// </summary>
        public BrokerRequestMessage()
        {
        }

        /// <summary>
        /// Creates the request from input parameters.
        /// </summary>
        /// <param name="request">subscribe or unsubscribe request</param>
        /// <param name="messageTypes">message types that shall be subscribed or unsubscribed</param>
        public BrokerRequestMessage(EBrokerRequest request, string[] messageTypes)
        {
            Request = request;
            MessageTypes = messageTypes;
        }

        /// <summary>
        /// Returns type of the request.
        /// </summary>
        [DataMember]
        public EBrokerRequest Request { get; set; }

        /// <summary>
        /// Returns the array of message types.
        /// </summary>
        [DataMember]
        public string[] MessageTypes { get; set; }
    }
}
