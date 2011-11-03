/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites
{
    /// <summary>
    /// Extends the communication by reliable messaging, buffered messaging and monitored network connection.
    /// </summary>
    /// <remarks>
    /// This is the composite messaging system that consist of:
    /// <ol>
    /// <li>Reliable Messaging  --> confirmation, whether the sent messages were delivered.</li>
    /// <li>Buffered Messaging  --> stores sent messages to the buffer if disconnected</li>
    /// <li>Monitored Messaging --> constantly monitores the connection</li>
    /// <li>Messaging System    --> responsible for sending and receiving messages</li>
    /// </ol>
    /// </remarks>
    public class ReliableBufferedMonitoredMessagingFactory : IReliableMessagingFactory
    {
        /// <summary>
        /// Constructs the factory with default parameters.
        /// </summary>
        /// <remarks>
        /// The serializer for reliable messages and for 'ping' messages is set to <see cref="XmlStringSerializer"/>.
        /// The maximum time, the acknowledge message must be received is set to 11 seconds.
        /// The maximum offline time is set to 10 seconds.
        /// The duplex output channel will check the connection with the 'ping' once per second and the response must be received within 2 seconds.
        /// Otherwise the connection is closed.<br/>
        /// The duplex input channel expects the 'ping' request at least once per 2s. Otherwise the duplex output
        /// channel is disconnected.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        public ReliableBufferedMonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging)
            : this(underlyingMessaging, new XmlStringSerializer(), TimeSpan.FromMilliseconds(11000),
                   TimeSpan.FromMilliseconds(10000), // max offline time
                   TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000))
        {
        }

        /// <summary>
        /// Constructs the factory with specified parameters.
        /// </summary>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        /// <param name="serializer">serializer used to serialize acknowledge messages and 'ping' messages</param>
        /// <param name="acknowledgeTimeout">the maximum time until the delivery of the message must be acknowledged</param>
        /// <param name="maxOfflineTime">the max time, the communicating applications can be disconnected</param>
        /// <param name="pingFrequency">how often the duplex output channel pings the connection</param>
        /// <param name="pingResponseTimeout">
        /// For the duplex output channel: the maximum time, the response for the ping must be received
        /// <br/>
        /// For the duplex input channel: the maximum time within the ping for the connected duplex output channel
        /// must be received.
        /// </param>
        public ReliableBufferedMonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging,
                                                  ISerializer serializer,
                                                  TimeSpan acknowledgeTimeout,

                                                  // Buffered Messaging
                                                  TimeSpan maxOfflineTime,
            
                                                  // Monitored Messaging
                                                  TimeSpan pingFrequency,
                                                  TimeSpan pingResponseTimeout)
        {
            using (EneterTrace.Entering())
            {
                IMessagingSystemFactory aMonitoredMessaging = new MonitoredMessagingFactory(underlyingMessaging, serializer, pingFrequency, pingResponseTimeout);
                IMessagingSystemFactory aBufferedMessaging = new BufferedMessagingFactory(aMonitoredMessaging, maxOfflineTime);
                myReliableMessaging = new ReliableMessagingFactory(aBufferedMessaging, serializer, acknowledgeTimeout);
            }
        }

        /// <summary>
        /// Creates the output channel sending messages to the input channel.
        /// </summary>
        /// <remarks>
        /// This output channel provides the buffered messaging. Since the the output channel does not receive messages,
        /// and also does not maintain the open connection, the reliable messaging and also the connection monitoring are not applicable.<br/>
        /// If the input channel is not available, the sent messages are stored in the buffer from where they are sent again
        /// when the input channel is available.
        /// If the message is not sent from the buffer (because the input channel is not available) within the specified offline time,
        /// the message is removed from the buffer.
        /// <br/>
        /// Note, when the message was successfully sent, it does not mean the message was delivered. It still can be lost on the way.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>output channel</returns>
        public IOutputChannel CreateOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myReliableMessaging.CreateOutputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the input channel receiving messages from the output channel.
        /// </summary>
        /// <remarks>
        /// Since the input channel does not maintain the open connection and also does not send messages,
        /// the reliable messaging, the buffered messaging and the connection monitoring are not applicable.
        /// Therefore, the implementation of the method just uses the underlying messaging to create the input channel.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>input channel</returns>
        public IInputChannel CreateInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myReliableMessaging.CreateInputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// This duplex output channel provides the reliable messaging, the buffered messaging and the connection monitoring.
        /// The connection monitoring constantly checks, if the connection is established. If the connection is broken,
        /// it notifies the buffered messaging layer. The buffered messaging layer then stores sent messages in the buffer
        /// and tries to reconnect. If the connection is reopen, the messages from the buffer are sent. If not, it notifies,
        /// the connection was closed and messages are deleted from the buffer.<br/>
        /// The reliable messaging notifies whether the sent messages were delivered. See also <see cref="IReliableDuplexOutputChannel"/>.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>reliable duplex output channel</returns>
        public IReliableDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myReliableMessaging.CreateDuplexOutputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// This duplex output channel provides the reliable messaging, the buffered messaging and the connection monitoring.
        /// The connection monitoring constantly checks, if the connection is established. If the connection is broken,
        /// it notifies the buffered messaging layer. The buffered messaging layer then stores sent messages in the buffer
        /// and tries to reconnect. If the connection is reopen, the messages from the buffer are sent. If not, it notifies,
        /// the connection was closed and messages are deleted from the buffer.<br/>
        /// The reliable messaging notifies whether the sent messages were delivered. See also <see cref="IReliableDuplexOutputChannel"/>.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">
        /// the response receiver id used by the duplex input channel to distinguish between
        /// connected duplex output channels
        /// </param>
        /// <returns>reliable duplex output channel</returns>
        public IReliableDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return myReliableMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending the response messages.
        /// </summary>
        /// <remarks>
        /// This duplex input channel provides the reliable messaging, the buffered messaging and the connection monitoring.
        /// The connection monitoring constantly checks, if the duplex output channel is still connected. If the
        /// duplex output channel does not 'ping' anymore, the duplex output channel is disconnected and 
        /// it the buffered messaging layer is notified about the disconnection.
        /// The buffered messaging layer then stores sent response messages to the buffer and waits if the connection
        /// with the duplex output channel is established again.
        /// If the connection is reopen, the messages from the buffer are sent.
        /// If not, it notifies, the duplex output channel is disconnected and messages are deleted from the buffer.<br/>
        /// The reliable messaging notifies whether the sent response messages were delivered. See also <see cref="IReliableDuplexInputChannel"/>.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>reliable duplex input channel</returns>
        public IReliableDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myReliableMessaging.CreateDuplexInputChannel(channelId);
            }
        }


        IDuplexOutputChannel IMessagingSystemFactory.CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return this.CreateDuplexOutputChannel(channelId);
            }
        }

        IDuplexOutputChannel IMessagingSystemFactory.CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return this.CreateDuplexOutputChannel(channelId, responseReceiverId);
            }
        }

        IDuplexInputChannel IMessagingSystemFactory.CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return this.CreateDuplexInputChannel(channelId);
            }
        }

        private IReliableMessagingFactory myReliableMessaging;
    }
}
