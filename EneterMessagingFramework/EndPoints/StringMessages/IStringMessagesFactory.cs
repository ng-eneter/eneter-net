/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// The interface declares the factory to create string message senders and receivers.
    /// </summary>
    public interface IStringMessagesFactory
    {
        /// <summary>
        /// Creates the string message sender.
        /// </summary>
        IStringMessageSender CreateStringMessageSender();

        /// <summary>
        /// Creates the string message receiver.
        /// </summary>
        IStringMessageReceiver CreateStringMessageReceiver();
    }
}
