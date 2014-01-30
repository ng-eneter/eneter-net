/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    public class MessageBusMessagingFactory : IMessagingSystemFactory
    {
        private class MessageBusConnectorFactory : IOutputConnectorFactory, IInputConnectorFactory
        {
            public MessageBusConnectorFactory(string clientConnectingAddress, string serviceConnctingAddress,
                IMessagingSystemFactory messageBusMessaging, IProtocolFormatter protocolFormatter)
            {
                using (EneterTrace.Entering())
                {
                    myClientConnectingAddress = clientConnectingAddress;
                    myServiceConnectingAddress = serviceConnctingAddress;
                    myMessageBusMessaging = messageBusMessaging;
                }
            }

            public IOutputConnector CreateOutputConnector(string serviceConnectorAddress, string clientConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    IDuplexOutputChannel aMessageBusOutputChannel = myMessageBusMessaging.CreateDuplexOutputChannel(myClientConnectingAddress, clientConnectorAddress);
                    return new MessageBusOutputConnector(serviceConnectorAddress, clientConnectorAddress, aMessageBusOutputChannel, myProtocolFormatter);
                }
            }

            public IInputConnector CreateInputConnector(string receiverAddress)
            {
                using (EneterTrace.Entering())
                {
                    // Note: message bus service address is encoded in OpenConnectionMessage when the service connects the message bus.
                    //       Therefore receiverAddress (which is message bus service address) is used when creating output channel.
                    IDuplexOutputChannel aMessageBusOutputChannel = myMessageBusMessaging.CreateDuplexOutputChannel(myServiceConnectingAddress, receiverAddress);
                    return new MessageBusInputConnector(receiverAddress, aMessageBusOutputChannel, myProtocolFormatter);
                }
            }

            private string myClientConnectingAddress;
            private string myServiceConnectingAddress;
            private IMessagingSystemFactory myMessageBusMessaging;
            private IProtocolFormatter myProtocolFormatter;
        }


        public MessageBusMessagingFactory(string clientConnectingAddress, string serviceConnctingAddress, IMessagingSystemFactory messageBusMessaging)
            : this(clientConnectingAddress, serviceConnctingAddress, messageBusMessaging, new EneterProtocolFormatter())
        {
        }


        public MessageBusMessagingFactory(string clientConnectingAddress, string serviceConnctingAddress, IMessagingSystemFactory messageBusMessaging, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
                myConnectorFactory = new MessageBusConnectorFactory(clientConnectingAddress, serviceConnctingAddress, messageBusMessaging, protocolFormatter);

                // Dispatch events in the same thread as notified from the underlying messaging.
                myDispatcher = new NoDispatching().GetDispatcher();
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new DefaultDuplexOutputChannel(channelId, null, myDispatcher, myConnectorFactory, myProtocolFormatter, false);
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, myDispatcher, myConnectorFactory, myProtocolFormatter, false);
            }
        }

        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IInputConnector anInputConnector = myConnectorFactory.CreateInputConnector(channelId);
                return new DefaultDuplexInputChannel(channelId, myDispatcher, anInputConnector, myProtocolFormatter);
            }
        }


        private IThreadDispatcher myDispatcher;
        private MessageBusConnectorFactory myConnectorFactory;
        private IProtocolFormatter myProtocolFormatter;
    }
}
