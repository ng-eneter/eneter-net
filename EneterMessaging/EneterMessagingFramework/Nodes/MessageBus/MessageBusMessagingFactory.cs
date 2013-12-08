/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Nodes.Broker;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.Nodes.MessageBus
{
    public class MessageBusMessagingFactory : IMessagingSystemFactory
    {
        private class MessageBusConnectorFactory : IOutputConnectorFactory, IInputConnectorFactory
        {
            public MessageBusConnectorFactory(string brokerAddress, IDuplexBrokerFactory brokerFactory, IMessagingSystemFactory brokerMessaging)
            {
                using (EneterTrace.Entering())
                {
                    myBrokerAddress = brokerAddress;
                    myBrokerFactory = brokerFactory;
                    myUnderlyingBrokerMessaging = brokerMessaging;
                }
            }

            public IOutputConnector CreateOutputConnector(string serviceConnectorAddress, string clientConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    IDuplexBrokerClient aBrokerClient = myBrokerFactory.CreateBrokerClient();
                    IDuplexOutputChannel aBrokerOutputChannel = myUnderlyingBrokerMessaging.CreateDuplexOutputChannel(myBrokerAddress, clientConnectorAddress);
                    return new MessageBusOutputConnector(serviceConnectorAddress, clientConnectorAddress, aBrokerClient, aBrokerOutputChannel);
                }
            }

            public IInputConnector CreateInputConnector(string receiverAddress)
            {
                using (EneterTrace.Entering())
                {
                    IDuplexBrokerClient aBrokerClient = myBrokerFactory.CreateBrokerClient();
                    IDuplexOutputChannel aBrokerOutputChannel = myUnderlyingBrokerMessaging.CreateDuplexOutputChannel(myBrokerAddress);
                    return new MessageBusInputConnector(receiverAddress, aBrokerClient, aBrokerOutputChannel);
                }
            }

            private string myBrokerAddress;
            private IDuplexBrokerFactory myBrokerFactory;
            private IMessagingSystemFactory myUnderlyingBrokerMessaging;
        }


        public MessageBusMessagingFactory(string brokerAddress, IMessagingSystemFactory brokerMessaging)
            : this(brokerAddress, brokerMessaging, new XmlStringSerializer(), new EneterProtocolFormatter())
        {
        }

        public MessageBusMessagingFactory(string brokerAddress, IMessagingSystemFactory brokerMessaging, ISerializer serializer)
            : this(brokerAddress, brokerMessaging, serializer, new EneterProtocolFormatter())
        {
        }


        public MessageBusMessagingFactory(string brokerAddress, IMessagingSystemFactory brokerMessaging, ISerializer serializer, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
                IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory(serializer);
                myConnectorFactory = new MessageBusConnectorFactory(brokerAddress, aBrokerFactory, brokerMessaging);

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


        private IDispatcher myDispatcher;
        private MessageBusConnectorFactory myConnectorFactory;
        private IProtocolFormatter myProtocolFormatter;
    }
}
