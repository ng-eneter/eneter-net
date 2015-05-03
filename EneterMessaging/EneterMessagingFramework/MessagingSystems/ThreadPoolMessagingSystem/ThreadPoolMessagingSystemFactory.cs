/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.ThreadPoolMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages asynchronously (when a message is received a separate thread is invoked to process it).
    /// </summary>
    /// <remarks>
    /// Each incoming message is routed into its own thread from the pool. It means when a message is received the thread
    /// from the pool is taken and the message is notified.
    /// </remarks>
    public class ThreadPoolMessagingSystemFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory.
        /// </summary>
        public ThreadPoolMessagingSystemFactory()
            : this(new LocalProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="protocolFormatter">formatter used to encode low-level messages between channels</param>
        public ThreadPoolMessagingSystemFactory(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myDefaultMessagingFactory = new DefaultMessagingSystemFactory(new ThreadPoolMessagingProvider(), protocolFormatter);
            }
        }


        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using the thread pool.
        /// </summary>
        /// <param name="channelId">Identifies the receiving duplex input channel.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myDefaultMessagingFactory.CreateDuplexOutputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using the thread pool.
        /// </summary>
        /// <param name="channelId">Identifies the receiving duplex input channel.</param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return myDefaultMessagingFactory.CreateDuplexOutputChannel(channelId, responseReceiverId);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using the thread pool.
        /// </summary>
        /// <param name="channelId">Identifies this duplex input channel.</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myDefaultMessagingFactory.CreateDuplexInputChannel(channelId);
            }
        }

        /// <summary>
        /// Threading mode for input channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed by one working thread.
        /// </remarks>
        public IThreadDispatcherProvider InputChannelThreading
        {
            get
            {
                return myDefaultMessagingFactory.InputChannelThreading;
            }

            set
            {
                myDefaultMessagingFactory.InputChannelThreading = value;
            }
        }

        /// <summary>
        /// Threading mode for output channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed via one working thread.
        /// </remarks>
        public IThreadDispatcherProvider OutputChannelThreading
        {
            get
            {
                return myDefaultMessagingFactory.OutputChannelThreading;
            }
            set
            {
                myDefaultMessagingFactory.OutputChannelThreading = value;
            }
        }

        private DefaultMessagingSystemFactory myDefaultMessagingFactory;
    }
}
