/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// The interface declares the factory to create duplex string message sender and receiver.
    /// </summary>
    public interface IDuplexStringMessagesFactory
    {
        /// <summary>
        /// Creates the duplex string message sender.
        /// </summary>
        /// <returns>duplex string message sender</returns>
        IDuplexStringMessageSender CreateDuplexStringMessageSender();

        /// <summary>
        /// Creates the duplex string message receiver.
        /// </summary>
        /// <returns>duplex string message receiver</returns>
        IDuplexStringMessageReceiver CreateDuplexStringMessageReceiver();
    }
}
