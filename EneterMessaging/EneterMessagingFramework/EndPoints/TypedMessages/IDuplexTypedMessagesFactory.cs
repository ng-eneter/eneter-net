/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Creates senders and receivers of typed messages.
    /// </summary>
    public interface IDuplexTypedMessagesFactory
    {
        /// <summary>
        /// Creates duplex typed message sender that can send request messages and receive response messages of specified type.
        /// </summary>
        /// <typeparam name="TResponse">Type of response messages.</typeparam>
        /// <typeparam name="TRequest">Type of request messages.</typeparam>
        /// <returns>duplex typed message sender</returns>
        IDuplexTypedMessageSender<TResponse, TRequest> CreateDuplexTypedMessageSender<TResponse, TRequest>();

        /// <summary>
        /// Creates synchronous duplex typed message sender that sends a request message and then
        /// waits until the response message is received.
        /// </summary>
        /// <typeparam name="TResponse">Response message type.</typeparam>
        /// <typeparam name="TRequest">Request message type.</typeparam>
        /// <returns>synchronous duplex typed message sender</returns>
        ISyncDuplexTypedMessageSender<TResponse, TRequest> CreateSyncDuplexTypedMessageSender<TResponse, TRequest>();
        
        /// <summary>
        /// Creates duplex typed message receiver that can receive request messages and
        /// send back response messages of specified type.
        /// </summary>
        /// <typeparam name="TResponse">Type of response messages.</typeparam>
        /// <typeparam name="TRequest">Type of receiving messages.</typeparam>
        /// <returns>duplex typed message receiver</returns>
        IDuplexTypedMessageReceiver<TResponse, TRequest> CreateDuplexTypedMessageReceiver<TResponse, TRequest>();
    }
}
