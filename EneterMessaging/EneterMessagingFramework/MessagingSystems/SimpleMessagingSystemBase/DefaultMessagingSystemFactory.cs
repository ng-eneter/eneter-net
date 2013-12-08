/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultMessagingSystemFactory : IMessagingSystemFactory
    {
        public DefaultMessagingSystemFactory(IMessagingProvider messagingProvider, IProtocolFormatter protocolFromatter)
        {
            using (EneterTrace.Entering())
            {
                myOutputConnectorFactory = new DefaultOutputConnectorFactory(messagingProvider);
                myInputConnectorFactory = new DefaultInputConnectorFactory(messagingProvider);
                myProtocolFormatter = protocolFromatter;
                
                InputChannelThreading = new NoDispatching();
                OutputChannelThreading = InputChannelThreading;
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, myOutputConnectorFactory, myProtocolFormatter, false);
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, myOutputConnectorFactory, myProtocolFormatter, false);
            }
        }

        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDispatcher aDispatcher = InputChannelThreading.GetDispatcher();
                IInputConnector anInputConnector = myInputConnectorFactory.CreateInputConnector(channelId);
                return new DefaultDuplexInputChannel(channelId, aDispatcher, anInputConnector, myProtocolFormatter);
            }
        }


        /// <summary>
        /// Factory that will create dispatchers responsible for routing events from duplex input channel according to
        /// desired threading strategy.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed by one working thread.
        /// </remarks>
        public IDispatcherProvider InputChannelThreading { get; set; }

        /// <summary>
        /// Factory that will create dispatchers responsible for routing events from duplex output channel according to
        /// desired threading strategy.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed via one working thread.
        /// </remarks>
        public IDispatcherProvider OutputChannelThreading { get; set; }


        private IProtocolFormatter myProtocolFormatter;
        private IOutputConnectorFactory myOutputConnectorFactory;
        private IInputConnectorFactory myInputConnectorFactory;
    }
}
