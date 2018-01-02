/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;


namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages via HTTP.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels using HTTP for sending and receiving messages.
    /// The channel id must be a valid URI address. E.g.: http://127.0.0.1/something/ or https://127.0.0.1/something/. <br/>
    /// Because HTTP is request-response based protocol, it does not keep the connection open.
    /// Therefore, for the bidirectional communication used by duplex channels, the polling mechanism is used.
    /// The duplex output channel regularly polls for response messages and the duplex input channel constantly measures the inactivity time
    /// to recognize whether the duplex output channel is still connected.<br/><br/>
    /// Notice, to start listening via input channel (or duplex input channel), the application must be executed with sufficient rights.
    /// Otherwise the exception will be thrown.
    /// </remarks>
    public class HttpMessagingSystemFactory : IMessagingSystemFactory
    {

        private class HttpInputConnectorFactory : IInputConnectorFactory
        {
            public HttpInputConnectorFactory(IProtocolFormatter protocolFormatter, int responseReceiverInactivityTimeout)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    myOutputConnectorInactivityTimeout = responseReceiverInactivityTimeout;
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new HttpInputConnector(inputConnectorAddress, myProtocolFormatter, myOutputConnectorInactivityTimeout);
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private int myOutputConnectorInactivityTimeout;
        }


        private class HttpOutputConnectorFactory : IOutputConnectorFactory
        {
            public HttpOutputConnectorFactory(IProtocolFormatter protocolFormatter, int pollingFrequency)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    myPollingFrequency = pollingFrequency;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new HttpOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter, myPollingFrequency);
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private int myPollingFrequency;
        }


        /// <summary>
        /// Constructs the factory that will create channels with default settings. The polling
        /// frequency will be 500 ms and the inactivity timeout will be 10 minutes.
        /// </summary>
        /// <remarks>
        /// The polling frequency and the inactivity time are used only by duplex channels.
        /// The polling frequency specifies how often the duplex output channel checks if there are pending response messages.
        /// The inactivity time is measured by the duplex input channel and specifies the maximum time, the duplex output channel
        /// does not have to poll for messages.
        /// If the inactivity time is exceeded, considers the duplex output channel as disconnected.
        /// </remarks>
        public HttpMessagingSystemFactory()
            : this(500, 600000, new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the factory that will create channel with specified settings.
        /// </summary>
        /// <remarks>
        /// The polling frequency and the inactivity time are used only by duplex channels.
        /// The polling frequency specifies how often the duplex output channel checks if there are pending response messages.
        /// The inactivity time is measured by the duplex input channel and specifies the maximum time, the duplex output channel
        /// does not have to poll for messages.
        /// If the inactivity time is exceeded, considers the duplex output channel as disconnected.
        /// </remarks>
        /// <param name="pollingFrequency">how often the duplex output channel polls for the pending response messages</param>
        /// <param name="inactivityTimeout">maximum time (measured by duplex input channel), the duplex output channel does not have to poll
        /// for response messages. If the time is exceeded, the duplex output channel is considered as disconnected.
        /// </param>
        public HttpMessagingSystemFactory(int pollingFrequency, int inactivityTimeout)
            : this(pollingFrequency, inactivityTimeout, new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the factory that will create channel with specified settings.
        /// </summary>
        /// <remarks>
        /// The polling frequency and the inactivity time are used only by duplex channels.
        /// The polling frequency specifies how often the duplex output channel checks if there are pending response messages.
        /// The inactivity time is measured by the duplex input channel and specifies the maximum time, the duplex output channel
        /// does not have to poll for messages.
        /// If the inactivity time is exceeded, considers the duplex output channel as disconnected.
        /// </remarks>
        /// <param name="pollingFrequency">how often the duplex output channel polls for the pending response messages</param>
        /// <param name="inactivityTimeout">maximum time (measured by duplex input channel), the duplex output channel does not have to poll
        /// for response messages. If the time is exceeded, the duplex output channel is considered as disconnected.</param>
        /// <param name="protocolFormatter">formatter for low-level messages between duplex output channel and duplex input channel</param>
        public HttpMessagingSystemFactory(int pollingFrequency, int inactivityTimeout, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myPollingFrequency = pollingFrequency;
                myProtocolFormatter = protocolFormatter;

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;

                myInputConnectorFactory = new HttpInputConnectorFactory(protocolFormatter, inactivityTimeout);
            }
        }

        /// <summary>
        /// Creates duplex output channel which can send and receive messages from the duplex input channel using HTTP.
        /// </summary>
        /// <remarks>
        /// The channel id must be a valid URI address e.g. http://127.0.0.1:8090/something/
        /// </remarks>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid address of the receiver. e.g. 127.0.0.1:8090</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                IOutputConnectorFactory aClientConnectorFactory = new HttpOutputConnectorFactory(myProtocolFormatter, myPollingFrequency);
                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, aDispatcherAfterMessageDecoded, aClientConnectorFactory);
            }
        }

        /// <summary>
        /// Creates duplex output channel which can send and receive messages from the duplex input channel using HTTP.
        /// </summary>
        /// <remarks>
        /// The channel id must be a valid URI address e.g. http://127.0.0.1:8090/something/
        /// </remarks>
        /// <param name="channelId">Identifies the input channel which shall be connected. The channel id must be a valid URI address e.g. http://127.0.0.1:8090/</param>
        /// <param name="responseReceiverId">Unique identifier of the output channel. If null then the id is generated automatically.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                IOutputConnectorFactory aClientConnectorFactory = new HttpOutputConnectorFactory(myProtocolFormatter, myPollingFrequency);
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, aDispatcherAfterMessageDecoded, aClientConnectorFactory);
            }
        }


        /// <summary>
        /// Creates the duplex input channel which can receive and send messages to the duplex output channel using UDP.
        /// </summary>
        /// <remarks>
        /// The channel id must be a valid URI address e.g. http://127.0.0.1:8090/something/
        /// </remarks>
        /// <param name="channelId">channel id specifying the address the duplex input channel listens to.</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();
                IInputConnector anInputConnector = myInputConnectorFactory.CreateInputConnector(channelId);
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                return new DefaultDuplexInputChannel(channelId, aDispatcher, aDispatcherAfterMessageDecoded, anInputConnector);
            }
        }

        /// <summary>
        /// Sets or gets the threading mode for input channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed by one working thread.
        /// </remarks>
        public IThreadDispatcherProvider InputChannelThreading { get; set; }

        /// <summary>
        /// Sets or gets the threading mode for output channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed via one working thread.
        /// </remarks>
        public IThreadDispatcherProvider OutputChannelThreading { get; set; }

        /// <summary>
        /// Defines how often the client poll the server for response messages.
        /// </summary>
        private int myPollingFrequency;

        private IProtocolFormatter myProtocolFormatter;

        private IInputConnectorFactory myInputConnectorFactory;

        private IThreadDispatcherProvider myDispatchingAfterMessageDecoded = new SyncDispatching();
    }
}
