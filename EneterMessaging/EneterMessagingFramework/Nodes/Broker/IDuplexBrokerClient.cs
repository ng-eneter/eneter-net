/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Broker client to publish and subscribe messages in the broker.
    /// </summary>
    /// <remarks>
    /// The broker client allows to publish events via the broker, so that broker will forward them to all subscribers.<br/>
    /// BrokerClient also allows to subscribe for events of interest.
    /// </remarks>
    public interface IDuplexBrokerClient : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// Event raised when the connection with the service was open.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// Event raised when the connection with the service was closed.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// The event is invoked when the observed event is received from the broker.
        /// </summary>
        event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        /// <summary>
        /// Publishes the event via the broker.
        /// </summary>
        /// <param name="eventId">identifies the event</param>
        /// <param name="serializedMessage">
        /// message content. If the message is not a primitive type or string then the input parameter expects the message is already serialized!
        /// </param>
        void SendMessage(string eventId, object serializedMessage);

        /// <summary>
        /// Subscribes the client for the event.
        /// </summary>
        /// <param name="eventId">type of event that shall be observed</param>
        void Subscribe(string eventId);

        /// <summary>
        /// Subscribes the client for list of events.
        /// </summary>
        /// <param name="eventIds">list of events that shall be observed</param>
        void Subscribe(string[] eventIds);

        /// <summary>
        /// Unsubscribes the client from the specified event.
        /// </summary>
        /// <param name="eventId">type of event which shall not be observed anymore</param>
        void Unsubscribe(string eventId);

        /// <summary>
        /// Unsubscribes the client from specified events.
        /// </summary>
        /// <param name="eventIds">list of events that shall not be observed anymore</param>
        void Unsubscribe(string[] eventIds);

        /// <summary>
        /// Unsubscribes this client from all types of events and regular expressions.
        /// </summary>
        void Unsubscribe();
    }
}
