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
    /// Sender capable to send messages of multiple types.
    /// </summary>
    /// <remarks>
    /// This is a client component which can send request messages and receive response messages.
    /// MultiTypedMessageSende can send messages only to MultiTypedMessageReceiver.
    /// </remarks>
    public interface IMultiTypedMessageSender : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// The event is raised when the connection with the receiver is open.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is raised when the connection with the receiver is closed.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// Registeres the handler for specified response message type.
        /// </summary>
        /// <remarks>
        /// The handler is called if the message of specified type is deserialized from the incoming message data.
        /// </remarks>
        /// <typeparam name="T">data type of the response message which will be handled by the handler</typeparam>
        /// <param name="handler">handler which will be called if the response message of specified type is received</param>
        void RegisterResponseMessageReceiver<T>(EventHandler<TypedResponseReceivedEventArgs<T>> handler);

        /// <summary>
        /// Unregisteres the handler for the specified type.
        /// </summary>
        /// <typeparam name="T">data type of the resposne message for which the handler shall be unregistered</typeparam>
        void UnregisterResponseMessageReceiver<T>();

        /// <summary>
        /// Returns response message types which are registered to be received.
        /// </summary>
        IEnumerable<Type> RegisteredResponseMessageTypes { get; }

        /// <summary>
        /// Sends request message.
        /// </summary>
        /// <remarks>
        /// The message of the specified type will be serialized and sent to the receiver.
        /// If the receiver has registered a handler for this message type then the handler will be called to process the message.
        /// </remarks>
        /// <typeparam name="TRequestMessage">Type of the message.</typeparam>
        /// <param name="message">request message</param>
        void SendRequestMessage<TRequestMessage>(TRequestMessage message);
    }
}
