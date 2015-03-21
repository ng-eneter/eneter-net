/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Sender which sends a message and then waits for a response.
    /// </summary>
    public interface ISyncMultitypedMessageSender : IAttachableDuplexOutputChannel
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
        /// Sends the message of specified type and then waits for the response of specified type.
        /// </summary>
        /// <typeparam name="TResponse">Type of request message.</typeparam>
        /// <typeparam name="TRequest">Type of response message.</typeparam>
        /// <param name="message">request message</param>
        /// <returns>response message</returns>
        TResponse SendRequestMessage<TResponse, TRequest>(TRequest message);
    }
}
