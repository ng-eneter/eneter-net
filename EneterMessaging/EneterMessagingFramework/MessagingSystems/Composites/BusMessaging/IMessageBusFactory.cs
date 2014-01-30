/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/


namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    public interface IMessageBusFactory
    {
        IMessageBus CreateMessageBus();
    }
}
