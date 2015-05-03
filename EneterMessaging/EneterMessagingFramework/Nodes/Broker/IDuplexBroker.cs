/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;

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
    }
}
