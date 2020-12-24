

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Specifies the internal broker request inside the <see cref="BrokerMessage"/>.
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
        Subscribe = 10,

        /// <summary>
        /// Request to unsubscribe from exactly specified message.
        /// </summary>
        Unsubscribe = 20,

        /// <summary>
        /// Request to unsubscribe all messages and regular expressions.
        /// </summary>
        UnsubscribeAll = 30,

        /// <summary>
        /// Request to publish a message.
        /// </summary>
        Publish = 40
    }


    /// <summary>
    /// Internal message used between DuplexBroker and DuplexBrokerClient.
    /// </summary>
    [Serializable]
    [DataContract]
    public class BrokerMessage
    {
        /// <summary>
        /// Default constructor used by a deserializer.
        /// </summary>
        public BrokerMessage()
        {
        }

        /// <summary>
        /// Constructs the message requesting the broker to subscribe or unsubscribe events.
        /// </summary>
        /// <param name="request">subscribing or unsubscribing</param>
        /// <param name="messageTypes">events to be subscribed or unsubscribed.</param>
        public BrokerMessage(EBrokerRequest request, string[] messageTypes)
        {
            Request = request;
            MessageTypes = messageTypes;
            Message = null;
        }

        /// <summary>
        /// Constructs the broker message requesting the broker to publish an event.
        /// </summary>
        /// <param name="messageTypeId">message type that shall be published</param>
        /// <param name="message">serialized message to be published</param>
        public BrokerMessage(string messageTypeId, object message)
        {
            Request = EBrokerRequest.Publish;
            MessageTypes = new string[] { messageTypeId };
            Message = message;
        }

        /// <summary>
        /// Type of the request.
        /// </summary>
        [DataMember]
        public EBrokerRequest Request { get; set; }

        /// <summary>
        /// Array of message types.
        /// </summary>
        [DataMember]
        public string[] MessageTypes { get; set; }

        /// <summary>
        /// Serialized message that shall be notified to subscribers.
        /// </summary>
        [DataMember]
        public object Message { get; set; }
    }
}
