/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/


namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Creates the message bus.
    /// </summary>
    public interface IMessageBusFactory
    {
        /// <summary>
        /// Instantiates the message bus.
        /// </summary>
        /// <returns>message bus</returns>
        IMessageBus CreateMessageBus();
    }
}