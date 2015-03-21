/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Receiver which receives messages of multiple types.
    /// </summary>
    /// <remarks>
    /// This is a service component which can receive request messages and send back response messages.
    /// MultiTypedMessageReceiver can receive messages from only from MultiTypedMessageSender and from SyncMultiTypedMessageSender.
    /// </remarks>
    public interface IMultiTypedMessageReceiver : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when a new multi typed message sender (client) opened the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when a multi typed message sender (client) closed the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Registeres the handler for specified message type.
        /// </summary>
        /// <remarks>
        /// The handler is called if the message of specified type is deserialized from the incoming message data.
        /// </remarks>
        /// <typeparam name="T">Type of the message the handler shall process.</typeparam>
        /// <param name="handler">The callback method which will be called to process the message of the given type.</param>
        void RegisterRequestMessageReceiver<T>(EventHandler<TypedRequestReceivedEventArgs<T>> handler);

        /// <summary>
        /// Unregisteres the handler for the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void UnregisterRequestMessageReceiver<T>();

        /// <summary>
        /// Returns message types which are registered to be received.
        /// </summary>
        IEnumerable<Type> RegisteredRequestMessageTypes { get; }

        /// <summary>
        /// Sends the response message of the specified type.
        /// </summary>
        /// <remarks>
        /// The message of the specified type will be serialized and sent back to the response receiver.
        /// If the response receiver has registered a handler for this message type then the handler will be called to process the message.
        /// </remarks>
        /// <typeparam name="TResponseMessage">Type of the message.</typeparam>
        /// <param name="responseReceiverId">Identifies response receiver which will receive the message.
        /// If the value is * then the broadcast to all connected response receivers will be sent.</param>
        /// <param name="responseMessage">response message</param>
        void SendResponseMessage<TResponseMessage>(string responseReceiverId, TResponseMessage responseMessage);
    }
}
