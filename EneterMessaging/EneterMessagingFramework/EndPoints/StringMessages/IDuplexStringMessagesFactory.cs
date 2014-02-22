/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// Creates sender and receiver for text messages.
    /// </summary>
    public interface IDuplexStringMessagesFactory
    {
        /// <summary>
        /// Creates message sender.
        /// </summary>
        /// <returns>string message sender</returns>
        IDuplexStringMessageSender CreateDuplexStringMessageSender();

        /// <summary>
        /// Creates message receiver.
        /// </summary>
        /// <returns>string message receiver</returns>
        IDuplexStringMessageReceiver CreateDuplexStringMessageReceiver();
    }
}
