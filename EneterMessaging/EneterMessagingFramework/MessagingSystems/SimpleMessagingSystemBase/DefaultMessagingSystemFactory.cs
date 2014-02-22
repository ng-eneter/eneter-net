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
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, myOutputConnectorFactory, myProtocolFormatter, false);
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, myOutputConnectorFactory, myProtocolFormatter, false);
            }
        }

        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();
                IInputConnector anInputConnector = myInputConnectorFactory.CreateInputConnector(channelId);
                return new DefaultDuplexInputChannel(channelId, aDispatcher, anInputConnector, myProtocolFormatter);
            }
        }


        /// <summary>
        /// Sets/gets threading mode for input channels.
        /// </summary>
        public IThreadDispatcherProvider InputChannelThreading { get; set; }

        /// <summary>
        /// Sets/gets threading mode used for input channels.
        /// </summary>
        public IThreadDispatcherProvider OutputChannelThreading { get; set; }


        private IProtocolFormatter myProtocolFormatter;

        private IOutputConnectorFactory myOutputConnectorFactory;
        private IInputConnectorFactory myInputConnectorFactory;
    }
}
