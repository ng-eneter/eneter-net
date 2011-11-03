/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// The interface declares the strongly typed message receiver.
    /// The receiver is able to receive messages of the specified type via one-way input channel.
    /// </summary>
    /// <typeparam name="_MessageDataType">
    /// Type of the message.
    /// </typeparam>
    public interface ITypedMessageReceiver<_MessageDataType> : IAttachableInputChannel
    {
        /// <summary>
        /// The event is invoked when a typed message was received.
        /// </summary>
        event EventHandler<TypedMessageReceivedEventArgs<_MessageDataType>> MessageReceived;
    }
}
