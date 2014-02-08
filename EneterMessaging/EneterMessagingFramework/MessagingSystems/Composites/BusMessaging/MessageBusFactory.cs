/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/


#if !SILVERLIGHT

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    public class MessageBusFactory : IMessageBusFactory
    {
        public MessageBusFactory()
            : this(new EneterProtocolFormatter())
        {
        }

        public MessageBusFactory(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
            }
        }

        public IMessageBus CreateMessageBus()
        {
            using (EneterTrace.Entering())
            {
                return new MessageBus(myProtocolFormatter);
            }
        }

        private IProtocolFormatter myProtocolFormatter;
    }
}

#endif