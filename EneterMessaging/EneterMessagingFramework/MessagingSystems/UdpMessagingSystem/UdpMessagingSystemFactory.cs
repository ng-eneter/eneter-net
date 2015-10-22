/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;
using System;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages via UDP.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels using UDP for sending and receiving messages.
    /// The channel id must be a valid UDP URI address. E.g.: udp://127.0.0.1:6080/. <br/>
    /// </remarks>
    public class UdpMessagingSystemFactory : IMessagingSystemFactory
    {
        private class UdpOutputConnectorFactory : IOutputConnectorFactory
        {
            public UdpOutputConnectorFactory(IProtocolFormatter protocolFormatter, bool reuseAddressFlag, int responseReceivingPort,
                bool isSessionless, short ttl, string multicastGroup)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    myReuseAddressFlag = reuseAddressFlag;
                    myResponseReceivingPort = responseReceivingPort;
                    myIsSessionlessFlag = isSessionless;
                    myTtl = ttl;
                    myMulticastGroup = multicastGroup;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    IOutputConnector anOutputConnector;
                    if (myIsSessionlessFlag)
                    {
                        anOutputConnector = new UdpSessionlessOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter, myReuseAddressFlag, myTtl, myMulticastGroup);
                    }
                    else
                    {
                        anOutputConnector = new UdpOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter, myReuseAddressFlag, myResponseReceivingPort, myTtl);
                    }

                    return anOutputConnector;
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private bool myReuseAddressFlag;
            private int myResponseReceivingPort;
            private bool myIsSessionlessFlag;
            private short myTtl;
            private string myMulticastGroup;
        }

        private class UdpInputConnectorFactory : IInputConnectorFactory
        {
            public UdpInputConnectorFactory(IProtocolFormatter protocolFormatter, bool reuseAddressFlag, bool isSessionless,
                short ttl, string multicastGroup)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    myReuseAddressFlag = reuseAddressFlag;
                    myIsSessionlessFlag = isSessionless;
                    myTtl = ttl;
                    myMulticastGroup = multicastGroup;
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    IInputConnector anInputConnector;
                    if (myIsSessionlessFlag)
                    {
                        anInputConnector = new UdpSessionlessInputConnector(inputConnectorAddress, myProtocolFormatter, myReuseAddressFlag, myTtl, myMulticastGroup);
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
            private bool myIsSessionlessFlag;
            private short myTtl;
            private string myMulticastGroup;
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

                if (IsSessionless)
                {
                    //Random aRnd = new Random();
                    //int aPort = aRnd.Next(10000, 60000);

                    aResponseReceiverId = "udp://0.0.0.0:0/";
                }

                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IOutputConnectorFactory aConnectorFactory = new UdpOutputConnectorFactory(myProtocolFormatter, ReuseAddress, -1, IsSessionless, Ttl, MulticastGroup);
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
                IOutputConnectorFactory aConnectorFactory = new UdpOutputConnectorFactory(myProtocolFormatter, ReuseAddress, -1, IsSessionless, Ttl, MulticastGroup);
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, myDispatcherAfterMessageDecoded, aConnectorFactory);
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId, int responseReceivingPort)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IOutputConnectorFactory aConnectorFactory = new UdpOutputConnectorFactory(myProtocolFormatter, ReuseAddress, responseReceivingPort, IsSessionless, Ttl, MulticastGroup);
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

                IInputConnectorFactory aConnectorFactory = new UdpInputConnectorFactory(myProtocolFormatter, ReuseAddress, IsSessionless, Ttl, MulticastGroup);
                IInputConnector anInputConnector = aConnectorFactory.CreateInputConnector(channelId);
                
                return new DefaultDuplexInputChannel(channelId, aDispatcher, myDispatcherAfterMessageDecoded, anInputConnector);
            }
        }

        public bool IsSessionless { get; set; }

        public short Ttl { get; set; }

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

        public string MulticastGroup { get; set; }

        private IProtocolFormatter myProtocolFormatter;
        private IThreadDispatcher myDispatcherAfterMessageDecoded = new NoDispatching().GetDispatcher();
    }
}

#endif