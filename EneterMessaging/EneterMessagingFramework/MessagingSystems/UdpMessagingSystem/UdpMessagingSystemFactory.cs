/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.Threading.Dispatching;
using System;
using System.Linq;
using System.Net;

#if !COMPACT_FRAMEWORK
using System.Net.NetworkInformation;
#endif

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages via UDP.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels using UDP for sending and receiving messages.
    /// The channel id must be a valid UDP URI address. E.g.: udp://127.0.0.1:6080/. <br/>
    /// The messaging via UDP supports unicast, multicast and broadcast communication.<br/>
    /// The unicast communication is the routing of messages from one sender to one receiver.
    /// (E.g. a client-service communication where a client sends messages to one service and the service 
    /// can send response messages to one client.)<br/> 
    /// The multicast communication is the routing of messages from one sender to multiple receivers
    /// (the receivers which joined the specific multicast group and listen to the specific port).
    /// The broadcast communication is the routing of messages from one sender to all receivers within the sub-net which listen
    /// to the specific port.<br/>
    /// <br/>
    /// <example>
    /// UDP unicast communication.
    /// <code>
    /// // Create UDP input channel.
    /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory();
    /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:8043/");
    /// 
    /// // Subscribe for receiving messages.
    /// anInputChannel.MessageReceived += OnMessageReceived;
    /// 
    /// // Start listening.
    /// anInputChannel.StartListening();
    /// 
    /// ...
    /// 
    /// // Stop listening.
    /// anInputChannel.StopListening();
    /// 
    /// 
    /// // Handling of messages.
    /// private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
    /// {
    ///     // Handle incoming message.
    ///     ....
    ///     
    ///     // Send response message.
    ///     IDuplexInputChannel anInputChannel = (IDuplexInputChannel)sender;
    ///     anInputChannel.SendResponseMessage(e.ResponseReceiverId, "Hi");
    /// }
    /// </code>
    /// 
    /// <code>
    /// // Create UDP output channel.
    /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory();
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8043/");
    /// 
    /// // Subscribe to receive messages.
    /// anOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
    /// 
    /// // Open the connection.
    /// anOutputChannel.OpenConnection();
    /// 
    /// ...
    /// 
    /// // Send a message.
    /// anOutputChannel.SendMessage("Hello");
    /// 
    /// ...
    /// // Close connection.
    /// anOutputChannel.CloseConnection();
    /// 
    /// 
    /// // Handling of received message.
    /// private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
    /// {
    ///     string aMessage = (string)e.Message;
    ///     ....
    /// }
    /// </code>
    /// </example>
    /// 
    /// <exanple>
    /// UDP multicast communication.
    /// <code>
    /// // Create UDP input channel.
    /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory()
    /// {
    ///     // The communication will be multicast or broadcast.
    ///     UnicastCommunication = false,
    ///     
    ///     // The multicast group which shall be joined.
    ///     MulticastGroupToReceive = "234.5.6.7"
    /// };
    /// 
    /// // This input channel will be able to receive messages sent to udp://127.0.0.1:8043/ or
    /// // to the multicast group udp://234.5.6.7:8043/.
    /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:8043/")
    /// 
    /// 
    /// // Subscribe for receiving messages.
    /// anInputChannel.MessageReceived += OnMessageReceived;
    /// 
    /// // Start listening.
    /// anInputChannel.StartListening();
    /// 
    /// ...
    /// 
    /// // Stop listening.
    /// anInputChannel.StopListening();
    /// 
    /// 
    /// // Handling of messages.
    /// private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
    /// {
    ///     // Handle incoming message.
    ///     ....
    ///     
    ///     // Send response message.
    ///     IDuplexInputChannel anInputChannel = (IDuplexInputChannel)sender;
    ///     anInputChannel.SendResponseMessage(e.ResponseReceiverId, "Hi");
    /// }
    /// </code>
    /// 
    /// <code>
    /// // Create UDP output channel which will send messages to the multicast group udp://234.5.6.7:8043/.
    /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory()
    /// {
    ///     // The communication will be multicast or broadcast.
    ///     UnicastCommunication = false
    /// };
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://234.5.6.7:8043/");
    /// 
    /// // Subscribe to receive messages.
    /// anOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
    /// 
    /// // Open the connection.
    /// anOutputChannel.OpenConnection();
    /// 
    /// ...
    /// 
    /// // Send a message to all receivers which have joined
    /// // the multicast group udp://234.5.6.7:8043/.
    /// anOutputChannel.SendMessage("Hello");
    /// 
    /// ...
    /// // Close connection.
    /// anOutputChannel.CloseConnection();
    /// 
    /// 
    /// // Handling of received message.
    /// private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
    /// {
    ///     string aMessage = (string)e.Message;
    ///     ....
    /// }
    /// </code>
    /// </exanple>
    /// </remarks>
    public class UdpMessagingSystemFactory : IMessagingSystemFactory
    {
        private class UdpConnectorFactory : IOutputConnectorFactory, IInputConnectorFactory
        {
            public UdpConnectorFactory(IProtocolFormatter protocolFormatter, bool reuseAddressFlag, int responseReceivingPort,
                bool isUnicast,
                bool allowReceivingBroadcasts, short ttl, string multicastGroup, bool multicastLoopbackFlag)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    myReuseAddressFlag = reuseAddressFlag;
                    myResponseReceivingPort = responseReceivingPort;

                    myIsUnicastFlag = isUnicast;

                    myAllowReceivingBroadcasts = allowReceivingBroadcasts;
                    myTtl = ttl;
                    myMulticastGroup = multicastGroup;
                    myMulticastLoopbackFlag = multicastLoopbackFlag;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    IOutputConnector anOutputConnector;
                    if (!myIsUnicastFlag)
                    {
                        anOutputConnector = new UdpSessionlessOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter, myReuseAddressFlag, myTtl, myAllowReceivingBroadcasts, myMulticastGroup, myMulticastLoopbackFlag);
                    }
                    else
                    {
                        anOutputConnector = new UdpOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter, myReuseAddressFlag, myResponseReceivingPort, myTtl);
                    }

                    return anOutputConnector;
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    IInputConnector anInputConnector;
                    if (!myIsUnicastFlag)
                    {
                        anInputConnector = new UdpSessionlessInputConnector(inputConnectorAddress, myProtocolFormatter, myReuseAddressFlag, myTtl, myAllowReceivingBroadcasts, myMulticastGroup, myMulticastLoopbackFlag);
                    }
                    else
                    {
                        anInputConnector = new UdpInputConnector(inputConnectorAddress, myProtocolFormatter, myReuseAddressFlag, myTtl);
                    }

                    return anInputConnector;
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private bool myReuseAddressFlag;
            private int myResponseReceivingPort;
            private bool myIsUnicastFlag;
            private bool myAllowReceivingBroadcasts;
            private short myTtl;
            private string myMulticastGroup;
            private bool myMulticastLoopbackFlag;
        }


        /// <summary>
        /// Constructs the UDP messaging factory.
        /// </summary>
        public UdpMessagingSystemFactory()
            : this(new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the UDP messaging factory.
        /// </summary>
        /// <param name="protocolFromatter">formatter used for low-level messaging between output and input channels.</param>
        public UdpMessagingSystemFactory(IProtocolFormatter protocolFromatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFromatter;
                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;
                Ttl = 128;
                ResponseReceiverPort = -1;
                UnicastCommunication = true;
                MulticastLoopback = true;
            }
        }

        /// <summary>
        /// Creates duplex output channel which can send and receive messages from the duplex input channel using UDP.
        /// </summary>
        /// <remarks>
        /// It can create duplex output channels for unicast, multicast or broadcast communication.
        /// If the property UnicastCommunication is set to true then it creates the output channel for the unicast communication.
        /// It means it can send messages to one particular input channel and receive messages only from that input channel.
        /// If the property UnicastCommunication is set to false then it creates the output channel for mulitcast or broadcast communication.
        /// It means it can send mulitcast or broadcast messages which can be received by multiple input channels.
        /// It can also receive multicast and broadcast messages.
        /// <example>
        /// Creating the duplex output channel for unicast communication (e.g. for client-service communication).
        /// <code>
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory();
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8765/");
        /// </code>
        /// </example>
        /// <example>
        /// Creating the duplex output channel for sending mulitcast messages (e.g. for streaming video to multiple receivers).
        /// <code>
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter)
        /// {
        ///     // Setup the factory to create channels for mulitcast or broadcast communication.
        ///     UnicastCommunication = false
        /// };
        /// 
        /// // Create output channel which will send messages to the mulitcast group 234.4.5.6 on the port 8765.
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://234.4.5.6:8765/");
        /// </code>
        /// </example>
        /// <example>
        /// Creating the duplex output channel for broadcast messages.
        /// <code>
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter)
        /// {
        ///     // Setup the factory to create channels for mulitcast or broadcast communication.
        ///     UnicastCommunication = false,
        ///     
        ///     // Setup the factory to create chennels which are allowed to send broadcast messages.
        ///     AllowSendingBroadcasts = true
        /// };
        /// 
        /// // Create output channel which will send broadcast messages to the port 8765.
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://255.255.255.255:8765/");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">UDP address in a valid URI format e.g. udp://127.0.0.1:8090/</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                string aResponseReceiverId = null;

                if (!UnicastCommunication)
                {
                    aResponseReceiverId = "udp://0.0.0.0/";
                }

                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IOutputConnectorFactory aConnectorFactory = new UdpConnectorFactory(myProtocolFormatter, ReuseAddress, ResponseReceiverPort, UnicastCommunication, AllowSendingBroadcasts, Ttl, MulticastGroupToReceive, MulticastLoopback);
                return new DefaultDuplexOutputChannel(channelId, aResponseReceiverId, aDispatcher, myDispatcherAfterMessageDecoded, aConnectorFactory);
            }
        }

        /// <summary>
        /// Creates duplex output channel which can send and receive messages from the duplex input channel using UDP.
        /// </summary>
        /// <remarks>
        /// It can create duplex output channels for unicast, multicast or broadcast communication.
        /// If the property UnicastCommunication is set to true then it creates the output channel for the unicast communication.
        /// It means it can send messages to one particular input channel and receive messages only from that input channel.
        /// If the property UnicastCommunication is set to false then it creates the output channel for mulitcast or broadcast communication.
        /// It means it can send mulitcast or broadcast messages which can be received by multiple input channels.
        /// It can also receive multicast and broadcast messages.<br/>
        /// <br/>
        /// This method allows to specify the id of the created output channel.
        /// <example>
        /// Creating the duplex output channel for unicast communication (e.g. for client-service communication)
        /// with a specific unique id.
        /// <code>
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory();
        /// string aSessionId = Guid.NewGuid().ToString();
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8765/", aSessionId);
        /// </code>
        /// </example>
        /// <example>
        /// Creating the duplex output channel which can send messages to a particular UDP address and
        /// which can recieve messages on a specific UDP address and which can receive mulitcast messages.
        /// <code>
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter)
        /// {
        ///     // Setup the factory to create channels for mulitcast or broadcast communication.
        ///     UnicastCommunication = false,
        ///     
        ///     // Specify the mulitcast group to receive messages from.
        ///     MulticastGroupToReceive = "234.4.5.6"
        /// };
        /// 
        /// // Create output channel which can send messages to the input channel listening to udp://127.0.0.1:8095/
        /// // and which is listening to udp://127.0.0.1:8099/ and which can also receive messages sent for the mulitcast
        /// // group 234.4.5.6.
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8095/", "udp://127.0.0.1:8099/");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. udp://127.0.0.1:8090/ </param>
        /// <param name="responseReceiverId">
        /// Unique identifier of the output channel.<br/>
        /// In unicast communication the identifier can be a string e.g. GUID which represents the session between output and input channel.<br/>
        /// In mulitcast or broadcast communication the identifier must be a valid URI address which will be used by the output channel
        /// to receive messages from input channels.<br/>
        /// <br/>
        /// If the parameter is null then in case of unicast communication a unique id is generated automatically.
        /// In case of multicast or broadcast communication the address udp://0.0.0.0:0/ is used which means the the output channel will
        /// listen to random free port on all available IP addresses.
        /// </param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IOutputConnectorFactory aConnectorFactory = new UdpConnectorFactory(myProtocolFormatter, ReuseAddress, ResponseReceiverPort, UnicastCommunication, AllowSendingBroadcasts, Ttl, MulticastGroupToReceive, MulticastLoopback);
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, myDispatcherAfterMessageDecoded, aConnectorFactory);
            }
        }

        /// <summary>
        /// Creates the duplex input channel which can receive and send messages to the duplex output channel using UDP.
        /// </summary>
        /// <remarks>
        /// It can create duplex input channels for unicast, multicast or broadcast communication.
        /// If the property UnicastCommunication is set to true then it creates the input channel for the unicast communication.
        /// It means, like a service it can receive connections and messages from multiple output channels but
        /// send messages only to particular output channels which are connected.
        /// If the property UnicastCommunication is set to false then it creates the output channel for mulitcast or broadcast communication.
        /// It means it can send mulitcast or broadcast messages which can be received by multiple output channels.
        /// It also can receive multicast and broadcast messages.
        /// <example>
        /// Creating the duplex input channel for unicast communication.
        /// <code>
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory();
        /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:8765/");
        /// </code>
        /// </example>
        /// <example>
        /// Creating the duplex input channel for multicast communication.
        /// <code>
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter)
        /// {
        ///     // Setup the factory to create channels for mulitcast or broadcast communication.
        ///     UnicastCommunication = false,
        ///     
        ///     // Specify the mulitcast group to receive messages from.
        ///     MulticastGroupToReceive = "234.4.5.6"
        /// }
        /// 
        /// // Create duplex input channel which is listening to udp://127.0.0.1:8095/ and can also receive multicast messages
        /// // sent to udp://234.4.5.6:8095/.
        /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:8095/");
        /// </code>
        /// </example>
        /// <example>
        /// Sending mulitcast and broadcast messages from the duplex input channel.
        /// <code>
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter)
        /// {
        ///     // Setup the factory to create channels for mulitcast or broadcast communication.
        ///     UnicastCommunication = false,
        ///     
        ///     // Setup the factory to create chennels which are allowed to send broadcast messages.
        ///     AllowSendingBroadcasts = true
        /// }
        /// 
        /// // Create duplex input channel which is listening to udp://127.0.0.1:8095/.
        /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:8095/");
        /// 
        /// // Subscribe to handle messages.
        /// anIputChannel.MessageReceived += OnMessageReceived;
        /// 
        /// // Start listening.
        /// anIputChannel.StartListening();
        /// 
        /// ...
        /// 
        /// // Send a multicast message.
        /// // Note: it will be received by all output and input channels which have joined the multicast group 234.4.5.6
        /// // and are listening to the port 8095.
        /// anInputChannel.SendResponseMessage("udp://234.4.5.6:8095/", "Hello");
        /// 
        /// ...
        /// 
        /// // Send a broadcast message.
        /// // Note: it will be received by all output and input channels within the sub-network which are listening to the port 8095.
        /// anInputChannel.SendResponseMessage("udp://255.255.255.255:8095/", "Hello");
        /// 
        /// ...
        /// 
        /// // Stop listening.
        /// anInputChannel.StopListening();
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Identifies this duplex input channel. The channel id must be a valid URI address (e.g. udp://127.0.0.1:8090/) the input channel will listen to.</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();

                IInputConnectorFactory aConnectorFactory = new UdpConnectorFactory(myProtocolFormatter, ReuseAddress, -1, UnicastCommunication, AllowSendingBroadcasts, Ttl, MulticastGroupToReceive, MulticastLoopback);
                IInputConnector anInputConnector = aConnectorFactory.CreateInputConnector(channelId);
                
                return new DefaultDuplexInputChannel(channelId, aDispatcher, myDispatcherAfterMessageDecoded, anInputConnector);
            }
        }

