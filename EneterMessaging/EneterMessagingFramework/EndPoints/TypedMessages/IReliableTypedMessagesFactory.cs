/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/


namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// The interface declares the factory to create reliable typed message sender and receiver.
    /// </summary>
    public interface IReliableTypedMessagesFactory
    {
        /// <summary>
        /// Creates reliable typed message sender.
        /// </summary>
        /// <typeparam name="_ResponseType">type of response messages</typeparam>
        /// <typeparam name="_RequestType">type of request messages</typeparam>
        /// <returns>reliable typed message sender</returns>
        IReliableTypedMessageSender<_ResponseType, _RequestType> CreateReliableDuplexTypedMessageSender<_ResponseType, _RequestType>();

        /// <summary>
        /// Creates reliable typed message receiver.
        /// </summary>
        /// <typeparam name="_ResponseType">type of response messages</typeparam>
        /// <typeparam name="_RequestType">type of request messages</typeparam>
        /// <returns>reliable typed message receiver</returns>
        IReliableTypedMessageReceiver<_ResponseType, _RequestType> CreateReliableDuplexTypedMessageReceiver<_ResponseType, _RequestType>();
    }
}
