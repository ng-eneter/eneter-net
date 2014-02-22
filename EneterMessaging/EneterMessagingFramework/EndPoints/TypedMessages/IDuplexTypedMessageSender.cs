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
    /// Sender of typed messages.
    /// </summary>
    /// <typeparam name="_ResponseType">receives response messages of this type.</typeparam>
    /// <typeparam name="_RequestType">sends messages of this type.</typeparam>
    public interface IDuplexTypedMessageSender<_ResponseType, _RequestType> : IAttachableDuplexOutputChannel
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
        /// The event is invoked when a response message was received.
        /// </summary>
        event EventHandler<TypedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;

        /// <summary>
        /// Sends message of specified type.
        /// </summary>
        /// <param name="message"></param>
        void SendRequestMessage(_RequestType message);
    }
}
