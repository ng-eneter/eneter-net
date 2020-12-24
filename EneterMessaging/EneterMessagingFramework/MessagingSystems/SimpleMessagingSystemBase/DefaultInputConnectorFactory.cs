


using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultInputConnectorFactory : IInputConnectorFactory
    {
        public DefaultInputConnectorFactory(IMessagingProvider messagingProvider, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myMessagingProvider = messagingProvider;
                myProtocolFormatter = protocolFormatter;
            }
        }

        public IInputConnector CreateInputConnector(string inputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                return new DefaultInputConnector(inputConnectorAddress, myMessagingProvider, myProtocolFormatter);
            }
        }

        private IMessagingProvider myMessagingProvider;
        private IProtocolFormatter myProtocolFormatter;
    }
}
