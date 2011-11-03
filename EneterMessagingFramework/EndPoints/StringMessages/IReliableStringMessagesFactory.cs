/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// The interface declares the factory to create reliable string message sender and receiver.
    /// </summary>
    public interface IReliableStringMessagesFactory
    {
        /// <summary>
        /// Creates the reliable string message sender.
        /// </summary>
        /// <returns>reliable string message sender</returns>
        IReliableStringMessageSender CreateReliableDuplexStringMessageSender();

        /// <summary>
        /// Creates the reliable string message receiver.
        /// </summary>
        /// <returns>reliable string message receiver</returns>
        IReliableStringMessageReceiver CreateReliableDuplexStringMessageReceiver();
    }
}
