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

        event EventHandler<TypedResponseReceivedEventArgs<object>> FailedToReceiveResponseMessage;

        void RegisterResponseMessageReceiver<T>(EventHandler<TypedResponseReceivedEventArgs<T>> handler);

        void UnregisterResponseMessageReceiver<T>();

        IEnumerable<Type> RegisteredResponseMessageTypes { get; }

        void SendRequestMessage<TRequestMessage>(TRequestMessage message);
    }
}
