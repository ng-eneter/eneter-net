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
    /// The messaging via UDP supports unicast, multicast and broadcast communication.
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
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using UDP.
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method generates the unique response receiver id automatically.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// <example>
        /// Creating the duplex output channel.
        /// <code>
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory();
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8765/");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. udp://127.0.0.1:8090/ </param>
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
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using UDP.
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
        /// <example>
        /// Creating the duplex output channel with specified client id.
        /// <code>
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory();
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8765/", "MyUniqueClientId_1");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. udp://127.0.0.1:8090/ </param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel.</param>
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
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using UDP.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// <example>
        /// Creating duplex input channel.
        /// <code>
        /// IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory();
        /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:9876/");
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

#if !MONO && !NET35 && (!SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81) && !COMPACT_FRAMEWORK
        
        /// <summary>
        /// Helper method returning IP addresses assigned to the device.
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

#if !NET35 && !SILVERLIGHT && !COMPACT_FRAMEWORK
        /// <summary>
        /// Checks if the port is available for listening.
        /// </summary>
        /// <param name="ipAddressAndPort">IP address and port.
        /// The IP address must be an address which is assigned to the local device. The method then checks whether the port
        /// is available for listening for the given IP address.
        /// </param>
        /// <returns>true if the port is available</returns>
        public static bool IsEndPointAvailableForListening(string ipAddressAndPort)
        {
            using (EneterTrace.Entering())
            {
                Uri aVerifiedUri = new Uri(ipAddressAndPort, UriKind.Absolute);
                IPEndPoint aVerifiedEndPoint = new IPEndPoint(IPAddress.Parse(aVerifiedUri.Host), aVerifiedUri.Port);

                IPGlobalProperties anIpGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

                // First check service listeners.
                IPEndPoint[] aListeners = anIpGlobalProperties.GetActiveUdpListeners();
                foreach (IPEndPoint aListener in aListeners)
                {
                    if (aVerifiedEndPoint.Equals(aListener))
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
        /// The unicast is the communication between one sender and one receiver. It means the output channel
        /// can send messages only to the input channel which listens to the specified IP address and port and then receive response messages
        /// only from this input channel.<br/>
        /// If false the factory will create channels for multicast or broadcast communication.
        /// The multicast is the communication between one sender and multiple receivers and the broadcast 
        /// is the communication between one sender and all receivers within the subnet.<br/>
        /// The default value is true.
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
        /// (The range from 224.0.0.0 to 224.0.0.255 is reserved for low-level routing protocols.)
        /// Multicast group set in MulticastGroupToReceive will be effective only if UnicastCommunication is set to false.
        /// <example>
        /// Creating input and output channels which can receive multicast messages.
        /// <code>
        /// // Create UDP messaging factory using simple protocol formatter.
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter);
        /// 
        /// // Setup messaging factory to create channels for mulitcast or broadcast communication.
        /// aMessaging.UnicastCommunication = false;
        /// 
        /// // Set the multicast group from which will be "subscribed" for receiving messages.
        /// aMessaging.MulticastGroupToReceive = "234.5.6.7";
        /// 
        /// // Create input channel which will listen to udp://192.168.30.1:8043/ and which will also
        /// // receive messages from the multicast group udp://234.5.6.7:8043/.
        /// IInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://192.168.30.1:8043/");
        /// 
        /// // Create output channel which can send messages to udp://192.168.30.1:8043/ and
        /// // which can receive multicast messages from the multicast address 234.5.6.7 and random free port.
        /// IOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://192.168.30.1:8043/");
        /// 
        /// // Create output channel which can send messages to udp://192.168.30.1:8043/ and
        /// which can receive multicast messages from the multicast address udp://234.5.6.7:7075/.
        /// IOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://192.168.30.1:8043/", "udp://0.0.0.0:7075/");
        /// </code>
        /// </example>
        /// <example>
        /// Creating output channel which can send multicast messages.
        /// <code>
        /// // Create UDP messaging factory using simple protocol formatter.
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter);
        /// 
        /// // Setup messaging factory to create channels for mulitcast or broadcast communication.
        /// aMessaging.UnicastCommunication = false;
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
        /// Input channel which can receive messages
        /// <code>
        /// // Create UDP messaging factory using simple protocol formatter.
        /// IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
        /// UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(aProtocolFormatter);
        /// 
        /// // Setup messaging factory to create channels for mulitcast or broadcast communication.
        /// aMessaging.UnicastCommunication = false;
        /// 
        /// // Create output channel which can send broadcast messages to the port 8043.
        /// IOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://255.255.255.255:8043/");
        /// </code>
        /// </example>
        /// </remarks>
        public bool AllowSendingBroadcasts { get; set; }

#if !COMPACT_FRAMEWORK
        /// <summary>
        /// Enables /disables receiving multicast messages by the same channel which sent them.
        /// </summary>
        /// <remarks>
        /// <example>
        /// Sending multicast message from the output channel and receiving it by the same channel which sent it.
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
        /// Sets or gets the port which shall be used for receiving response messages in output channels in case of unicast communication.
        /// </summary>
        /// <remarks>
        /// When a client connect an IP address and port for the unicast communication a random free port is assigned for receiving messages.
        /// This property allows to use a speciefied port instead of random one.
        /// </remarks>
        public int ResponseReceiverPort { get; set; }
#else
        private bool AllowReceivingBroadcasts { get; set; }

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
        /// If the value is true then the duplex input channel can start listening to the IP address and port which is already used by other channel.
        /// </remarks>
        public bool ReuseAddress { get; set; }

        private IProtocolFormatter myProtocolFormatter;
        private IThreadDispatcher myDispatcherAfterMessageDecoded = new NoDispatching().GetDispatcher();
    }
}

#endif