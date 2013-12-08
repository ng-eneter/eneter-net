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
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites
{
    /// <summary>
    /// Extends the communication by the buffered messaging and the network connection monitoring.
    /// </summary>
    /// <remarks>
    /// This is the composite messaging system that consist of:
    /// <ol>
    /// <li>Buffered Messaging  --> buffering messages if disconnected (while automatically trying to reconnect)</li>
    /// <li>Monitored Messaging --> constantly monitoring the connection</li>
    /// <li>Messaging System    --> responsible for sending and receiving messages</li>
    /// </ol>
    /// The buffer stores messages if the connection is not open. The connection monitor constantly checks if the connection
    /// is established. See also <see cref="BufferedMessagingFactory"/> and <see cref="MonitoredMessagingFactory"/>.
    /// </remarks>
    public class BufferedMonitoredMessagingFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory with default settings.
        /// </summary>
        /// <remarks>
        /// The serializer for the 'ping' messages checking the connection is set to <see cref="XmlStringSerializer"/>.
        /// The maximum offline time is set to 10 seconds.
        /// The duplex output channel will check the connection with the 'ping' once per second and the response must be received within 2 seconds.
        /// Otherwise the connection is closed.<br/>
        /// The duplex input channel expects the 'ping' request at least once per 2 seconds. Otherwise the duplex output
        /// channel is disconnected.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        public BufferedMonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging)
            : this(underlyingMessaging, new XmlStringSerializer(),
                   TimeSpan.FromMilliseconds(10000), // max offline time
                   TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000))
        {
        }

        /// <summary>
        /// Constructs the factory with the specified parameters.
        /// </summary>
        /// <remarks>
        /// The maximum offline time is set to 10 seconds.
        /// The duplex output channel will check the connection with the 'ping' once per second and the response must be received within 2 seconds.
        /// Otherwise the connection is closed.<br/>
        /// The duplex input channel expects the 'ping' request at least once per 2 seconds. Otherwise the duplex output
        /// channel is disconnected.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system</param>
        /// <param name="serializer">serializer used to serialize the 'ping' requests</param>
        public BufferedMonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging, ISerializer serializer)
            : this(underlyingMessaging, serializer,
                   TimeSpan.FromMilliseconds(10000), // max offline time
                   TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000))
        {
        }

        /// <summary>
        /// Constructs the factory with the specified parameters.
        /// </summary>
        /// <param name="underlyingMessaging">underlying messaging system</param>
        /// <param name="serializer">serializer used to serialize the 'ping' requests</param>
        /// <param name="maxOfflineTime">the maximum time, the messaging can work offline. When the messaging works offline,
        /// the sent messages are buffered and the connection is being reopened. If the connection is
        /// not reopen within maxOfflineTime, the connection is closed.
        /// </param>
        /// <param name="pingFrequency">how often the connection is checked with the 'ping' requests.</param>
        /// <param name="pingResponseTimeout">the maximum time, the response for the 'ping' is expected.</param>
        public BufferedMonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging,
                                                  ISerializer serializer,

                                                  // Buffered Messaging
                                                  TimeSpan maxOfflineTime,
            
                                                  // Monitored Messaging
                                                  TimeSpan pingFrequency,
                                                  TimeSpan pingResponseTimeout)
        {
            using (EneterTrace.Entering())
            {
                IMessagingSystemFactory aMonitoredMessaging = new MonitoredMessagingFactory(underlyingMessaging, serializer, pingFrequency, pingResponseTimeout);
                myBufferedMessaging = new BufferedMessagingFactory(aMonitoredMessaging, maxOfflineTime);
            }
        }


        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// This duplex output channel provides the buffered messaging and the connection monitoring.
        /// The channel regularly checks if the connection is available. It sends 'ping' requests and expects 'ping' responses
        /// within the specified time. If the 'ping' response does not come, the buffered messaging layer is notified,
        /// that the connection was interrupted.
        /// The buffered messaging then tries to reconnect and stores sent messages to the buffer.
        /// If the connection is open, the buffered messages are sent.
        /// If the reconnection was not successful, it notifies <see cref="IDuplexOutputChannel.ConnectionClosed"/>
        /// and deletes messages from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>composit duplex output channel <see cref="ICompositeDuplexOutputChannel"/></returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myBufferedMessaging.CreateDuplexOutputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// This duplex output channel provides the buffered messaging and the connection monitoring.
        /// The channel regularly checks if the connection is available. It sends 'ping' requests and expects 'ping' responses
        /// within the specified time. If the 'ping' response does not come, the buffered messaging layer is notified,
        /// that the connection was interrupted.
        /// The buffered messaging then tries to reconnect and stores sent messages to the buffer.
        /// If the connection is open, the buffered messages are sent.
        /// If the reconnection was not successful, it notifies <see cref="IDuplexOutputChannel.ConnectionClosed"/>
        /// and deletes messages from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">the unique id of this response receiver</param>
        /// <returns>composit duplex output channel <see cref="ICompositeDuplexOutputChannel"/></returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return myBufferedMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending the response messages.
        /// </summary>
        /// <remarks>
        /// This duplex input channel provides the buffered messaging and the connection monitoring.
        /// The channel regularly checks if the duplex output channel is still connected. It expect, that every connected duplex output channel
        /// sends regularly 'ping' messages. If the 'ping' message from the duplex output channel is not received within the specified
        /// time, the duplex output channel is disconnected and the buffered messaging (as the layer above) is notified about the
        /// disconnection.
        /// The buffered messaging then puts all sent response messages to the buffer and waits whether the duplex output channel reconnects.
        /// If the duplex output channel reopens the connection, the buffered response messages are sent.
        /// If the duplex output channel does not reconnect, the event
        /// <see cref="IDuplexInputChannel.ResponseReceiverDisconnected"/> is invoked and rsponse messages are deleted from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>composit duplex input channel with the buffer <see cref="ICompositeDuplexInputChannel"/></returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myBufferedMessaging.CreateDuplexInputChannel(channelId);
            }
        }

        private IMessagingSystemFactory myBufferedMessaging;
    }
}