#if (!SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81) && !COMPACT_FRAMEWORK
        
        /// <summary>
        /// Returns IP addresses assigned to the device which can be used for listening.
        /// </summary>
        /// <returns>array of available addresses</returns>
        public static string[] GetAvailableIpAddresses()
        {
            using (EneterTrace.Entering())
            {
                return TcpMessagingSystemFactory.GetAvailableIpAddresses();
            }
        }
#endif

#if !SILVERLIGHT && !COMPACT_FRAMEWORK
        /// <summary>
        /// Checks if the port is available for UDP listening.
        /// </summary>
        /// <param name="ipAddressAndPort">IP address and port.</param>
        /// <example>
        /// Check if the application can start listening on the IP address 127.0.0.1 and the port 8099.
        /// <code>
        /// bool isPortAvailable = UdpMessagingSystemFactory.IsPortAvailableForUdpListening("127.0.0.1:8099");
        /// </code>
        /// </example>
        /// <returns>true if the port is available</returns>
        public static bool IsPortAvailableForUdpListening(string ipAddressAndPort)
        {
            using (EneterTrace.Entering())
            {
                Uri aUri = new Uri(ipAddressAndPort, UriKind.Absolute);
                IPAddress anIpAddress = IPAddress.Parse(aUri.Host);

                if (!anIpAddress.Equals(IPAddress.Any) &&
                    !anIpAddress.Equals(IPAddress.IPv6Any) && !anIpAddress.Equals(IPAddress.IPv6Loopback))
                {
                    string[] anAvailableIpAddresses = GetAvailableIpAddresses();
                    if (!anAvailableIpAddresses.Contains(aUri.Host))
                    {
                        throw new InvalidOperationException("The IP address '" + aUri.Host + "' is not assigned to this device.");
                    }
                }

                IPEndPoint anEndPointToVerify = new IPEndPoint(anIpAddress, aUri.Port);

                IPGlobalProperties anIpGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

                // First check service listeners.
                IPEndPoint[] aListeners = anIpGlobalProperties.GetActiveUdpListeners();
                foreach (IPEndPoint aListener in aListeners)
                {
                    if (anEndPointToVerify.Equals(aListener))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
#endif

        /// <summary>
        /// Sets or gets whether the communication is unicast.
        /// </summary>
        /// <remarks>
        /// If true the factory will create channels for unicast communication. 
        /// The unicast is the communication between one sender and one receiver. It means if a sender sends a message it is
        /// routed to one particular receiver. The client-service communication is an example of the unicast communication.
        /// When the client sends a request message it is delivered to one service. Then when the service sends a response message
        /// it is delivered to one client.<br/>
        /// If false the factory will create channels for multicast or broadcast communication which is the communication between
        /// one sender and several receivers. It means when a sender sends a mulitcast or a broadcast message the message may be
        /// delivered to multiple receivers. E.g. in case of video streaming the sender does not send data packets individually to
        /// each receiver but it sends it just ones and routers mulitply it and deliver it to all receivers.
        /// </remarks>
        public bool UnicastCommunication { get; set; }

        /// <summary>
        /// Sets or gets time to live value for UDP datagrams.
        /// </summary>
        /// <remarks>
        /// When an UDP datagram is traveling across the network each router decreases its TTL value by one.
        /// Once the value is decreased to 0 the datagram is discarded. Therefore the TTL value specifies
        /// how many routers a datagram can traverse.<br/>
        /// E.g. if the value is set to 1 the datagram will not leave the local network.<br/>
        /// The default value is 128.
        /// </remarks>
        public short Ttl { get; set; }

        /// <summary>
        /// Sets or gets the multicast group to receive messages from.
        /// </summary>
        /// <remarks>
        /// Multicast group (multicast address) is a an IP address which is from the range 224.0.0.0 - 239.255.255.255.
        /// (The range from 224.0.0.0 to 224.0.0.255 is reserved for low-level routing protocols and you should not use it in your applications.)
        /// Rceiving messages from the mulitcast group means the communication is not unicast but mulitcast.
        /// Therefore to use this property UnicastCommunication must be set to false.
        /// <example>
        /// Creating input channel which can receive multicast messages.
        /// <code>
        /// // Create UDP messaging factory using simple protocol formatter.
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter)
        /// {
        ///     // Setup messaging factory to create channels for mulitcast or broadcast communication.
        ///     UnicastCommunication = false,
        /// 
        ///     // Set the multicast group which shall be joined for receiving messages.
        ///     aMessaging.MulticastGroupToReceive = "234.5.6.7"
        /// };
        /// 
        /// // Create input channel which will listen to udp://192.168.30.1:8043/ and which will also
        /// // receive messages from the multicast group udp://234.5.6.7:8043/.
        /// IInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://192.168.30.1:8043/");
        /// </code>
        /// </example>
        /// <example>
        /// Creating output channel which can send multicast messages.
        /// <code>
        /// // Create UDP messaging factory using simple protocol formatter.
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter)
        /// {
        ///     // Setup messaging factory to create channels for mulitcast or broadcast communication.
        ///     UnicastCommunication = false
        /// };
        /// 
        /// // Create output channel which can send messages to the multicast address udp://234.5.6.7:8043/.
        /// IOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://234.5.6.7:8043/");
        /// </code>
        /// </example>
        /// </remarks>
        public string MulticastGroupToReceive { get; set; }

#if !SILVERLIGHT
        
        /// <summary>
        /// Enables / disables sending broadcast messages.
        /// </summary>
        /// <remarks>
        /// Broadcast is a message which is routed to all devices within the sub-network.
        /// To be able to send broadcasts UnicastCommunication must be set to false.
        /// <example>
        /// Output channel which can send broadcast messages to all input channels within the sub-network
        /// which listen to the port 8055.
        /// <code>
        /// // Create UDP messaging factory using simple protocol formatter.
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter);
        /// 
        /// // Setup messaging factory to create channels for mulitcast or broadcast communication.
        /// aMessaging.UnicastCommunication = false;
        /// 
        /// // Enable sending broadcasts.
        /// aMessaging.AllowSendingBroadcasts = true;
        /// 
        /// // Create output channel which will send broadcast messages to all devices within the sub-network
        /// // which listen to the port 8055.
        /// IOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://255.255.255.255:8055/");
        /// 
        /// // Initialize output channel for sending broadcast messages and receiving responses.
        /// anOutputChannel.OpenConnection();
        /// 
        /// // Send UDP broadcast.
        /// anOutputChannel.SendMessage("Hello");
        /// 
        /// ...
        /// 
        /// // Close channel - it will release listening thread.
        /// anOutputChannel.CloseConnection();
        /// </code>
        /// </example>
        /// 
        /// <example>
        /// Input channel which can receive broadcast messages.
        /// <code>
        /// // Create UDP messaging factory using simple protocol formatter.
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter);
        /// 
        /// // Setup messaging factory to create channels for mulitcast or broadcast communication.
        /// aMessaging.UnicastCommunication = false;
        /// 
        /// // Create input channel which can receive broadcast messages to the port 8055.
        /// IInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://0.0.0.0:8055/");
        /// 
        /// // Subscribe to receive messages.
        /// anInputChannel.MessageReceived += OnMessageReceived;
        /// 
        /// // Start listening for messages.
        /// anInputChannel.StartListening();
        /// 
        /// ...
        /// 
        /// // Stop listening.
        /// anInputChannel.StopListening();
        /// </code>
        /// </example>
        /// </remarks>
        public bool AllowSendingBroadcasts { get; set; }

