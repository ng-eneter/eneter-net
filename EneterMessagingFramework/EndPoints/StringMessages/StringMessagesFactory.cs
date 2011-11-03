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
    /// The interface declares the factory to create string message senders and receivers.
    /// </summary>
    public class StringMessagesFactory : IStringMessagesFactory
    {
        /// <summary>
        /// Creates the string message sender.
        /// </summary>
        public IStringMessageSender CreateStringMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new StringMessageSender();
            }
        }

        /// <summary>
        /// Creates the string message receiver.
        /// </summary>
        public IStringMessageReceiver CreateStringMessageReceiver()
        {
            using (EneterTrace.Entering())
            {
                return new StringMessageReceiver();
            }
        }
    }
}
