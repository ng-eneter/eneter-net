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
    public interface IMultiTypedMessageReceiver : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when a multi typed message sender opened the connection via its duplex output channel.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when a multi typed message sender closed the connection via its duplex output channel.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        event EventHandler<TypedRequestReceivedEventArgs<object>> FailedToReceiveRequestMessage;

        void RegisterRequestMessageReceiver<T>(EventHandler<TypedRequestReceivedEventArgs<T>> handler);

        void UnregisterRequestMessageReceiver<T>();

        IEnumerable<Type> RegisteredRequestMessageTypes { get; }

        void SendResponseMessage<TResponseMessage>(string responseReceiverId, TResponseMessage responseMessage);
    }
}
