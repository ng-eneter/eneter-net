/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Sender of specified message type.
    /// </summary>
    /// <remarks>
    /// This is a client component which can send request messages and receive response messages.
    /// DuplexTypedMessageSender can send messages only to DuplexTypedMessageReceiver.
    /// </remarks>
    /// <typeparam name="TResponse">receives response messages of this type.</typeparam>
    /// <typeparam name="TRequest">sends messages of this type.</typeparam>
    public interface IDuplexTypedMessageSender<TResponse, TRequest> : IAttachableDuplexOutputChannel
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
        /// The event is raised when a response message is received.
        /// </summary>
        event EventHandler<TypedResponseReceivedEventArgs<TResponse>> ResponseReceived;

        /// <summary>
        /// Sends message.
        /// </summary>
        /// <remarks>
        /// The given message is serialized and then sent via duplex output channel to the connected DuplexTypedMessageReceiver.
        /// DuplexTypedMessageReceiver deserilizes the message and raises the MessageReceived event to notify that a message is received.
        /// </remarks>
        /// <param name="message">request message</param>
        void SendRequestMessage(TRequest message);
    }
}
