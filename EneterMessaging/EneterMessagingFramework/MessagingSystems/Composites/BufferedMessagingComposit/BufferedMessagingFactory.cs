/*
    /// Project: Eneter.Messaging.Framework
    /// Author:  Ondrej Uzovic
    /// 
    /// Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    /// <summary>
    /// Extension allowing to work offline until the connection is available.
    /// </summary>
    /// <remarks>
    /// The buffered messaging is intended to overcome relatively short time intervals when the connection is not available.
    /// It means, the buffered messaging is able to hide the connection is not available and work offline while
    /// trying to reconnect.<br/>
    /// If the connection is not available, the buffered messaging stores sent messages (and sent response messages)
    /// in the buffer and sends them when the connection is established.<br/>
    /// Buffered messaging also checks if the between duplex output channel and duplex input channel is active.
    /// If the connection is not used (messages do not flow) the buffered messaging
    /// waits the specified maxOfflineTime and then disconnects the client.
    /// <br/>
    /// <b>Note:</b><br/>
    /// The buffered messaging does not require that both communicating parts create channels with buffered messaging factory.
    /// It means, e.g. the duplex output channel created with buffered messaging with underlying TCP, can send messages
    /// directly to the duplex input channel created with just TCP messaging factory.
    /// 
    /// <example>
    /// Simple client buffering messages in case of a disconnection.
    /// <code>
    /// // Create TCP messaging.
    /// IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();
    /// 
    /// // Create buffered messaging that internally uses TCP.
    /// IMessagingSystemFactory aMessaging = new BufferedMessagingSystemFactory(anUnderlyingMessaging);
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
    /// // Send a message. If the connection was broken the message will be stored in the buffer.
    /// // Note: The buffered messaging will try to reconnect automatically.
    /// aSender.SendMessage("Hello.");
    /// </code>
    /// </example>
    /// </remarks>
    public class BufferedMessagingFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory with default parameters.
        /// </summary>
        /// <remarks>
        /// The maximum offline time will be set to 10 seconds.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system e.g. Websocket, TCP, ...</param>
        public BufferedMessagingFactory(IMessagingSystemFactory underlyingMessaging)
            : this(underlyingMessaging, TimeSpan.FromMilliseconds(10000))
        {
        }

        /// <summary>
        /// Constructs the factory from the specified parameters.
        /// </summary>
        /// <param name="underlyingMessaging">underlying messaging system e.g. Websocket, TCP, ...</param>
        /// <param name="maxOfflineTime">the max time, the communicating applications can be disconnected</param>
        public BufferedMessagingFactory(IMessagingSystemFactory underlyingMessaging, TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingMessaging = underlyingMessaging;
                myMaxOfflineTime = maxOfflineTime;
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// If the connection is not established, it puts sent messages to the buffer while trying to reconnect.
        /// Then when the connection is established, the messages are sent from the buffer.
        /// If the reconnect is not successful within the maximum offline time, it notifies <see cref="IDuplexOutputChannel.ConnectionClosed"/> and messages
        /// are deleted from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>buffered duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexOutputChannel anUnderlyingDuplexOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId);
                return new BufferedDuplexOutputChannel(anUnderlyingDuplexOutputChannel, myMaxOfflineTime);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// If the connection is not established, it puts sent messages to the buffer while trying to reconnect.
        /// Then when the connection is established, the message are sent from the buffer.
        /// If the reconnect is not successful within the maximum offline time, it notifies <see cref="IDuplexOutputChannel.ConnectionClosed"/> and messages
        /// are deleted from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">response receiver id of this duplex output channel</param>
        /// <returns>buffered duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexOutputChannel anUnderlyingDuplexOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
                return new BufferedDuplexOutputChannel(anUnderlyingDuplexOutputChannel, myMaxOfflineTime);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending the response messages.
        /// </summary>
        /// <remarks>
        /// If the connection with the duplex output channel is not established, it puts sent response messages to the buffer.
        /// Then, when the duplex input channel is connected, the response messages are sent.
        /// If the duplex output channel does not connect within the specified maximum offline time, the event
        /// <see cref="IDuplexInputChannel.ResponseReceiverDisconnected"/> is invoked and response messages are deleted from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>buffered duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexInputChannel anUnderlyingDuplexInputChannel = myUnderlyingMessaging.CreateDuplexInputChannel(channelId);
                return new BufferedDuplexInputChannel(anUnderlyingDuplexInputChannel, myMaxOfflineTime);
            }
        }

        private IMessagingSystemFactory myUnderlyingMessaging;
        private TimeSpan myMaxOfflineTime;
    }
}
