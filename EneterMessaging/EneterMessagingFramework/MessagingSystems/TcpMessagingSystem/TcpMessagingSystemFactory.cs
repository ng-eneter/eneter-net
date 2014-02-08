/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !WINDOWS_PHONE_70

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.Threading.Dispatching;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

#if !SILVERLIGHT
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
#endif

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    /// <summary>
    /// Implements the messaging system delivering messages via TCP.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels using TCP for sending and receiving messages.
    /// The channel id must be a valid URI address. E.g.: tcp://127.0.0.1:6080/. <br/>
    /// Notice, Silverlight and Windows Phone do not support TCP listeners.
    /// Therefore, only sending of messages (and receiving response messages) is possible on these platforms.<br/>
    /// More details:<br/>
    /// TCP in Silverlight is restricted to ports 4502 - 4532 and requires the TcpPolicyServer running on the service side.<br/>
    /// Windows Phone 7.0 does not suport TCP at all. The TCP is supported from Windows Phone 7.1. TCP in Windows Phone 7.1
    /// does not require TcpPolicyServer and is not restricted to certain ports as in Silverlight. 
    /// </remarks>
    public class TcpMessagingSystemFactory : IMessagingSystemFactory
    {
#if !SILVERLIGHT
        private class TcpInputConnectorFactory : IInputConnectorFactory
        {
            public TcpInputConnectorFactory(ISecurityFactory securityFactory,
                int sendTimeout, int receiveTimeout, int sendBuffer, int receiveBuffer)
            {
                using (EneterTrace.Entering())
                {
                    mySecurityFactory = securityFactory;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                    mySendBuffer = sendBuffer;
                    myReceiveBuffer = receiveBuffer;
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new TcpInputConnector(inputConnectorAddress, mySecurityFactory, mySendTimeout, myReceiveTimeout, mySendBuffer, myReceiveBuffer);
                }
            }

            private ISecurityFactory mySecurityFactory;
            private int mySendTimeout;
            private int myReceiveTimeout;
            private int mySendBuffer;
            private int myReceiveBuffer;
        }

        private class TcpOutputConnectorFactory : IOutputConnectorFactory
        {
            public TcpOutputConnectorFactory(ISecurityFactory securityFactory, int connectionTimeout, int sendTimeout, int receiveTimeout,
                int sendBuffer, int receiveBuffer)
            {
                using (EneterTrace.Entering())
                {
                    mySecurityFactory = securityFactory;
                    myConnectionTimeout = connectionTimeout;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                    mySendBuffer = sendBuffer;
                    myReceiveBuffer = receiveBuffer;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new TcpOutputConnector(inputConnectorAddress, mySecurityFactory, myConnectionTimeout, mySendTimeout, myReceiveTimeout,
                        mySendBuffer, myReceiveBuffer);
                }
            }

            private ISecurityFactory mySecurityFactory;
            private int myConnectionTimeout;
            private int mySendTimeout;
            private int myReceiveTimeout;
            private int mySendBuffer;
            private int myReceiveBuffer;
        }
#else
        private class TcpOutputConnectorFactory : IOutputConnectorFactory
        {
            public TcpOutputConnectorFactory(int connectionTimeout, int sendTimeout, int receiveTimeout)
            {
                using (EneterTrace.Entering())
                {
                    myConnectionTimeout = connectionTimeout;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                }
            }

            public IOutputConnector CreateOutputConnector(string serviceConnectorAddress, string clientConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new TcpOutputConnector(serviceConnectorAddress, myConnectionTimeout, mySendTimeout, myReceiveTimeout);
                }
            }

            private int myConnectionTimeout;
            private int mySendTimeout;
            private int myReceiveTimeout;
        }
#endif


