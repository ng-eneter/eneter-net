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
        /// Subscribes the client for event types matching with the given regular expression.
        /// </summary>
        /// <remarks>
        /// When a published message comes to the broker, the broker will check the event id
        /// and will forward it to all subscribed clients.<br/>
        /// The broker will use the given regular expression to recognize whether the client is subscribed
        /// or not.<br/>
        /// The .NET based Broker internally uses Regex class to evaluate the regular expression.<br/>
        /// The Java based Broker internally uses Pattern class to evaluate the regular expression.<br/>
        /// Regular expressions between .NET and Java does not have to be fully compatible.
        /// 
        /// <example>
        /// Few examples for subscribing via regular expression.
        /// <code>
        /// // Subscribing for message types starting with the string MyMsg.Speed
        /// myDuplexBrokerClient.SubscribeRegExp(@"^MyMsg\.Speed);
        /// 
        /// // Subscribing for message types starting with MyMsg.Speed or App.Utilities
        /// myDuplexBrokerClient.SubscribeRegExp(@"^MyMsg\.Speed|^App\.Utilities");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="regularExpression">
        /// Regular expression that will be evaluated by the broker to recognize whether the client is subscribed.
        /// </param>
        void SubscribeRegExp(string regularExpression);

        /// <summary>
        /// Subscribes the client for message types matching with the given list of regular expressions.
        /// </summary>
        /// <remarks>
        /// When a published message comes to the broker, the broker will check the message type id
        /// and will forward it to all subscribed clients.<br/>
        /// The broker will use the given regular expression to recognize whether the client is subscribed
        /// or not.<br/>
        /// The .NET based Broker internally uses Regex class to evaluate the regular expression.<br/>
        /// The Java based Broker internally uses Pattern class to evaluate the regular expression.<br/>
        /// Regular expressions between .NET and Java does not have to be fully compatible.
        /// </remarks>
        /// <param name="regularExpressions">
        /// List of regular expressions that will be evaluated by the broker to recognize whether the client is subscribed.
        /// </param>
        void SubscribeRegExp(string[] regularExpressions);

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
        /// Removes the regular expression subscription.
        /// </summary>
        /// <remarks>
        /// When the broker receives this request, it will search if the given regular expression string
        /// exists for the calling client. If yes, it will be removed.
        /// </remarks>
        /// <param name="regularExpression">Regular expression that was previously used for the subscription
        /// and now shall be removed.</param>
        void UnsubscribeRegExp(string regularExpression);

        /// <summary>
        /// Removes regular expression subscriptions.
        /// </summary>
        /// <remarks>
        /// When the broker receives this request, it will search if the given regular expression strings
        /// exist for the calling client. If yes, they will be removed.
        /// </remarks>
        /// <param name="regularExpressions">Regular expressions that shall be removed from subscriptions.</param>
        void UnsubscribeRegExp(string[] regularExpressions);

        /// <summary>
        /// Unsubscribes this client from all types of events and regular expressions.
        /// </summary>
        void Unsubscribe();
    }
}
