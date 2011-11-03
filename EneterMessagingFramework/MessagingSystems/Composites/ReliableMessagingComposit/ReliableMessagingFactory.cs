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

namespace Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit
{
    /// <summary>
    /// Implements the factory creating channels for the reliable communication.
    /// </summary>
    /// <remarks>
    /// For more details about the reliable communication, refer to <see cref="IReliableMessagingFactory"/>
    /// </remarks>
    public class ReliableMessagingFactory : IReliableMessagingFactory
    {
        /// <summary>
        /// Constructs the factory with default settings.
        /// </summary>
        /// <remarks>
        /// For the serialization of reliable messages is used <see cref="XmlStringSerializer"/>.
        /// The maximum time, the acknowledge message must be received is set to 12 seconds.
        /// </remarks>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        public ReliableMessagingFactory(IMessagingSystemFactory underlyingMessaging)
            : this(underlyingMessaging, new XmlStringSerializer(), TimeSpan.FromMilliseconds(12000))
        {
        }

        /// <summary>
        /// Constructs the factory with specified parameters.
        /// </summary>
        /// <param name="underlyingMessaging">underlying messaging system e.g. HTTP, TCP, ...</param>
        /// <param name="serializer">serializer used to serialize the acknowledge messages</param>
        /// <param name="acknowledgeTimeout">the maximum time until the delivery of the message must be acknowledged</param>
        public ReliableMessagingFactory(IMessagingSystemFactory underlyingMessaging, ISerializer serializer, TimeSpan acknowledgeTimeout)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingMessaging = underlyingMessaging;
                mySerializer = serializer;
                myAcknowledgementTimeout = acknowledgeTimeout;
            }
        }

        /// <summary>
        /// Creates the output channel by using the underlying messaging system.
        /// </summary>
        /// <remarks>
        /// Since the output channel communicates oneway, the acknowledge messaging is not applicable.
        /// Therefore, the implementation of the method just uses the underlying messaging to create the output channel.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>output channel</returns>
        public IOutputChannel CreateOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myUnderlyingMessaging.CreateOutputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the input channel by using the underlying messaging system.
        /// </summary>
        /// <remarks>
        /// Since the input channel communicates oneway, the acknowledge messaging is not applicable.
        /// Therefore, the implementation of the method just uses the underlying messaging to create the input channel.
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
        /// Creates the reliable duplex output channel that can send messages and receive response messages from <see cref="IReliableDuplexInputChannel"/>.
        /// </summary>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>reliable duplex output channel</returns>
        public IReliableDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexOutputChannel anUnderlyingDuplexOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId);
                return new ReliableDuplexOutputChannel(anUnderlyingDuplexOutputChannel, mySerializer, myAcknowledgementTimeout);
            }
        }

        /// <summary>
        /// Creates the reliable duplex output channel that can send messages and receive response messages from <see cref="IReliableDuplexInputChannel"/>.
        /// </summary>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">id of the response receiver</param>
        /// <returns>reliable duplex output channel</returns>
        public IReliableDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexOutputChannel anUnderlyingDuplexOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
                return new ReliableDuplexOutputChannel(anUnderlyingDuplexOutputChannel, mySerializer, myAcknowledgementTimeout);
            }
        }

        /// <summary>
        /// Creates the reliable duplex input channel that can receive messages from <see cref="IReliableDuplexOutputChannel"/> and send back
        /// response messages.
        /// </summary>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>reliableduplex input channel</returns>
        public IReliableDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexInputChannel anUnderlyingDuplexInputChannel = myUnderlyingMessaging.CreateDuplexInputChannel(channelId);
                return new ReliableDuplexInputChannel(anUnderlyingDuplexInputChannel, mySerializer, myAcknowledgementTimeout);
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



        private IMessagingSystemFactory myUnderlyingMessaging;
        private ISerializer mySerializer;
        private TimeSpan myAcknowledgementTimeout;
    }
}
