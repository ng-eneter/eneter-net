/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

#if !SILVERLIGHT

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Declares factory creating the message bus.
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

#endif