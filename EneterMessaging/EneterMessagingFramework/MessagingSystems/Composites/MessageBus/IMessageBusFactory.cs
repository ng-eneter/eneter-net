/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

#if !SILVERLIGHT

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    public interface IMessageBusFactory
    {
        IMessageBus CreateMessageBus();
    }
}

#endif