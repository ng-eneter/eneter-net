

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
using Eneter.Messaging.Threading.Dispatching;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages via TCP.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels using TCP for sending and receiving messages.
    /// The channel id must be a valid URI address. E.g.: tcp://127.0.0.1:6080/.
    /// <example>
    /// Creating output and input channel for TCP messaging.
    /// <code>
    /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    /// 
    /// // Create duplex input channel which can receive messages on the address 127.0.0.1 and the port 9043
    /// // and which can send response messages to connected output channels.
    /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:9043/");
    /// 
    /// // Subscribe to handle messages.
    /// anInputChannel.MessageReceived += OnMessageReceived;
    /// 
    /// // Start listening and be able to receive messages.
    /// anInputChannel.StartListening();
    /// 
    /// ...
    /// 
    /// // Stop listening.
    /// anInputChannel.StopListeing();
    /// </code>
    /// 
    /// <code>
    /// // Create duplex output channel which can send messages to 127.0.0.1 on the port 9043 and
    /// // receive response messages.
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:9043/");
    /// 
    /// // Subscribe to handle messages.
    /// anOutputChannel.ResponseMessageReceived += OnMessageReceived;
    /// 
    /// // Open connection to the input channel which listens to tcp://127.0.0.1:9043/.
    /// anOutputChannel.OpenConnection();
    /// 
    /// ...
    /// 
    /// // Close connection.
    /// anOutputChannel.CloseConnection();
    /// </code>
    /// </example>
    /// </remarks>
    public class TcpMessagingSystemFactory : IMessagingSystemFactory
    {
        private class TcpInputConnectorFactory : IInputConnectorFactory
        {
            public TcpInputConnectorFactory(IProtocolFormatter protocolFormatter, ISecurityFactory securityFactory,
                int sendTimeout, int receiveTimeout, int sendBuffer, int receiveBuffer,
                bool reuseAddressFlag,
                int maxAmountOfConnections)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    mySecurityFactory = securityFactory;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                    mySendBuffer = sendBuffer;
                    myReceiveBuffer = receiveBuffer;
                    myReuseAddressFlag = reuseAddressFlag;
                    myMaxAmountOfConnections = maxAmountOfConnections;
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new TcpInputConnector(inputConnectorAddress, myProtocolFormatter, mySecurityFactory, mySendTimeout, myReceiveTimeout, mySendBuffer, myReceiveBuffer, myReuseAddressFlag, myMaxAmountOfConnections);
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private ISecurityFactory mySecurityFactory;
            private int mySendTimeout;
            private int myReceiveTimeout;
            private int mySendBuffer;
            private int myReceiveBuffer;
            private bool myReuseAddressFlag;
            private int myMaxAmountOfConnections;
        }

        private class TcpOutputConnectorFactory : IOutputConnectorFactory
        {
            public TcpOutputConnectorFactory(IProtocolFormatter protocolFormatter, ISecurityFactory securityFactory, int connectionTimeout, int sendTimeout, int receiveTimeout,
                int sendBuffer, int receiveBuffer,
                bool reuseAddressFlag,
                int responseReceivingPort)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    mySecurityFactory = securityFactory;
                    myConnectionTimeout = connectionTimeout;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                    mySendBuffer = sendBuffer;
                    myReceiveBuffer = receiveBuffer;
                    myReuseAddressFlag = reuseAddressFlag;
                    myResponseReceivingPort = responseReceivingPort;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new TcpOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter, mySecurityFactory,
                        myConnectionTimeout, mySendTimeout, myReceiveTimeout, mySendBuffer, myReceiveBuffer,
                        myReuseAddressFlag,
                        myResponseReceivingPort);
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private ISecurityFactory mySecurityFactory;
            private int myConnectionTimeout;
            private int mySendTimeout;
            private int myReceiveTimeout;
            private int mySendBuffer;
            private int myReceiveBuffer;
            private bool myReuseAddressFlag;
            private int myResponseReceivingPort;
        }


        /// <summary>
        /// Constructs the TCP messaging factory.
        /// </summary>
        /// <remarks>
        /// The connection and sending timeouts are sent to 30 seconds. All incoming messages will notified via one working thread.
        /// </remarks>
        public TcpMessagingSystemFactory()
            : this(new EneterProtocolFormatter())
        {
        }

        
        /// <summary>
        /// Constructs the TCP messaging factory.
        /// </summary>
        /// <param name="protocolFormatter">formats OpenConnection, CloseConnection and Message messages between channels.</param>
        public TcpMessagingSystemFactory(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;

                ConnectTimeout = TimeSpan.FromMilliseconds(30000);

                // Infinite time for sending and receiving.
                SendTimeout = TimeSpan.FromMilliseconds(0);
                ReceiveTimeout = TimeSpan.FromMilliseconds(0);

                SendBufferSize = 8192;
                ReceiveBufferSize = 8192;

                ResponseReceiverPort = -1;

                MaxAmountOfConnections = -1;

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;
            }
        }

        /// <summary>
        /// Creates duplex output channel which can send and receive messages from the duplex input channel using TCP.
        /// </summary>
        /// <remarks>
        /// <example>
        /// Creating the duplex output channel.
        /// <code>
        /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8765/");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Identifies the input channel which shall be connected. The channel id must be a valid URI address e.g. tcp://127.0.0.1:8090/ </param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IOutputConnectorFactory anOutputConnectorFactory = new TcpOutputConnectorFactory(
                    myProtocolFormatter, ClientSecurityStreamFactory,
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds,
                    SendBufferSize, ReceiveBufferSize,
                    ReuseAddress, ResponseReceiverPort);

                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, myDispatcherAfterMessageDecoded, anOutputConnectorFactory);
            }
        }

        /// <summary>
        /// Creates duplex output channel which can send and receive messages from the duplex input channel using TCP.
        /// </summary>
        /// <remarks>
        /// <example>
        /// Creating the duplex output channel with specified client id.
        /// <code>
        /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8765/", "MyUniqueClientId_1");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Identifies the input channel which shall be connected. The channel id must be a valid URI address e.g. tcp://127.0.0.1:8090/</param>
        /// <param name="responseReceiverId">Unique identifier of the output channel. If null then the id is generated automatically.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IOutputConnectorFactory anOutputConnectorFactory = new TcpOutputConnectorFactory(
                    myProtocolFormatter, ClientSecurityStreamFactory,
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds,
                    SendBufferSize, ReceiveBufferSize,
                    ReuseAddress, ResponseReceiverPort);

                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, myDispatcherAfterMessageDecoded, anOutputConnectorFactory);
            }
        }

        /// <summary>
        /// Creates the duplex input channel which can receive and send messages to the duplex output channel using TCP.
        /// </summary>
        /// <remarks>
        /// <example>
        /// Creating duplex input channel.
        /// <code>
        /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
        /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:9876/");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">The IP address and port which shall be used for listening.
        /// The channel id must be a valid URI address (e.g. tcp://127.0.0.1:8090/).<br/>
        /// If the IP address is 0.0.0.0 then it will listen to all available IP addresses.
        /// E.g. if the address is tcp://0.0.0.0:8033/ then it will listen to all available IP addresses on the port 8033.
        /// </param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();

                IInputConnectorFactory aFactory = new TcpInputConnectorFactory(
                    myProtocolFormatter,
                    ServerSecurityStreamFactory,
                    (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds, SendBufferSize, ReceiveBufferSize,
                    ReuseAddress,
                    MaxAmountOfConnections);
                IInputConnector anInputConnector = aFactory.CreateInputConnector(channelId);

                return new DefaultDuplexInputChannel(channelId, aDispatcher, myDispatcherAfterMessageDecoded, anInputConnector);
            }
        }

        /// <summary>
        /// Returns IP addresses assigned to the device which can be used for the listening.
        /// </summary>
        /// <returns>array of available addresses</returns>
        public static string[] GetAvailableIpAddresses()
        {
            using (EneterTrace.Entering())
            {
                List<string> anIpAddresses = new List<string>();
                foreach (NetworkInterface aNetworkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    IPInterfaceProperties aProperties = aNetworkInterface.GetIPProperties();
                    if (aProperties != null && aProperties.UnicastAddresses != null)
                    {
                        foreach (UnicastIPAddressInformation aUnicastIpAddress in aProperties.UnicastAddresses)
                        {
                            if (aUnicastIpAddress.Address.AddressFamily == AddressFamily.InterNetworkV6 ||
                                aUnicastIpAddress.Address.AddressFamily == AddressFamily.InterNetwork && aUnicastIpAddress.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred)
                            {
                                anIpAddresses.Add(aUnicastIpAddress.Address.ToString());
                            }
                        }
                    }
                }

                return anIpAddresses.ToArray();
            }
        }

        /// <summary>
        /// Checks if the port is available for TCP listening.
        /// </summary>
        /// <remarks>
        /// The method checks if the IP address and the port is available for TCP listenig.
        /// </remarks>
        /// <param name="ipAddressAndPort">IP address and port.</param>
        /// <example>
        /// Check if the application can start listening on the IP address 127.0.0.1 and the port 8099.
        /// <code>
        /// bool isPortAvailable = TcpMessagingSystemFactory.IsPortAvailableForTcpListening("tcp://127.0.0.1:8099/");
        /// </code>
        /// </example>
        /// <returns>true if the port is available</returns>
        public static bool IsPortAvailableForTcpListening(string ipAddressAndPort)
        {
            using (EneterTrace.Entering())
            {
                Uri aUri = new Uri(ipAddressAndPort, UriKind.Absolute);
                IPAddress anIpAddress = IPAddress.Parse(aUri.Host);

                if (!anIpAddress.Equals(IPAddress.Any))
                {
                    string[] anAvailableIpAddresses = GetAvailableIpAddresses();
                    if (!anAvailableIpAddresses.Contains(anIpAddress.ToString()))
                    {
                        throw new InvalidOperationException("The IP address '" + anIpAddress.ToString() + "' is not assigned to this device.");
                    }
                }

                IPEndPoint anEndPointToVerify = new IPEndPoint(anIpAddress, aUri.Port);

                IPGlobalProperties anIpGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

                // First check service listeners.
                IPEndPoint[] aListeners = anIpGlobalProperties.GetActiveTcpListeners();
                foreach (IPEndPoint aListener in aListeners)
                {
                    if (anEndPointToVerify.Equals(aListener))
                    {
                        return false;
                    }
                }

                // Second check response listeners.
                TcpConnectionInformation[] aConnections = anIpGlobalProperties.GetActiveTcpConnections();

                foreach (TcpConnectionInformation aConnection in aConnections)
                {
                    if (anEndPointToVerify.Equals(aConnection.LocalEndPoint))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Sets or gets the security stream factory for the server.
        /// If the factory is set, then the input channel and the duplex input channel use it to establish
        /// the secure communication.
        /// </summary>
        /// <remarks>
        /// If set to null then default NonSecurityFactory is created and used.
        /// <example>
        /// Using SSL on the service side.
        /// <code>
        /// TcpMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
        /// aMessaging.ServerSecurityStreamFactory = new ServerSslFactory(serverX509Certificate);
        /// 
        /// // Create input channel using SSL.
        /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:9876/");
        /// </code>
        /// </example>
        /// </remarks>
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
        /// <remarks>
        /// If set to null then default NonSecurityFactory is created and used.
        /// <example>
        /// Using SSL on the client side.
        /// <code>
        /// TcpMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
        /// aMessaging.ClientSecurityStreamFactory = new ClientSslFactory(hostNamePresentOnServiceCertificate);
        /// 
        /// // Create input channel using SSL.
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:9876/");
        /// </code>
        /// </example>
        /// </remarks>
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
        /// Size of the buffer in bytes for sending messages. Default value is 8192 bytes.
        /// </summary>
        public int SendBufferSize { get; set; }

        /// <summary>
        /// Size of the buffer in bytes for receiving messages. Default value is 8192 bytes.
        /// </summary>
        public int ReceiveBufferSize { get; set; }

        /// <summary>
        /// Sets or gets timeout to send a message. Default is 0 what is infinite time.
        /// </summary>
        public TimeSpan SendTimeout { get; set; }
        
        /// <summary>
        /// Sets or gets timeout to receive a message. If not received within the time the connection is closed. Default is 0 what it infinite time.
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; }

        /// <summary>
        /// Sets or gets the flag indicating whether the socket can be bound to the address which is already in use.
        /// </summary>
        /// <remarks>
        /// If the value is true then the input channels or output channels can start listening to the IP address and port which is already used by other channel.
        /// </remarks>
        public bool ReuseAddress { get; set; }


        /// <summary>
        /// Sets ot gets timeout to open the connection. Default is 30000 miliseconds. Value 0 is infinite time.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; }

        /// <summary>
        /// Sets or gets the maximum amount of connections the input channel can accept.
        /// </summary>
        /// <remarks>
        /// The default value is -1 which means the amount of connections is not restricted.<br/>
        /// If the maximum amount of connections is reached and some other client opens the TCP connection its
        /// socket is automatically closed.
        /// </remarks>
        public int MaxAmountOfConnections { get; set; }

        /// <summary>
        /// Sets or gets the threading mode for input channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed one by one via a working thread.
        /// </remarks>
        public IThreadDispatcherProvider InputChannelThreading { get; set; }

        /// <summary>
        /// Sets or gets the threading mode for output channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed one by one via a working thread.
        /// </remarks>
        public IThreadDispatcherProvider OutputChannelThreading { get; set; }


        private ISecurityFactory myServerSecurityStreamFactory = new NonSecurityFactory();
        private ISecurityFactory myClientSecurityStreamFactory = new NonSecurityFactory();
        private IProtocolFormatter myProtocolFormatter;
        private IThreadDispatcher myDispatcherAfterMessageDecoded = new NoDispatching().GetDispatcher();
    }
}
