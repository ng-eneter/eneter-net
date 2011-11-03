/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// The interface declares the factory to create duplex strongly typed message sender and receiver.
    /// </summary>
    public interface IDuplexTypedMessagesFactory
    {
        /// <summary>
        /// Creates duplex typed message sender.
        /// </summary>
        /// <typeparam name="_ResponseType">Type of response messages.</typeparam>
        /// <typeparam name="_RequestType">Type of request messages.</typeparam>
        /// <returns>duplex typed message sender</returns>
        IDuplexTypedMessageSender<_ResponseType, _RequestType> CreateDuplexTypedMessageSender<_ResponseType, _RequestType>();
        
        /// <summary>
        /// Creates duplex typed message receiver.
        /// </summary>
        /// <typeparam name="_ResponseType">Type of response messages.</typeparam>
        /// <typeparam name="_RequestType">Type of receiving messages.</typeparam>
        /// <returns>duplex typed message receiver</returns>
        IDuplexTypedMessageReceiver<_ResponseType, _RequestType> CreateDuplexTypedMessageReceiver<_ResponseType, _RequestType>();
    }
}
