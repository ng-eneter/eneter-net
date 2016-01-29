/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    /// <summary>
    /// Extension providing the connection monitoring.
    /// </summary>
    /// <remarks>
    /// The monitored messaging regularly monitors if the connection is still available.
    /// It sends ping messages and receives ping messages in a defined frequency. If sending of the ping message fails
    /// or the ping message is not received within the specified time the connection is considered broken.<br/>
    /// The advantage of the monitored messaging is that the disconnection can be detected very early.<br/>
    /// <br/>
    /// When the connection is monitored, the duplex output channel periodically sends 'ping' messages
    /// to the duplex input channel and waits for responses.
    /// If the response comes within the specified timeout, the connection is open.
    /// <br/>
    /// On the receiver side, the duplex input channel waits for the 'ping' messages and monitors if the connected
    /// duplex output channel is still alive. If the 'ping' message does not come within the specified timeout,
    /// the particular duplex output channel is disconnected.
    /// <br/><br/>
    /// <b>Note</b>
    /// Channels created by monitored messaging factory cannot communicate with channels, that were not created
    /// by monitored factory. E.g. the channel created with the monitored messaging factory with underlying TCP
    /// will not communicate with channels created directly with TCP messaging factory. The reason is, the
    /// communicating channels must understand the 'ping' communication.
    /// <example>
    /// The following example shows how to use monitored messaging:
    /// <code>
    /// // Create TCP messaging.
    /// IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();
    /// 
    /// // Create monitored messaging that internally uses TCP.
    /// IMessagingSystemFactory aMessaging = new MonitoredMessagingSystemFactory(anUnderlyingMessaging);
    /// 
    /// // Create the duplex output channel.
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8045/");
    /// 
    /// // Create message sender to send simple string messages.
    /// IDuplexStringMessagesFactory aSenderFactory = new DuplexStringMessagesFactory();
    /// IDuplexStringMessageSender aSender = aSenderFactory.CreateDuplexStringMessageSender();
    /// 
    /// // Subscribe to receive responses.
    /// aSender.ResponseReceived += OnResponseReceived;
    /// 
    /// // Attach output channel an be able to send messages and receive responses.
    /// aSender.AttachDuplexOutputChannel(anOutputChannel);
    /// 
    /// ...
    /// 
    /// // Send a message.
    /// aSender.SendMessage("Hello.");
    /// </code>
    /// </example>
    /// </remarks>
    public class MonitoredMessagingFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory with default settings.
        /// </summary>
        /// <remarks>
        /// It uses optimized custom serializer which is optimized to serialize/deserialize MonitorChannelMessage which is
        /// used for the internal communication between output and input channels.
        /// The ping message is sent once per second and it is expected the ping message is received at least once per two seconds.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        public MonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging)
            : this(underlyingMessaging, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000))
        {
        }

        /// <summary>
        /// Constructs the factory from specified parameters.
        /// </summary>
        /// <param name="underlyingMessaging">underlying messaging system e.g. Websocket, TCP, ...</param>
        /// <param name="pingFrequency">how often the ping message is sent.</param>
        /// <param name="pingReceiveTimeout">the maximum time within it the ping message must be received.</param>
        public MonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging,
                                        TimeSpan pingFrequency,
                                        TimeSpan pingReceiveTimeout)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingMessaging = underlyingMessaging;
                PingFrequency = pingFrequency;
                ReceiveTimeout = pingReceiveTimeout;
                Serializer = new MonitoredMessagingCustomSerializer();

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;
            }
        }

        /// <summary>
        /// Creates the output channel which can send messages to the input channel and receive response messages.
        /// </summary>
        /// <remarks>
        /// In addition the output channel monitors the connection availability. It sends ping messages in a specified frequency to the input channel
        /// and expects receiving ping messages within a specified time.
        /// </remarks>
        /// <param name="channelId">input channel address. It must comply to underlying messaging</param>
        /// <returns>monitoring duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexOutputChannel anUnderlyingChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId);
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new MonitoredDuplexOutputChannel(anUnderlyingChannel, Serializer, (int)PingFrequency.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds, aDispatcher);
            }
        }

        /// <summary>
        /// Creates the output channel which can send messages to the input channel and receive response messages.
        /// </summary>
        /// <remarks>
        /// In addition the output channel monitors the connection availability. It sends ping messages in a specified frequency to the input channel
        /// and expects receiving ping messages within a specified time.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">response receiver id of the channel</param>
        /// <returns>monitoring duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexOutputChannel anUnderlyingChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new MonitoredDuplexOutputChannel(anUnderlyingChannel, Serializer, (int)PingFrequency.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds, aDispatcher);
            }
        }

        /// <summary>
        /// Creates the input channel which can receive messages from the output channel and send response messages.
        /// </summary>
        /// <remarks>
        /// In addition it expects receiving ping messages from each connected client within a specified time and sends
        /// ping messages to each connected client in a specified frequency.
        /// </remarks>
        /// <param name="channelId">input channel address. It must comply to underlying messaging</param>
        /// <returns>monitoring duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexInputChannel anUnderlyingChannel = myUnderlyingMessaging.CreateDuplexInputChannel(channelId);
                IThreadDispatcher aThreadDispatcher = InputChannelThreading.GetDispatcher();
                return new MonitoredDuplexInputChannel(anUnderlyingChannel, Serializer, (int)PingFrequency.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds, aThreadDispatcher);
            }
        }

        /// <summary>
        /// How often the ping message shall be sent.
        /// </summary>
        public TimeSpan PingFrequency { get; set; }

        /// <summary>
        /// Time within it the ping message must be received.
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; }

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
        /// Serializer which shall be used to serialize MonitorChannelMessage.
        /// </summary>
        public ISerializer Serializer { get; set; }

        private IMessagingSystemFactory myUnderlyingMessaging;
    }
}
