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
    /// Declares the broker client.
    /// </summary>
    /// <remarks>
    /// The broker client allows to send messages via the broker, so that broker will forward them to all subscribers.<br/>
    /// It also allows to subscribe for messages the client is interested to.
    /// </remarks>
    public interface IDuplexBrokerClient : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// The event is invoked when the observed message is received from the broker.
        /// </summary>
        event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        /// <summary>
        /// Sends the message of the specified type to the broker.
        /// </summary>
        /// <param name="messageTypeId">message type id</param>
        /// <param name="serializedMessage">
        /// message content. If the message is not a simple type then the input parameter expects the message is already serialized!
        /// </param>
        void SendMessage(string messageTypeId, object serializedMessage);

        /// <summary>
        /// Subscribes the client for the message.
        /// </summary>
        /// <param name="messageType">message type the client wants to observe</param>
        void Subscribe(string messageType);

        /// <summary>
        /// Subscribes the client for list of messages.
        /// </summary>
        /// <param name="messageTypes">list of message types the client wants to observe</param>
        void Subscribe(string[] messageTypes);

        /// <summary>
        /// Subscribes the client for message types matching with the given regular expression.
        /// </summary>
        /// <remarks>
        /// When a published message comes to the broker, the broker will check the message type id
        /// and will forward it to all subscribed clients.<br/>
        /// The broker will use the given regular expression to recognize whether the client is subscribed
        /// or not.<br/>
        /// The broker internally uses Regex class provided by .NET.
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
        /// The broker internally uses Regex class provided by .NET.
        /// </remarks>
        /// <param name="regularExpressions">
        /// List of regular expressions that will be evaluated by the broker to recognize whether the client is subscribed.
        /// </param>
        void SubscribeRegExp(string[] regularExpressions);

        /// <summary>
        /// Unsubscribes the client from the specified message.
        /// </summary>
        /// <param name="messageType">message type the client does not want to observe anymore</param>
        void Unsubscribe(string messageType);

        /// <summary>
        /// Unsubscribes the client from specified messages.
        /// </summary>
        /// <param name="messageTypes">list of message types the client does not want to observe anymore</param>
        void Unsubscribe(string[] messageTypes);

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
        /// Removes all regular expression subscriptions.
        /// </summary>
        /// <remarks>
        /// When the broker receives this request, it will search if the given regular expression strings
        /// exist for the calling client. If yes, they will be removed.
        /// </remarks>
        /// <param name="regularExpressions"></param>
        void UnsubscribeRegExp(string[] regularExpressions);

        /// <summary>
        /// Completely unsubscribes the client from all messages (including all regular expressions).
        /// </summary>
        void Unsubscribe();
    }
}
