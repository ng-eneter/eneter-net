/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The interface declares the factory to create duplex typed sequenced message sender and receiver.
    /// </summary>
    public interface IDuplexTypedSequencedMessagesFactory
    {
        /// <summary>
        /// Creates the duplex typed sequenced message sender.
        /// </summary>
        /// <typeparam name="_ResponseType">The type of receiving response messages.</typeparam>
        /// <typeparam name="_RequestType">The type of sending messages.</typeparam>
        /// <returns>duplex typed sequenced message sender</returns>
        IDuplexTypedSequencedMessageSender<_ResponseType, _RequestType> CreateDuplexTypedSequencedMessageSender<_ResponseType, _RequestType>();
        
        /// <summary>
        /// Creates the duplex typed sequences message receiver.
        /// </summary>
        /// <typeparam name="_ResponseType">The type of sending response messages.</typeparam>
        /// <typeparam name="_RequestType">The type of receiving messages.</typeparam>
        /// <returns>duplex typed sequenced message receiver</returns>
        IDuplexTypedSequencedMessageReceiver<_ResponseType, _RequestType> CreateDuplexTypedSequencedMessageReceiver<_ResponseType, _RequestType>();
    }
}
