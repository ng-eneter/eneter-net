/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    /// <summary>
    /// Extends the messaging system by a buffer storing messages while the connection is not available.
    /// </summary>
    /// <remarks>
    /// The buffered messaging is intended to temporarily store sent messages while the network connection is not available.
    /// Typical scenarios are:
    /// <br/><br/>
    /// <b>Short disconnections</b><br/>
    /// The network connection is unstable and can be anytime interrupted. In case of the disconnection, the sent messages are stored
    /// in the memory buffer while the connection tries to be automatically reopen. If the reopen is successful and the connection
    /// is established, the messages are sent from the buffer.
    /// <br/><br/>
    /// <b>Independent startup order</b><br/>
    /// The communicating applications starts in undefined order and initiates the communication. In case the application receiving
    /// messages is not up, the sent messages are stored in the buffer. Then when the receiving application is running, the messages
    /// are automatically sent from the buffer.
    /// <br/>
    /// <br/>
    /// The buffering is applied for sent messages. It means, the buffering is applied for 'output channel', 'duplex output channel'
    /// and 'duplex input channe' (for sending response messages). It is not applicable for input channel.
    /// <br/><br/>
    /// <b>Note</b><br/>
    /// The buffered messaging does not require, that both communicating parts create channels with buffered messaging factory.
    /// It means, e.g. the duplex output channel created with buffered messaging with underlying TCP, can send messages
    /// directly to the duplex input channel created with just TCP messaging factory.
    /// </remarks>
    public class BufferedMessagingFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory with default parameters.
        /// </summary>
        /// <remarks>
        /// The maximum offline time will be set to 10 seconds.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        public BufferedMessagingFactory(IMessagingSystemFactory underlyingMessaging)
            : this(underlyingMessaging, TimeSpan.FromMilliseconds(10000))
        {
        }

        /// <summary>
        /// Constructs the factory from the specified parameters.
        /// </summary>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
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
        /// Creates the output channel sending messages to the input channel.
        /// </summary>
        /// <remarks>
        /// If the input channel is not available, the sent messages are stored in the buffer from where they are sent again
        /// when the input channel is available.
        /// If the message is not sent from the buffer (because the input channel is not available) within the specified offline time,
        /// the message is removed from the buffer.
        /// <br/>
        /// Note, when the message was successfully sent, it does not mean the message was delivered. It still can be lost on the way.
        /// <br/>
        /// The returned output channel is the composite channel. Therefore, if you need to reach underlying channels,
        /// you can cast it to <see cref="ICompositeOutputChannel"/>.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>composit output channel <see cref="ICompositeOutputChannel"/></returns>
        public IOutputChannel CreateOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IOutputChannel anUnderlyingOutputChannel = myUnderlyingMessaging.CreateOutputChannel(channelId);
                return new BufferedOutputChannel(anUnderlyingOutputChannel, myMaxOfflineTime);
            }
        }

        /// <summary>
        /// Creates the input channel receiving messages from the output channel.
        /// </summary>
        /// <remarks>
        /// The buffering functionality is not applicable for the input channel.
        /// Therefore, this method just uses the underlying messaging system to create the input channel.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>input channel</returns>
        public IInputChannel CreateInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myUnderlyingMessaging.CreateInputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// If the connection is not established, it puts sent messages to the buffer while trying to reconnect.
        /// Then when the connection is established, the messages are sent from the buffer.
        /// If the reconnect is not successful within the maximum offline time, it notifies <see cref="IDuplexOutputChannel.ConnectionClosed"/> and messages
        /// re deleted from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>composit duplex output channel <see cref="ICompositeDuplexOutputChannel"/> </returns>
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
        /// re deleted from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">response receiver id of this duplex output channel</param>
        /// <returns>composit duplex output channel with the buffer <see cref="ICompositeDuplexOutputChannel"/></returns>
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
        /// <see cref="IDuplexInputChannel.ResponseReceiverDisconnected"/> is invoked and rsponse messages are deleted from the buffer.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>composit duplex input channel with the buffer <see cref="ICompositeDuplexInputChannel"/></returns>
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
