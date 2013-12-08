/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultInputConnectorFactory : IInputConnectorFactory
    {
        public DefaultInputConnectorFactory(IMessagingProvider messagingProvider)
        {
            using (EneterTrace.Entering())
            {
                myMessagingProvider = messagingProvider;
            }
        }

        public IInputConnector CreateInputConnector(string serviceConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                return new DefaultInputConnector(serviceConnectorAddress, myMessagingProvider);
            }
        }

        private IMessagingProvider myMessagingProvider;
    }
}
