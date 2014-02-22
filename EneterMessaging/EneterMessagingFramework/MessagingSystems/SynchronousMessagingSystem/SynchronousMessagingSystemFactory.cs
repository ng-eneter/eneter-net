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

namespace Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages synchronously (like a synchronous local call).
    /// </summary>
    /// <remarks>
    /// It creates output and input channels using the caller thread to deliver messages.
    /// <br/><br/>
    /// Different instances of SynchronousMessagingSystemFactory are independent and so they
    /// are different messaging systems. Therefore if you want to send/receive a message through this messaging system
    /// then output and input channels must be created with the same instance of SynchronousMessagingSystemFactory.
    /// </remarks>
    public class SynchronousMessagingSystemFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory representing the messaging system.
        /// </summary>
        /// <remarks>
        /// Note: Every instance of the synchronous messaging system factory represents one messaging system.
        ///       It means that two instances of this factory class creates channels for two independent messaging system.
        /// </remarks>
        public SynchronousMessagingSystemFactory()
            : this(new LocalProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the factory representing the messaging system.
        /// </summary>
        /// <param name="protocolFormatter">formatter used to encode low-level messages between channels</param>
        public SynchronousMessagingSystemFactory(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myDefaultMessagingFactory = new DefaultMessagingSystemFactory(new SynchronousMessagingProvider(), protocolFormatter);
            }
        }

        /// <summary>
        /// Creates the duplex output channel communicating with the specified duplex input channel using synchronous local call.
        /// </summary>
        /// <remarks>
        /// The duplex output channel can send messages and receive response messages.
        /// </remarks>
        /// <param name="channelId">identifies the receiving duplex input channel</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myDefaultMessagingFactory.CreateDuplexOutputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the duplex output channel communicating with the specified duplex input channel using synchronous local call.
        /// </summary>
        /// <remarks>
        /// The duplex output channel can send messages and receive response messages.
        /// </remarks>
        /// <param name="channelId">identifies the receiving duplex input channel</param>
        /// <param name="responseReceiverId">identifies the response receiver of this duplex output channel</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return myDefaultMessagingFactory.CreateDuplexOutputChannel(channelId, responseReceiverId);
            }
        }

        /// <summary>
        /// Creates the duplex input channel listening to messages on the specified channel id.
        /// </summary>
        /// <remarks>
        /// The duplex input channel can send response messages back to the duplex output channel.
        /// </remarks>
        /// <param name="channelId">identifies this duplex input channel</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return myDefaultMessagingFactory.CreateDuplexInputChannel(channelId);
            }
        }

        /// <summary>
        /// Factory that will create dispatchers responsible for routing events from duplex input channel according to
        /// desired threading strategy.
        /// </summary>
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
        /// Factory that will create dispatchers responsible for routing events from duplex output channel according to
        /// desired threading strategy.
        /// </summary>
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
