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
    /// The interface declares the strongly typed duplex message sender.
    /// The duplex sender is able to send messages of the specified type and receive responses of the specified type.
    /// </summary>
    /// <typeparam name="_ResponseType">The type of receiving response messages.</typeparam>
    /// <typeparam name="_RequestType">The type of sending messages.</typeparam>
    public interface IDuplexTypedMessageSender<_ResponseType, _RequestType> : IAttachableDuplexOutputChannel
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
        /// The event is invoked when a response message was received.
        /// </summary>
        event EventHandler<TypedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;

        /// <summary>
        /// Sends the strongly typed message.
        /// </summary>
        /// <param name="message"></param>
        void SendRequestMessage(_RequestType message);
    }
}
