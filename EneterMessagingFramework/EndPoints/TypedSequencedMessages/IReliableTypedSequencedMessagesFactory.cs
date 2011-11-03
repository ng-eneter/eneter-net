/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The interface declares the factory to create reliable typed sequenced message sender and receiver.
    /// </summary>
    public interface IReliableTypedSequencedMessagesFactory
    {
        /// <summary>
        /// Creates the reliable typed sequenced message sender.
        /// </summary>
        /// <typeparam name="_ResponseType">type of response message</typeparam>
        /// <typeparam name="_RequestType">type of request message</typeparam>
        /// <returns>reliable typed sequenced message sender</returns>
        IReliableTypedSequencedMessageSender<_ResponseType, _RequestType> CreateReliableTypedSequencedMessageSender<_ResponseType, _RequestType>();

        /// <summary>
        /// reliable typed sequenced message receiver.
        /// </summary>
        /// <typeparam name="_ResponseType">type of response message</typeparam>
        /// <typeparam name="_RequestType">type of request message</typeparam>
        /// <returns>reliable typed sequenced message receiver</returns>
        IReliableTypedSequencedMessageReceiver<_ResponseType, _RequestType> CreateReliableTypedSequencedMessageReceiver<_ResponseType, _RequestType>();
    }
}
