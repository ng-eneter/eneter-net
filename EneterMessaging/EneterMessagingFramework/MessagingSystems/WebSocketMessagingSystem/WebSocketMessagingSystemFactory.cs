


using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
using Eneter.Messaging.Threading.Dispatching;
using System;


namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages via websockets.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels using WebSockets for sending and receiving messages.
    /// The channel id must be a valid URI address. E.g.: ws://127.0.0.1:6080/MyService/.
    /// </remarks>
    public class WebSocketMessagingSystemFactory : IMessagingSystemFactory
    {
        private class WebSocketInputConnectorFactory : IInputConnectorFactory
        {
            public WebSocketInputConnectorFactory(IProtocolFormatter protocolFormatter, ISecurityFactory serverSecurityFactory, int sendTimeout, int receiveTimeout,
                bool reuseAddressFlag,
                int maxAmountOfConnections)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    myServerSecurityFactory = serverSecurityFactory;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                    myReuseAddressFlag = reuseAddressFlag;
                    myMaxAmountOfConnections = maxAmountOfConnections;
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new WebSocketInputConnector(inputConnectorAddress, myProtocolFormatter, myServerSecurityFactory, mySendTimeout, myReceiveTimeout, myReuseAddressFlag, myMaxAmountOfConnections);
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private ISecurityFactory myServerSecurityFactory;
            private int mySendTimeout;
            private int myReceiveTimeout;
            private bool myReuseAddressFlag;
            private int myMaxAmountOfConnections;
        }

        private class WebSocketOutputConnectorFactory : IOutputConnectorFactory
        {
            public WebSocketOutputConnectorFactory(IProtocolFormatter protocolFormatter, ISecurityFactory clientSecurityFactory,
                int connectionTimeout, int sendTimeout, int receiveTimeout,
                int pingFrequency,
                int responseReceivingPort)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    myClientSecurityFactory = clientSecurityFactory;
                    myConnectionTimeout = connectionTimeout;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                    myPingFrequency = pingFrequency;
                    myResponseReceivingPort = responseReceivingPort;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new WebSocketOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter, myClientSecurityFactory,
                        myConnectionTimeout, mySendTimeout, myReceiveTimeout,
                        myPingFrequency,
                        myResponseReceivingPort);
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private ISecurityFactory myClientSecurityFactory;
            private int myConnectionTimeout;
            private int mySendTimeout;
            private int myReceiveTimeout;
            private int myPingFrequency;
            private int myResponseReceivingPort;
        }


        /// <summary>
        /// Constructs the WebSocket messaging factory.
        /// </summary>
        /// <remarks>
        /// The ping frequency is set to default value 5 minutes.
        /// The pinging is intended to keep the connection alive in
        /// environments that would drop the connection if not active for some time.
        /// </remarks>
        public WebSocketMessagingSystemFactory()
            : this(new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the WebSocket messaging factory.
        /// </summary>
        /// <param name="protocolFormatter">formatter used for low-level messaging between output and input channels.</param>
        public WebSocketMessagingSystemFactory(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
                
                ConnectTimeout = TimeSpan.FromMilliseconds(30000);

                // Infinite time for sending and receiving.
                SendTimeout = TimeSpan.FromMilliseconds(0);
                ReceiveTimeout = TimeSpan.FromMilliseconds(0);

                ResponseReceiverPort = -1;
                MaxAmountOfConnections = -1;

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;

                PingFrequency = TimeSpan.FromMilliseconds(300000);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using WebSocket.
        /// </summary>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. ws://127.0.0.1:8090/MyService/</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IOutputConnectorFactory aFactory = new WebSocketOutputConnectorFactory(myProtocolFormatter, ClientSecurityStreamFactory,
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds,
                    (int)PingFrequency.TotalMilliseconds,
                    ResponseReceiverPort);

                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, myDispatcherAfterMessageDecoded, aFactory);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using WebSocket.
        /// </summary>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. ws://127.0.0.1:8090/</param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IOutputConnectorFactory aFactory = new WebSocketOutputConnectorFactory(myProtocolFormatter, ClientSecurityStreamFactory,
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds,
                    (int)PingFrequency.TotalMilliseconds,
                    ResponseReceiverPort);

                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, myDispatcherAfterMessageDecoded, aFactory);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using WebSocket.
        /// </summary>
        /// <param name="channelId">Identifies this duplex input channel. The channel id must be a valid URI address (e.g. ws://127.0.0.1:8090/MyService/) the input channel will listen to.</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();

                IInputConnectorFactory anInputConnectorFactory = new WebSocketInputConnectorFactory(myProtocolFormatter, ServerSecurityStreamFactory,
                    (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds,
                    ReuseAddress, MaxAmountOfConnections);
                IInputConnector anInputConnector = anInputConnectorFactory.CreateInputConnector(channelId);

                return new DefaultDuplexInputChannel(channelId, aDispatcher, myDispatcherAfterMessageDecoded, anInputConnector);
            }
        }


        /// <summary>
        /// Sets or gets the security stream factory for the server.
        /// If the factory is set, then the input channel and the duplex input channel use it to establish
        /// the secure communication.
        /// </summary>
        public ISecurityFactory ServerSecurityStreamFactory
        {
            get
            {
                return myServerSecurityStreamFactory;
            }
            set
            {
                myServerSecurityStreamFactory = (value != null) ? value : new NonSecurityFactory();
            }
        }

        /// <summary>
        /// Sets and gets the security stream factory for the client.
        /// If the factory is set, then the output channel and the duplex output channel use it to establish
        /// the secure communication.
        /// </summary>
        public ISecurityFactory ClientSecurityStreamFactory
        {
            get
            {
                return myClientSecurityStreamFactory;
            }
            set
            {
                myClientSecurityStreamFactory = (value != null) ? value : new NonSecurityFactory();
            }
        }

        /// <summary>
        /// Sets or gets the port which shall be used for receiving response messages in output channels.
        /// </summary>
        /// <remarks>
        /// When a client connects an IP address and port a random free port is assigned for receiving messages.
        /// This property allows to use a specific port instead of random one.<br/>
        /// <br/>
        /// Default value is -1 which means a random free port is chosen for receiving response messages.
        /// </remarks>
        public int ResponseReceiverPort { get; set; }

        /// <summary>
        /// Sets ot gets timeout to open the connection. Default is 30000 miliseconds. Value 0 is infinite time.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; }

        /// <summary>
        /// Sets or gets timeout to send a message. Default is 0 what it infinite time.
        /// </summary>
        public TimeSpan SendTimeout { get; set; }

        /// <summary>
        /// Sets or gets timeout to receive a message. If not received within the time the connection is closed. Default is 0 what it infinite time.
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; }

        /// <summary>
        /// Sets or gets the flag indicating whether the socket can be bound to the address which is already used.
        /// </summary>
        /// <remarks>
        /// If the value is true then the duplex input channel can start listening to the IP address and port which is already used by other channel.
        /// </remarks>
        public bool ReuseAddress { get; set; }

        /// <summary>
        /// Sets or gets the maximum amount of connections the input channel can handle.
        /// </summary>
        /// <remarks>
        /// The default value is -1 which means the amount of connections is not restrected.
        /// </remarks>
        public int MaxAmountOfConnections { get; set; }


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
        /// Ping frequency.
        /// </summary>
        /// <remarks>
        /// The pinging is intended to keep the connection alive in
        /// environments that would drop the connection if not active for some time.
        /// </remarks>
        public TimeSpan PingFrequency { get; set; }


        private ISecurityFactory myServerSecurityStreamFactory = new NonSecurityFactory();

        private ISecurityFactory myClientSecurityStreamFactory = new NonSecurityFactory();

        private IProtocolFormatter myProtocolFormatter;
        private IThreadDispatcher myDispatcherAfterMessageDecoded = new NoDispatching().GetDispatcher();
    }
}