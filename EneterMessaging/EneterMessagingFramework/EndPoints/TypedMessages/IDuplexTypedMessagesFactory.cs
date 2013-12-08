/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// The interface declares the factory that creates message senders and receivers.
    /// </summary>
    public interface IDuplexTypedMessagesFactory
    {
        /// <summary>
        /// Creates duplex typed message sender that can send request messages and receive response
        /// messages of specified type.
        /// </summary>
        /// <typeparam name="_ResponseType">Type of response messages.</typeparam>
        /// <typeparam name="_RequestType">Type of request messages.</typeparam>
        /// <returns>duplex typed message sender</returns>
        IDuplexTypedMessageSender<_ResponseType, _RequestType> CreateDuplexTypedMessageSender<_ResponseType, _RequestType>();

        /// <summary>
        /// Creates synchronous duplex typed message sender that sends a request message and then
        /// waits until the response message is received.
        /// </summary>
        /// <typeparam name="_ResponseType">Response message type.</typeparam>
        /// <typeparam name="_RequestType">Request message type.</typeparam>
        /// <returns></returns>
        ISyncDuplexTypedMessageSender<_ResponseType, _RequestType> CreateSyncDuplexTypedMessageSender<_ResponseType, _RequestType>();
        
        /// <summary>
        /// Creates duplex typed message receiver that can receive request messages and
        /// send back response messages of specified type.
        /// </summary>
        /// <typeparam name="_ResponseType">Type of response messages.</typeparam>
        /// <typeparam name="_RequestType">Type of receiving messages.</typeparam>
        /// <returns>duplex typed message receiver</returns>
        IDuplexTypedMessageReceiver<_ResponseType, _RequestType> CreateDuplexTypedMessageReceiver<_ResponseType, _RequestType>();
    }
}
