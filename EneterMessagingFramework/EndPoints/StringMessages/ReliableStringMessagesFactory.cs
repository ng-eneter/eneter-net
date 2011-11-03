/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// Implements the factory to create reliable string message sender and receiver.
    /// </summary>
    public class ReliableStringMessagesFactory : IReliableStringMessagesFactory
    {
        /// <summary>
        /// Creates the reliable string message sender.
        /// </summary>
        /// <returns>reliable string message sender</returns>
        public IReliableStringMessageSender CreateReliableDuplexStringMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new ReliableDuplexStringMessageSender();
            }
        }

        /// <summary>
        /// Creates the reliable string message receiver.
        /// </summary>
        /// <returns>reliable string message receiver</returns>
        public IReliableStringMessageReceiver CreateReliableDuplexStringMessageReceiver()
        {
            using (EneterTrace.Entering())
            {
                return new ReliableDuplexStringMessageReceiver();
            }
        }
    }
}
