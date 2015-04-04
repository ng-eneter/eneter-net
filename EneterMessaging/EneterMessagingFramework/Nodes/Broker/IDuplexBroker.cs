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
    /// Broker component (for publish-subscribe scenarios).
    /// </summary>
    /// <remarks>
    /// The broker receives messages and forwards them to subscribed clients.
    /// </remarks>
    public interface IDuplexBroker : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the observed event is received.
        /// </summary>
        event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        /// <summary>
        /// Publishes the event.
        /// </summary>
        /// <param name="eventId">identification of published event.</param>
        /// <param name="serializedMessage">
        /// message content. If the message is not a primitive type or string then the input parameter expects the message is already serialized!
        /// </param>
        void SendMessage(string eventId, object serializedMessage);

        /// <summary>
        /// Subscribes for the event.
        /// </summary>
        /// <remarks>
        /// If you can call this method multiple times to subscribe for multile events.
        /// </remarks>
        /// <param name="eventId">identification of event that shall be observed</param>
        void Subscribe(string eventId);

        /// <summary>
        /// Subscribes for list of events.
        /// </summary>
        /// <remarks>
        /// If you can call this method multiple times to subscribe for multile events.
        /// </remarks>
        /// <param name="eventIds">list of events that shall be observed</param>
        void Subscribe(string[] eventIds);

        /// <summary>
        /// Unsubscribes from the specified event.
        /// </summary>
        /// <param name="eventId">type of event which shall not be observed anymore</param>
        void Unsubscribe(string eventId);

        /// <summary>
        /// Unsubscribes from specified events.
        /// </summary>
        /// <param name="eventIds">list of events that shall not be observed anymore</param>
        void Unsubscribe(string[] eventIds);

        /// <summary>
        /// Unsubscribes from all types of events and regular expressions.
        /// </summary>
        void Unsubscribe();
    }
}
