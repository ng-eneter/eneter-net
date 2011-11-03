/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


using Eneter.Messaging.Diagnostic;
namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// Implements the factory to create duplex string message sender and receiver.
    /// </summary>
    public class DuplexStringMessagesFactory : IDuplexStringMessagesFactory
    {
        /// <summary>
        /// Creates the duplex string message sender.
        /// </summary>
        /// <returns>duplex string message sender</returns>
        public IDuplexStringMessageSender CreateDuplexStringMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexStringMessageSender();
            }
        }

        /// <summary>
        /// Creates the duplex string message receiver.
        /// </summary>
        /// <returns>duplex string message receiver</returns>
        public IDuplexStringMessageReceiver CreateDuplexStringMessageReceiver()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexStringMessageReceiver();
            }
        }
    }
}