#if !COMPACT_FRAMEWORK
        /// <summary>
        /// Enables /disables receiving multicast messages from the same IP address from which they were sent.
        /// </summary>
        /// <remarks>
        /// <example>
        /// Sending multicast message from the output channel and receiving it by the same output channel.
        /// <code>
        ///  UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(new EasyProtocolFormatter())
        ///  {
        ///      UnicastCommunication = false,
        ///      MulticastGroupToReceive = "234.1.2.3",
        ///      MulticastLoopback = true
        ///  };
        /// 
        ///  ManualResetEvent aMessageReceived = new ManualResetEvent(false);
        ///  string aReceivedMessage = null;
        ///  IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://234.1.2.3:8090/", "udp://0.0.0.0:8090/");
        ///  anOutputChannel.ResponseMessageReceived += (x, y) =&gt;
        ///  {
        ///      aReceivedMessage = (string)y.Message;
        ///      aMessageReceived.Set();
        ///  };
        /// 
        ///  try
        ///  {
        ///      anOutputChannel.OpenConnection();
        ///      anOutputChannel.SendMessage("Hello");
        ///      aMessageReceived.WaitIfNotDebugging(1000);
        ///  }
        ///  finally
        ///  {
        ///      anOutputChannel.CloseConnection();
        ///  }
        /// 
        ///  Assert.AreEqual("Hello", aReceivedMessage);
        /// </code>
        /// </example>
        /// </remarks>
        public bool MulticastLoopback { get; set; }