#if SILVERLIGHT
        /// <summary>
        /// Constructs the factory that will create output channels and duplex output channels with default settings.
        /// </summary>
        /// <remarks>
        /// The response messages will be received in the main Silverlight thread.
        /// </remarks>
        public TcpMessagingSystemFactory()
            : this(true, new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the factory that will create output channels and duplex output channels.
        /// </summary>
        /// <param name="receiveInSilverlightThread">true - if the response messages shall be received in the main Silverlight thread.</param>
        public TcpMessagingSystemFactory(bool receiveInSilverlightThread)
            : this(receiveInSilverlightThread, new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the TCP messaging factory.
        /// </summary>
        /// <param name="receiveInSilverlightThread">true - if the response messages shall be received in the main Silverlight thread.</param>
        /// <param name="protocolFormatter">formatter used for low-level messages between duplex output and duplex input channels.</param>
        public TcpMessagingSystemFactory(bool receiveInSilverlightThread, IProtocolFormatter<byte[]> protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
                ConnectTimeout = TimeSpan.FromMilliseconds(30000);
                
                // Infinite time for sending and receiving.
                // Note: do not forget to convert this 0 to -1 when using ManualResetEvent.
                SendTimeout = TimeSpan.FromMilliseconds(0);
                ReceiveTimeout = TimeSpan.FromMilliseconds(0);

                if (receiveInSilverlightThread)
                {
                    OutputChannelThreading = new SilverlightDispatching();
                }
                else
                {
                    OutputChannelThreading = new SyncDispatching();
                }
            }
        }
#endif

#if !SILVERLIGHT

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
        public TcpMessagingSystemFactory(IProtocolFormatter<byte[]> protocolFormatter)
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

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;
            }
        }
#endif

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using TCP.
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
        /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8765/");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. tcp://127.0.0.1:8090/ </param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
#if !SILVERLIGHT
                IOutputConnectorFactory anOutputConnectorFactory = new TcpOutputConnectorFactory(ClientSecurityStreamFactory,
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds,
                    SendBufferSize, ReceiveBufferSize);
#else
                IOutputConnectorFactory anOutputConnectorFactory = new TcpOutputConnectorFactory(
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
#endif
                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, anOutputConnectorFactory, myProtocolFormatter, false);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using TCP.
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
        /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8765/", "MyUniqueClientId_1");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. tcp://127.0.0.1:8090/ </param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
#if !SILVERLIGHT
                IOutputConnectorFactory anOutputConnectorFactory = new TcpOutputConnectorFactory(ClientSecurityStreamFactory,
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds,
                    SendBufferSize, ReceiveBufferSize);
#else
                IOutputConnectorFactory anOutputConnectorFactory = new TcpOutputConnectorFactory(
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
#endif
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, anOutputConnectorFactory, myProtocolFormatter, false);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using TCP.
        /// The method is not supported in Silverlight and Windows Phone.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// <example>
        /// Creating duplex input channel.
        /// <code>
        /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
        /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:9876/");
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Identifies this duplex input channel. The channel id must be a valid URI address (e.g. tcp://127.0.0.1:8090/) the input channel will listen to.</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
#if !SILVERLIGHT
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();

                IInputConnectorFactory aFactory = new TcpInputConnectorFactory(
                    ServerSecurityStreamFactory,
                    (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds, SendBufferSize, ReceiveBufferSize);
                IInputConnector anInputConnector = aFactory.CreateInputConnector(channelId);

                return new DefaultDuplexInputChannel(channelId, aDispatcher, anInputConnector, myProtocolFormatter);
#else
                throw new NotSupportedException("The Tcp duplex input channel is not supported in Silverlight.");
#endif
            }
        }


#if !SILVERLIGHT
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

        private ISecurityFactory myServerSecurityStreamFactory = new NonSecurityFactory();
        private ISecurityFactory myClientSecurityStreamFactory = new NonSecurityFactory();

#endif

#if !COMPACT_FRAMEWORK
        /// <summary>
        /// Size of the buffer in bytes for sending messages. Default value is 8192 bytes.
        /// </summary>
        public int SendBufferSize { get; set; }

        /// <summary>
        /// Size of the buffer in bytes for receiving messages. Default value is 8192 bytes.
        /// </summary>
        public int ReceiveBufferSize { get; set; }

        /// <summary>
        /// Sets or gets timeout to send a message. Default is 0 what it infinite time.
        /// </summary>
        public TimeSpan SendTimeout { get; set; }
        
        /// <summary>
        /// Sets or gets timeout to receive a message. If not received within the time the connection is closed. Default is 0 what it infinite time.
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; }
#else
        // Note: Compact framework does not support these settings in Sockets. - it throws exception.
        private int SendBufferSize { get; set; }
        private int ReceiveBufferSize { get; set; }
        private TimeSpan SendTimeout { get; set; }
        private TimeSpan ReceiveTimeout { get; set; }
#endif
        
        /// <summary>
        /// Sets ot gets timeout to open the connection. Default is 30000 miliseconds. Value 0 is infinite time.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; }

#if !SILVERLIGHT
        /// <summary>
        /// Provides thread dispatcher responsible for routing events from duplex input channel according to
        /// desired threading model.
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


        private IProtocolFormatter<byte[]> myProtocolFormatter;
    }
}

#endif