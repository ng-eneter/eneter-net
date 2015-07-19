/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using System.Collections.Generic;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Broker component.
    /// </summary>
    /// <remarks>
    /// The broker is the communication component intended for publish-subscribe scenario.
    /// It is the component which allows consumers to subscribe for desired message types
    /// and allows publishers to send a message to subscribed consumers.<br/>
    /// <br/>
    /// When the broker receives a message from a publisher it finds all consumers subscribed to that
    /// message and forwards them the message.
    /// </remarks>
    public interface IDuplexBroker : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the publisher published a message to subscribers.
        /// </summary>
        event EventHandler<PublishInfoEventArgs> MessagePublished;

        /// <summary>
        /// The event is invoked when the broker subscribed a client for messages.
        /// </summary>
        event EventHandler<SubscribeInfoEventArgs> ClientSubscribed;

        /// <summary>
        /// The event is invoked when the broker unsubscribed a client from messages.
        /// </summary>
        event EventHandler<SubscribeInfoEventArgs> ClientUnsubscribed;
        

        /// <summary>
        /// The event is invoked when the observed message is received.
        /// </summary>
        event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        /// <summary>
        /// Publishes the message.
        /// </summary>
        /// <param name="messageType">identifies the type of the published message. The broker will forward the message
        /// to all subscribers subscribed to this message type.</param>
        /// <param name="serializedMessage">message content.</param>
        void SendMessage(string messageType, object serializedMessage);

        /// <summary>
        /// Subscribes for the message type.
        /// </summary>
        /// <param name="messageType">identifies the type of the message which shall be subscribed.</param>
        void Subscribe(string messageType);

        /// <summary>
        /// Subscribes for list of message types.
        /// </summary>
        /// <param name="messageTypes">list of message types which shall be subscribed.</param>
        void Subscribe(string[] messageTypes);

        /// <summary>
        /// Unsubscribes from the specified message type.
        /// </summary>
        /// <param name="messageType">message type the client does not want to receive anymore.</param>
        void Unsubscribe(string messageType);

        /// <summary>
        /// Unsubscribes from specified events.
        /// </summary>
        /// <param name="messageTypes">list of message types the client does not want to receive anymore.</param>
        void Unsubscribe(string[] messageTypes);

        /// <summary>
        /// Unsubscribe all messages.
        /// </summary>
        void Unsubscribe();

        /// <summary>
        /// Returns messages which are subscribed by the given subscriber.
        /// </summary>
        /// <param name="responseReceiverId">subscriber response receiver id.</param>
        /// <returns>subscribed messages</returns>
        IEnumerable<string> GetSubscribedMessages(string responseReceiverId);

        /// <summary>
        /// Returns subscribers which are subscribed for the given message type id.
        /// </summary>
        /// <param name="messageTypeId">message type id</param>
        /// <returns>subscribed subscribers</returns>
        IEnumerable<string> GetSubscribedResponseReceivers(string messageTypeId);
    }
}
