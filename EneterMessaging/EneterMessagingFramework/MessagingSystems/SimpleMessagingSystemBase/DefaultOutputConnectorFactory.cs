/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultOutputConnectorFactory : IOutputConnectorFactory
    {
        public DefaultOutputConnectorFactory(IMessagingProvider messagingProvider)
        {
            using (EneterTrace.Entering())
            {
                myMessagingProvider = messagingProvider;
            }
        }

        public IOutputConnector CreateOutputConnector(string serviceConnectorAddress, string clientConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                return new DefaultOutputConnector(serviceConnectorAddress, clientConnectorAddress, myMessagingProvider);
            }
        }

        private IMessagingProvider myMessagingProvider;
    }
}
