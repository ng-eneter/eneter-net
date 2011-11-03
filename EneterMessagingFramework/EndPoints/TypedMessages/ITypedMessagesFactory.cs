/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// The interface declares the factory to create strongly typed message senders and receivers.
    /// </summary>
    public interface ITypedMessagesFactory
    {
        /// <summary>
        /// Creates the typed message sender.
        /// The sender sends the messages via attached one-way output channel.
        /// </summary>
        ITypedMessageSender<_MessageDataType> CreateTypedMessageSender<_MessageDataType>();

        /// <summary>
        /// Creates the typed message receiver.
        /// The receiver receives messages via attached one-way input channel.
        /// </summary>
        ITypedMessageReceiver<_MessageDataType> CreateTypedMessageReceiver<_MessageDataType>();
    }
}
