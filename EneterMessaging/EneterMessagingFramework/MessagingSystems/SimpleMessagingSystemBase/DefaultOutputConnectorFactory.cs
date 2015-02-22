/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultOutputConnectorFactory : IOutputConnectorFactory
    {
        public DefaultOutputConnectorFactory(IMessagingProvider messagingProvider, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myMessagingProvider = messagingProvider;
                myProtocolFormatter = protocolFormatter;
            }
        }

        public IOutputConnector CreateOutputConnector(string serviceConnectorAddress, string clientConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                return new DefaultOutputConnector(serviceConnectorAddress, clientConnectorAddress, myMessagingProvider, myProtocolFormatter);
            }
        }

        private IMessagingProvider myMessagingProvider;
        private IProtocolFormatter myProtocolFormatter;
    }
}
