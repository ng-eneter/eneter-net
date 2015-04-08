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

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    /// <summary>
    /// Extension providing the connection monitoring.
    /// </summary>
    /// <remarks>
    /// When the connection is monitored, the duplex output channel periodically sends 'ping' messages
    /// to the duplex input channel and waits for responses.
    /// If the response comes within the specified timeout, the connection is open.
    /// <br/>
    /// On the receiver side, the duplex input channel waits for the 'ping' messages and monitors if the connected
    /// duplex output channel is still alive. If the 'ping' message does not come within the specified timeout,
    /// the particular duplex output channel is disconnected.
    /// <br/><br/>
    /// Notice, the output channel and the input channel do not maintain an open connection.
    /// Therefore, the monitored messaging is not applicable for them. The implementation of this factory just uses
    /// the underlying messaging to create them.
    /// <br/><br/>
    /// <b>Note</b>
    /// Channels created by monitored messaging factory cannot communicate with channels, that were not created
    /// by monitored factory. E.g. the channel created with the monitored messaging factory with underlying TCP
    /// will not communicate with channels created directly with TCP messaging factory. The reason is, the
    /// communicating channels must understand the 'ping' communication.
    /// </remarks>
    public class MonitoredMessagingFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory with default settings.
        /// </summary>
        /// <remarks>
        /// It uses <see cref="XmlStringSerializer"/>.
        /// The duplex output channel will check the connection with the 'ping' once per second and the response must be received within 2 seconds.
        /// Otherwise the connection is closed.<br/>
        /// The duplex input channel expects the 'ping' request at least once per 3s (1s + 2s). Otherwise the duplex output
        /// channel is disconnected.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        public MonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging)
            : this(underlyingMessaging, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000))
        {
        }

        /// <summary>
        /// Constructs the factory from specified parameters.
        /// </summary>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        /// <param name="pingFrequency">how often the duplex output channel pings the connection</param>
        /// <param name="pingReceiveTimeout">
        /// For the duplex output channel: the maximum time, the response for the ping must be received
        /// <br/>
        /// For the duplex input channel: pingFrequency + pingResponseTimeout = the maximum time within the ping for the connected duplex output channel
        /// must be received.
        /// </param>
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
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// The channel also regularly checks if the connection is available. It sends 'ping' messages and expect 'ping' responses
        /// within the specified timeout. If the 'ping' response does not come within the specified timeout, the event
        /// <see cref="IDuplexOutputChannel.ConnectionClosed"/> is invoked.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>monitoring duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexOutputChannel anUnderlyingChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId);
                return new MonitoredDuplexOutputChannel(anUnderlyingChannel, Serializer, (int)PingFrequency.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// The channel also regularly checks if the connection is available. It sends 'ping' messages and expect 'ping' responses
        /// within the specified timeout. If the 'ping' response does not come within the specified timeout, the event
        /// <see cref="IDuplexOutputChannel.ConnectionClosed"/> is invoked.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">response receiver id of the channel</param>
        /// <returns>monitoring duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexOutputChannel anUnderlyingChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
                return new MonitoredDuplexOutputChannel(anUnderlyingChannel, Serializer, (int)PingFrequency.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending the response messages.
        /// </summary>
        /// <remarks>
        /// It also checks if the duplex output channel is still connected. It expect, that every connected duplex output channel
        /// sends regularly 'ping' messages. If the 'ping' message from the duplex output channel is not received within the specified
        /// timeout, the duplex output channel is disconnected. The event <see cref="IDuplexInputChannel.ResponseReceiverDisconnected"/>
        /// is invoked.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>monitoring duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexInputChannel anUnderlyingChannel = myUnderlyingMessaging.CreateDuplexInputChannel(channelId);
                return new MonitoredDuplexInputChannel(anUnderlyingChannel, Serializer, (int)PingFrequency.TotalMilliseconds, (int)ReceiveTimeout.TotalMilliseconds);
            }
        }

        public TimeSpan PingFrequency { get; set; }
        public TimeSpan ReceiveTimeout { get; set; }

        public ISerializer Serializer { get; set; }

        private IMessagingSystemFactory myUnderlyingMessaging;
    }
}
