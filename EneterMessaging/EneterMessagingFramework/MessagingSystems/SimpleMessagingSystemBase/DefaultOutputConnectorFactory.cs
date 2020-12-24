


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

        public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                return new DefaultOutputConnector(inputConnectorAddress, outputConnectorAddress, myMessagingProvider, myProtocolFormatter);
            }
        }

        private IMessagingProvider myMessagingProvider;
        private IProtocolFormatter myProtocolFormatter;
    }
}