#else
        private bool MulticastLoopback { get; set; }
#endif
        /// <summary>
        /// Sets or gets the port which shall be used for receiving response messages by output channel in case of unicast communication.
        /// </summary>
        /// <remarks>
        /// When a client connects an IP address and port for the unicast communication a random free port is assigned for receiving messages.
        /// This property allows to use a specieficport instead of random one.
        /// This property works only for the unicast communication.
        /// </remarks>
        public int ResponseReceiverPort { get; set; }
#else
        private bool AllowSendingBroadcasts { get; set; }

        private bool MulticastLoopback { get; set; }

        private int ResponseReceiverPort { get; set; }
#endif


        /// <summary>
        /// Factory that will create dispatchers responsible for routing events from duplex input channel according to
        /// desired threading strategy.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed by one working thread.
        /// </remarks>
        public IThreadDispatcherProvider InputChannelThreading { get; set; }

        /// <summary>
        /// Factory that will create dispatchers responsible for routing events from duplex output channel according to
        /// desired threading strategy.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed via one working thread.
        /// </remarks>
        public IThreadDispatcherProvider OutputChannelThreading { get; set; }

        /// <summary>
        /// Sets or gets the flag indicating whether the socket can be bound to the address which is already used.
        /// </summary>
        /// <remarks>
        /// If the value is true then the input channel or output channel can start listening to the IP address and port which is already in use by other channel.
        /// </remarks>
        public bool ReuseAddress { get; set; }

        private IProtocolFormatter myProtocolFormatter;
        private IThreadDispatcher myDispatcherAfterMessageDecoded = new NoDispatching().GetDispatcher();
    }
}

#endif