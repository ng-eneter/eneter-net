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
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites
{
    /// <summary>
    /// Extends the communication by reliable messaging and buffered messaging.
    /// </summary>
    /// <remarks>
    /// This is the composite messaging system that consist of:
    /// <ol>
    /// <li>Reliable Messaging  --> confirms, the sent message was delivered</li>
    /// <li>Buffered Messaging  --> buffering messages if disconnected (while automatically trying to reconnect)</li>
    /// <li>Messaging System    --> responsible for sending and receiving messages</li>
    /// </ol>
    /// </remarks>
    public class ReliableBufferedMessagingFactory : IReliableMessagingFactory
    {
        /// <summary>
        /// Constructs the factory with default settings.
        /// </summary>
        /// <remarks>
        /// The serializer for the reliable messages is set to <see cref="XmlStringSerializer"/>.
        /// The maximum time, the acknowledge message must be received is set to 11 seconds.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        public ReliableBufferedMessagingFactory(IMessagingSystemFactory underlyingMessaging)
            : this(underlyingMessaging, new XmlStringSerializer(), TimeSpan.FromMilliseconds(11000),
                   TimeSpan.FromMilliseconds(10000)) // max offline time
        {
        }


        /// <summary>
        /// Constructs the factory with specified parameters.
        /// </summary>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        /// <param name="serializer">serializer used to serialize the acknowledge communication</param>
        /// <param name="acknowledgeTimeout">the maximum time, until the acknowledge message is expected. If the time is exceeded
        /// it will be notified, the message was not delivered.</param>
        /// <param name="maxOfflineTime">the max time, the communicating applications can be disconnected</param>
        public ReliableBufferedMessagingFactory(IMessagingSystemFactory underlyingMessaging,
                                                  ISerializer serializer,
                                                  TimeSpan acknowledgeTimeout,

                                                  // Buffered Messaging
                                                  TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                IMessagingSystemFactory aBufferedMessaging = new BufferedMessagingFactory(underlyingMessaging, maxOfflineTime);
                myReliableMessaging = new ReliableMessagingFactory(aBufferedMessaging, serializer, acknowledgeTimeout);
            }
        }

        /// <summary>
        /// Creates the output channel sending messages to the input channel.
        /// </summary>
        /// <remarks>
        /// This output channel provides the buffered messaging. Since the the output channel does not receive messages,
        /// the reliable messaging is not applicable.<br/>
        /// If the input channel is not available, the sent messages are stored in the buffer from where they are sent again
        /// when the input channel is available.
        /// If the message is not sent from the buffer (because the input channel is not available) within the specified offline time,
        /// the message is removed from the buffer.
        /// <br/>
        /// Note, when the message was successfully sent, it does not mean the message was delivered. It still can be lost on the way.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>composit output channel <see cref="ICompositeOutputChannel"/></returns>
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
        /// The buffering as well as reliable messages are not applicable.
        /// This method just uses the underlying messaging system to create the input channel.
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
        /// Creates the reliable duplex output channel that can send messages and receive response messages from <see cref="IReliableDuplexInputChannel"/>.
        /// </summary>
        /// <remarks>
        /// This duplex output channel provides the reliable messaging and the buffered messaging.
        /// If the connection is not established, it puts sent messages to the buffer while trying to reconnect.
        /// If the reliable duplex input channel receives the message, it sends back the acknowledge message.
        /// When the reliable duplex output channel receives the acknowledge message it invokes <see cref="IReliableDuplexOutputChannel.MessageDelivered"/>.
        /// If the acknowledge message is not delivered until the specified time, it invokes <see cref="IReliableDuplexOutputChannel.MessageNotDelivered"/>.
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
        /// Creates the reliable duplex output channel that can send messages and receive response messages from <see cref="IReliableDuplexInputChannel"/>.
        /// </summary>
        /// <remarks>
        /// This duplex output channel provides the reliable messaging and the buffered messaging.
        /// If the connection is not established, it puts sent messages to the buffer while trying to reconnect.
        /// If the reliable duplex input channel receives the message, it sends back the acknowledge message.
        /// When the reliable duplex output channel receives the acknowledge message it invokes <see cref="IReliableDuplexOutputChannel.MessageDelivered"/>.
        /// If the acknowledge message is not delivered until the specified time, it invokes <see cref="IReliableDuplexOutputChannel.MessageNotDelivered"/>.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">the response receiver id used by the duplex input channel to distinguish between
        /// connected duplex output channels</param>
        /// <returns>reliable duplex output channnel</returns>
        public IReliableDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return myReliableMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
            }
        }

        /// <summary>
        /// Creates the reliable duplex input channel that can receive messages from <see cref="IReliableDuplexOutputChannel"/> and send back
        /// response messages.
        /// </summary>
        /// <remarks>
        /// This duplex output channel provides the reliable messaging and the buffered messaging.
        /// If the connection is not established, it puts sent response messages to the buffer.
        /// If the reliable duplex output channel receives the response message, it sends back the acknowledge message.
        /// When the reliable duplex input channel receives the acknowledge message it invokes <see cref="IReliableDuplexInputChannel.ResponseMessageDelivered"/>.
        /// If the acknowledge message is not delivered until the specified time, it invokes <see cref="IReliableDuplexInputChannel.ResponseMessageNotDelivered"/>.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>reliable duplex input channnel</returns>
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
