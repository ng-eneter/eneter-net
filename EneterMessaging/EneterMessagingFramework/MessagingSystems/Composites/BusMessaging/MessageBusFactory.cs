/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    public class MessageBusFactory : IMessageBusFactory
    {
        IMessageBus IMessageBusFactory.CreateMessageBus()
        {
            using (EneterTrace.Entering())
            {
                return new MessageBus();
            }
        }
    }
}
