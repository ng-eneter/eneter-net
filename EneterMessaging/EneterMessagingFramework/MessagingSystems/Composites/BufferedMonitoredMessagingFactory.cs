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
    /// This messaging combines buffered and monitored messaging.
    /// </summary>
    /// <remarks>
    /// Monitored messaging constantly monitors the connection and if the disconnection is detected the buffered messaging
    /// is notified. Buffered messaging then tries to reconnect and meanwhile stores all sent messages into a buffer.
    /// Once the connection is recovered the messages stored in the buffer are sent.<br/>
    /// <br/>
    /// The buffered monitored messaging is composed from following messagings:
    /// <ul>
    /// <li><i>BufferedMessaging</i> is on the top and is responsible for storing of messages during the disconnection (offline time)
    /// and automatic reconnect.</li>
    /// <li><i>MonitoredMessaging</i> is in the middle and is responsible for continuous monitoring of the connection.</li>
    /// <li><i>UnderlyingMessaging</i> (e.g. TCP) is on the bottom and is responsible for sending and receiving messages.</li>
    /// </ul>
    /// The following example shows how to create BufferedMonitoredMessaging:
    /// <example>
    /// <code>
    /// // Create TCP messaging system.
    /// IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();
    /// 
    /// // Create buffered monitored messaging which takes TCP as underlying messaging.
    /// IMessagingSystemFactory aMessaging = new BufferedMonitoredMessagingFactory(anUnderlyingMessaging);
    /// 
    /// // Then creating channels which can be then attached to communication components.
    /// IDuplexInputChannel anInputChannel = aMessaging.createDuplexInputChannel("tcp://127.0.0.1:8095/");
    /// IDuplexInputChannel anOutputChannel = aMessaging.createDuplexOutputChannel("tcp://127.0.0.1:8095/");
    /// </code>
    /// </example>
    /// </remarks>
    public class BufferedMonitoredMessagingFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory with default settings.
        /// </summary>
        /// <remarks>
        /// The maximum offline time for buffered messaging is set to 10 seconds.
        /// The ping frequency for monitored messaging is set to 1 second and the receive timeout for monitored messaging
        /// is set to 2 seconds.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system e.g. TCP, ...</param>
        public BufferedMonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging)
            : this(underlyingMessaging,
                   TimeSpan.FromMilliseconds(10000), // max offline time
                   TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000))
        {
        }

        /// <summary>
        /// Constructs the factory with the specified parameters.
        /// </summary>
        /// <param name="underlyingMessaging">underlying messaging system e.g. TCP</param>
        /// <param name="maxOfflineTime">the maximum time, the messaging can work offline. When the messaging works offline,
        /// the sent messages are buffered and the connection is being reopened. If the connection is
        /// not reopen within maxOfflineTime, the connection is closed.
        /// </param>
        /// <param name="pingFrequency">how often the connection is checked with the 'ping' requests.</param>
        /// <param name="pingResponseTimeout">the maximum time, the response for the 'ping' is expected.</param>
        public BufferedMonitoredMessagingFactory(IMessagingSystemFactory underlyingMessaging,

                                                  // Buffered Messaging
                                                  TimeSpan maxOfflineTime,
            
                                                  // Monitored Messaging
                                                  TimeSpan pingFrequency,
                                                  TimeSpan pingResponseTimeout)
        {
            using (EneterTrace.Entering())
            {
                myMonitoredMessaging = new MonitoredMessagingFactory(underlyingMessaging, pingFrequency, pingResponseTimeout);
                myBufferedMessaging = new BufferedMessagingFactory(myMonitoredMessaging, maxOfflineTime);
            }
        }


        /// <summary>
        /// Creates the output channel which can send and receive messages.
        /// </summary>
        /// <remarks>
        /// This duplex output channel provides the buffered messaging and the connection monitoring.
        /// It regularly checks if the connection is available. It sends 'ping' requests and expects 'ping' responses
        /// within the specified time. If the 'ping' response does not come the disconnection is notified to the buffered messaging.
        /// The buffered messaging then tries to reconnect and meanwhile stores the sent messages to the buffer.
        /// Once the connection is recovered the messages stored in the buffer are sent.
        /// If the connection recovery was not possible the event IDuplexOutputChannel.ConnectionClosed
        /// is raised the message buffer is deleted.
        /// </remarks>
        /// <param name="channelId">input channel address. The syntax must comply to underlying messaging</param>
        /// <returns>buffered and monitored duplex output channel</returns>
        public IBufferedDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myBufferedMessaging.CreateDuplexOutputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the output channel which can send and receive messages.
        /// </summary>
        /// <remarks>
        /// This duplex output channel provides the buffered messaging and the connection monitoring.
        /// It regularly checks if the connection is available. It sends 'ping' requests and expects 'ping' responses
        /// within the specified time. If the 'ping' response does not come the disconnection is notified to the buffered messaging.
        /// The buffered messaging then tries to reconnect and meanwhile stores the sent messages to the buffer.
        /// Once the connection is recovered the messages stored in the buffer are sent.
        /// If the connection recovery was not possible the event IDuplexOutputChannel.ConnectionClosed
        /// is raised the message buffer is deleted.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">the unique id of this response receiver</param>
        /// <returns>buffered and monitored duplex output channel</returns>
        public IBufferedDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return myBufferedMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
            }
        }

        /// <summary>
        /// Creates the input channel which can receive and send messages.
        /// </summary>
        /// <remarks>
        /// This duplex input channel provides the buffered messaging and the connection monitoring.
        /// It regularly checks if the duplex output channel is still connected. It expect, that every connected duplex output channel
        /// sends regularly 'ping' messages. If the 'ping' message from the duplex output channel is not received within the specified
        /// time the duplex output channel is disconnected and the buffered messaging is notified about the disconnection.
        /// The buffered messaging then puts all sent response messages to the buffer and waits whether the duplex output channel reconnects.
        /// If the duplex output channel reopens the connection the messages stored in the buffer are sent.
        /// If the duplex output channel does not reconnect the event
        /// IDuplexInputChannel.ResponseReceiverDisconnected is raised and response messages are deleted from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>buffered and monitored duplex input channel</returns>
        public IBufferedDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myBufferedMessaging.CreateDuplexInputChannel(channelId);
            }
        }

        /// <summary>
        /// Returns underlying buffered messaging.
        /// </summary>
        public BufferedMessagingFactory BufferedMessaging { get { return myBufferedMessaging; } }

        /// <summary>
        /// Returns underlying monitored messaging.
        /// </summary>
        public MonitoredMessagingFactory MonitoredMessaging { get { return myMonitoredMessaging; } }


        IDuplexOutputChannel IMessagingSystemFactory.CreateDuplexOutputChannel(string channelId)
        {
            return CreateDuplexOutputChannel(channelId);
        }

        IDuplexOutputChannel IMessagingSystemFactory.CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            return CreateDuplexOutputChannel(channelId, responseReceiverId);
        }

        IDuplexInputChannel IMessagingSystemFactory.CreateDuplexInputChannel(string channelId)
        {
            return CreateDuplexInputChannel(channelId);
        }


        private BufferedMessagingFactory myBufferedMessaging;
        private MonitoredMessagingFactory myMonitoredMessaging;
    }
}
