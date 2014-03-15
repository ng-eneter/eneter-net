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

#if SILVERLIGHT
using System;
#endif

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// Implements the messaging system delivering messages via HTTP.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels using HTTP for sending and receiving messages.
    /// The channel id must be a valid URI address. E.g.: http://127.0.0.1/something/ or https://127.0.0.1/something/. <br/>
    /// Because HTTP is request-response based protocol, it does not keep the connection open.
    /// Therefore, for the bidirectional communication used by duplex channels, the polling mechanism is used.
    /// The duplex output channel regularly polls for response messages and the duplex input channel constantly measures the inactivity time
    /// to recognize whether the duplex output channel is still connected.<br/><br/>
    /// Notice, to start listening via input channel (or duplex input channel), the application must be executed with sufficient rights.
    /// Otherwise the exception will be thrown.<br/>
    /// Also notice, Silverlight and Windows Phone 7 do not support listening to HTTP requests.
    /// Therefore, only sending of messages (and receiving response messages) is possible in these platforms.
    /// </remarks>
    public class HttpMessagingSystemFactory : IMessagingSystemFactory
    {

#if !SILVERLIGHT
        private class HttpInputConnectorFactory : IInputConnectorFactory
        {
            public HttpInputConnectorFactory(int responseReceiverInactivityTimeout)
            {
                using (EneterTrace.Entering())
                {
                    myOutputConnectorInactivityTimeout = responseReceiverInactivityTimeout;
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new HttpInputConnector(inputConnectorAddress, myOutputConnectorInactivityTimeout);
                }
            }

            private int myOutputConnectorInactivityTimeout;
        }
#endif

        private class HttpOutputConnectorFactory : IOutputConnectorFactory
        {
            public HttpOutputConnectorFactory(int pollingFrequency)
            {
                using (EneterTrace.Entering())
                {
                    myPollingFrequency = pollingFrequency;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new HttpOutputConnector(inputConnectorAddress, outputConnectorAddress, myPollingFrequency);
                }
            }

            private int myPollingFrequency;
        }

#if !SILVERLIGHT
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
        /// <br/><br/>
        /// In case of Silverlight or Windows Phone 7, the response messages are recieved in the main Silverlight thread.
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
        public HttpMessagingSystemFactory(int pollingFrequency, int inactivityTimeout, IProtocolFormatter<byte[]> protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myPollingFrequency = pollingFrequency;
                myProtocolFormatter = protocolFormatter;

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;

                myInputConnectorFactory = new HttpInputConnectorFactory(inactivityTimeout);
            }
        }
#else

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
        /// <br/><br/>
        /// In case of Silverlight or Windows Phone 7, the response messages are recieved in the main Silverlight thread.
        /// </remarks>
        public HttpMessagingSystemFactory()
            : this(500, true, new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the factory that will create channel with specified settings.
        /// The response messages are received in the main silverlight thread.
        /// </summary>
        /// <param name="pollingFrequency">how often the duplex output channel polls for the pending response messages</param>
        public HttpMessagingSystemFactory(int pollingFrequency)
            : this(pollingFrequency, true, new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the factory that will create channel with specified settings.
        /// </summary>
        /// <param name="pollingFrequency">how often the duplex output channel polls for the pending response messages</param>
        /// <param name="receiveInSilverlightThread">true if the response messages shall be received in the main silverlight thread</param>
        public HttpMessagingSystemFactory(int pollingFrequency, bool receiveInSilverlightThread)
            : this(pollingFrequency, receiveInSilverlightThread, new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the factory that will create channel with specified settings.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="pollingFrequency">how often the duplex output channel polls for the pending response messages</param>
        /// <param name="receiveInSilverlightThread">true if the response messages shall be received in the main silverlight thread</param>
        /// <param name="protocolFormatter">formatter for low-level messages between duplex output channel and duplex input channel</param>
        public HttpMessagingSystemFactory(int pollingFrequency, bool receiveInSilverlightThread, IProtocolFormatter<byte[]> protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myPollingFrequency = pollingFrequency;
                myProtocolFormatter = protocolFormatter;

                if (receiveInSilverlightThread)
                {
                    OutputChannelThreading = new SilverlightDispatching();
                }
                else
                {
                    OutputChannelThreading = new NoDispatching();
                }
            }
        }
#endif

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using HTTP.
        /// The channel id must be a valid URI address e.g. http://127.0.0.1:8090/something/
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method generates the unique response receiver id automatically.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid address of the receiver. e.g. 127.0.0.1:8090</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                IOutputConnectorFactory aClientConnectorFactory = new HttpOutputConnectorFactory(myPollingFrequency);
                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, aDispatcherAfterMessageDecoded, aClientConnectorFactory, myProtocolFormatter, false);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using HTTP.
        /// The channel id must be a valid URI address e.g. http://127.0.0.1:8090/something/
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method allows to specified a desired response receiver id. Please notice, the response receiver
        /// id is supposed to be unique.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid address of the receiver. e.g. 127.0.0.1:8090</param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                IOutputConnectorFactory aClientConnectorFactory = new HttpOutputConnectorFactory(myPollingFrequency);
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, aDispatcherAfterMessageDecoded, aClientConnectorFactory, myProtocolFormatter, false);
            }
        }


        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using HTTP.
        /// The channel id must be a valid URI address e.g. http://127.0.0.1:8090/something/
        /// The method is not supported in Silverlight and Windows Phone 7.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// </remarks>
        /// <param name="channelId">channel id specifying the address the duplex input channel listens to.</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
#if !SILVERLIGHT
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();
                IInputConnector anInputConnector = myInputConnectorFactory.CreateInputConnector(channelId);
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                return new DefaultDuplexInputChannel(channelId, aDispatcher, aDispatcherAfterMessageDecoded, anInputConnector, myProtocolFormatter);
#else
                throw new NotSupportedException("Http duplex input channel is not supported in Silverlight.");
#endif
            }
        }

#if !SILVERLIGHT
        /// <summary>
        /// Factory that will create dispatchers responsible for routing events from duplex input channel according to
        /// desired threading strategy.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed by one working thread.
        /// </remarks>
        public IThreadDispatcherProvider InputChannelThreading { get; set; }
#endif

        /// <summary>
        /// Factory that will create dispatchers responsible for routing events from duplex output channel according to
        /// desired threading strategy.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed via one working thread.
        /// </remarks>
        public IThreadDispatcherProvider OutputChannelThreading { get; set; }

        /// <summary>
        /// Defines how often the client poll the server for response messages.
        /// </summary>
        private int myPollingFrequency;

        private IProtocolFormatter<byte[]> myProtocolFormatter;

#if !SILVERLIGHT
        private IInputConnectorFactory myInputConnectorFactory;
#endif

        private IThreadDispatcherProvider myDispatchingAfterMessageDecoded = new SyncDispatching();
    }
}
