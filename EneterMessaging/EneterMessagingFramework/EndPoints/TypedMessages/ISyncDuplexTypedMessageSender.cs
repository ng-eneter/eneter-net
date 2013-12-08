/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Message sender that sends a request message and then waits until the response is received.
    /// </summary>
    /// <typeparam name="TResponse">Response message type.</typeparam>
    /// <typeparam name="TRequest">Request message type.</typeparam>
    public interface ISyncDuplexTypedMessageSender<TResponse, TRequest> : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// The event is raised when the connection with the receiver is open.
        /// </summary>
        /// <remarks>
        /// Notice, the event is invoked in a thread from the thread pool. Therefore, if you need to manipulate UI,
        /// do not forget to marshal it to the UI thread.
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is raised when the connection with the receiver is closed.
        /// </summary>
        /// <remarks>
        /// Notice, the event is raised in a thread from the thread pool. Therefore, if you need to manipulate UI,
        /// do not forget to marshal it to the UI thread.
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// Sends the request message and returns the response.
        /// </summary>
        /// <param name="message">request message</param>
        /// <returns>response message</returns>
        TResponse SendRequestMessage(TRequest message);
    }
}

