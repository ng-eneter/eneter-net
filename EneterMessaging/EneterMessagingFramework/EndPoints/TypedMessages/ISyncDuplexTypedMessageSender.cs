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
    /// Synchronized sender of typed messages (it waits until the response is received).
    /// </summary>
    /// <remarks>
    /// Declares message sender that sends request messages of specified type and receive response messages of specified type.
    /// Synchronous means it when the message is sent it waits until the response message is received.
    /// If the waiting for the response message exceeds the specified timeout the TimeoutException is thrown.
    /// </remarks>
    /// <typeparam name="TResponse">Response message type.</typeparam>
    /// <typeparam name="TRequest">Request message type.</typeparam>
    public interface ISyncDuplexTypedMessageSender<TResponse, TRequest> : IAttachableDuplexOutputChannel
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
        /// Sends the request message and returns the response.
        /// </summary>
        /// <remarks>
        /// It waits until the response message is received. If waiting for the response exceeds the specified timeout
        /// TimeoutException is thrown.
        /// </remarks>
        /// <param name="message">request message</param>
        /// <returns>response message</returns>
        TResponse SendRequestMessage(TRequest message);
    }
}

