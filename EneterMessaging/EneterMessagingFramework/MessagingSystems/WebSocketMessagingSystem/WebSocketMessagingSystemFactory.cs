/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !WINDOWS_PHONE_70

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

#if !SILVERLIGHT
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
#endif

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    /// <summary>
    /// Implements the messaging system delivering messages via websockets.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels using WebSockets for sending and receiving messages.
    /// The channel id must be a valid URI address. E.g.: ws://127.0.0.1:6080/MyService/. <br/>
    /// Notice, Silverlight and Windows Phone do not support TCP listeners and therefore WebSocket listeners are not suported too.
    /// Therefore, only sending messages (and receiving response messages) is possible on these platforms.<br/>
    /// More details:<br/>
    /// TCP in Silverlight is restricted to ports 4502 - 4532 and requires the TcpPolicyServer running on the service side.<br/>
    /// Windows Phone 7.0 does not suport TCP at all. The TCP is supported from Windows Phone 7.1. TCP in Windows Phone 7.1
    /// does not require TcpPolicyServer and is not restricted to certain ports as in Silverlight. 
    /// </remarks>
    public class WebSocketMessagingSystemFactory : IMessagingSystemFactory
    {
#if !SILVERLIGHT
        private class WebSocketInputConnectorFactory : IInputConnectorFactory
        {
            public WebSocketInputConnectorFactory(ISecurityFactory serverSecurityFactory, int sendTimeout, int receiveTimeout)
            {
                using (EneterTrace.Entering())
                {
                    myServerSecurityFactory = serverSecurityFactory;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new WebSocketInputConnector(inputConnectorAddress, myServerSecurityFactory, mySendTimeout, myReceiveTimeout);
                }
            }

            private ISecurityFactory myServerSecurityFactory;
            private int mySendTimeout;
            private int myReceiveTimeout;
        }

        private class WebSocketOutputConnectorFactory : IOutputConnectorFactory
        {
            public WebSocketOutputConnectorFactory(ISecurityFactory clientSecurityFactory, int connectionTimeout, int sendTimeout, int receiveTimeout)
            {
                using (EneterTrace.Entering())
                {
                    myClientSecurityFactory = clientSecurityFactory;
                    myConnectionTimeout = connectionTimeout;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new WebSocketOutputConnector(inputConnectorAddress, myClientSecurityFactory, myConnectionTimeout, mySendTimeout, myReceiveTimeout);
                }
            }

            private ISecurityFactory myClientSecurityFactory;
            private int myConnectionTimeout;
            private int mySendTimeout;
            private int myReceiveTimeout;
        }

#else

        private class WebSocketOutputConnectorFactory : IOutputConnectorFactory
        {
            public WebSocketOutputConnectorFactory(int connectionTimeout, int sendTimeout, int receiveTimeout)
            {
                using (EneterTrace.Entering())
                {
                    myConnectionTimeout = connectionTimeout;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new WebSocketOutputConnector(inputConnectorAddress, myConnectionTimeout, mySendTimeout, myReceiveTimeout);
                }
            }

            private int myConnectionTimeout;
            private int mySendTimeout;
            private int myReceiveTimeout;
        }
#endif        

        /// <summary>
        /// Constructs the WebSocket messaging factory.
        /// </summary>
        public WebSocketMessagingSystemFactory()
            : this(new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the WebSocket messaging factory.
        /// </summary>
        /// <param name="concurrencyMode">
        /// Specifies the threading mode for receiving messages in input channel and duplex input channel.</param>
        /// <param name="protocolFormatter">formatter used for low-level messaging between output and input channels.</param>
        public WebSocketMessagingSystemFactory(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
                ConnectTimeout = TimeSpan.FromMilliseconds(30000);
                SendTimeout = TimeSpan.FromMilliseconds(0);
                ReceiveTimeout = TimeSpan.FromMilliseconds(0);

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using WebSocket.
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
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. ws://127.0.0.1:8090/MyService/ </param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();

#if !SILVERLIGHT
                IOutputConnectorFactory aFactory = new WebSocketOutputConnectorFactory(ClientSecurityStreamFactory,
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
#else
                IOutputConnectorFactory aFactory = new WebSocketOutputConnectorFactory(
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
#endif
                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, aFactory, myProtocolFormatter, false);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using WebSocket.
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
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. ws://127.0.0.1:8090/ </param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();

#if !SILVERLIGHT
                IOutputConnectorFactory aFactory = new WebSocketOutputConnectorFactory(ClientSecurityStreamFactory,
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
#else
                IOutputConnectorFactory aFactory = new WebSocketOutputConnectorFactory(
                    (int)ConnectTimeout.TotalMilliseconds, (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
#endif
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, aFactory, myProtocolFormatter, false);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using WebSocket.
        /// The method is not supported in Silverlight and Windows Phone.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// </remarks>
        /// <param name="channelId">Identifies this duplex input channel. The channel id must be a valid URI address (e.g. ws://127.0.0.1:8090/MyService/) the input channel will listen to.</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
#if !SILVERLIGHT
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();

                IInputConnectorFactory anInputConnectorFactory = new WebSocketInputConnectorFactory(ServerSecurityStreamFactory,
                    (int)SendTimeout.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
                IInputConnector anInputConnector = anInputConnectorFactory.CreateInputConnector(channelId);

                return new DefaultDuplexInputChannel(channelId, aDispatcher, anInputConnector, myProtocolFormatter);
#else
                throw new NotSupportedException("The WebSocket duplex input channel is not supported in Silverlight.");
#endif
            }
        }

#if !SILVERLIGHT
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
#endif

        /// <summary>
        /// Sets ot gets timeout to open the connection. Default is 30000 miliseconds. Value 0 is infinite time.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; }

#if !COMPACT_FRAMEWORK
        /// <summary>
        /// Sets or gets timeout to send a message. Default is 0 what it infinite time.
        /// </summary>
        public TimeSpan SendTimeout { get; set; }

        /// <summary>
        /// Sets or gets timeout to receive a message. If not received within the time the connection is closed. Default is 0 what it infinite time.
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; }
#else
        // Dummy timeouts - Compact Framework does not support timeouts.
        private TimeSpan SendTimeout { get; set; }
        private TimeSpan ReceiveTimeout { get; set; }
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

#if !SILVERLIGHT
        private ISecurityFactory myServerSecurityStreamFactory = new NonSecurityFactory();
        private ISecurityFactory myClientSecurityStreamFactory = new NonSecurityFactory();
#endif

        private IProtocolFormatter myProtocolFormatter;
    }
}


#endif