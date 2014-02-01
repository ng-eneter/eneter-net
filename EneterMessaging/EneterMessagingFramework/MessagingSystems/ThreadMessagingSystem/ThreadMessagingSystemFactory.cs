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

namespace Eneter.Messaging.MessagingSystems.ThreadMessagingSystem
{
    /// <summary>
    /// Implements the messaging system delivering messages to the particular working thread.
    /// </summary>
    /// <remarks>
    /// Each input channel is represented by its own working thread removing messages from the queue and processing them
    /// one by one.
    /// <br/><br/>
    /// Different instances of ThreadMessagingSystemFactory are independent and so they
    /// are different messaging systems. Therefore if you want to send/receive a message with this messaging system
    /// then output and input channels must be created by the same instance of ThreadMessagingSystemFactory.
    /// <br/><br/>
    /// Notice, the messages are always received in one particular working thread, but the notification events e.g. connection opened
    /// are invoked in a different thread.
    /// </remarks>
    public class ThreadMessagingSystemFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs thread based messaging factory.
        /// </summary>
        /// <remarks>
        /// Every instance of the synchronous messaging system factory represents one messaging system.
        /// It means that two instances of this factory class creates channels for two independent (differnt) messaging system.
        /// </remarks>
        public ThreadMessagingSystemFactory()
            : this(new LocalProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs thread based messaging factory.
        /// </summary>
        /// <remarks>
        /// Every instance of the synchronous messaging system factory represents one messaging system.
        /// It means that two instances of this factory class creates channels for two independent (differnt) messaging system.
        /// </remarks>
        /// <param name="protocolFormatter">low-level message formatter for the communication between channels.</param>
        public ThreadMessagingSystemFactory(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myDefaultMessagingFactory = new DefaultMessagingSystemFactory(new ThreadMessagingProvider(), protocolFormatter);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using the working thread.
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method generates the unique response receiver id automatically.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
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
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using the working thread.
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method allows to specified a desired response receiver id. Please notice, the response receiver
        /// id is supposed to be unique.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
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
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using the working thread.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// </remarks>
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
        /// Factory that will create dispatchers responsible for routing events from duplex input channel according to
        /// desired threading strategy.
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
        /// Factory that will create dispatchers responsible for routing events from duplex output channel according to
        /// desired threading strategy.
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

        public IProtocolFormatter ProtocolFormatter { get { return myDefaultMessagingFactory.ProtocolFormatter; } }

        private DefaultMessagingSystemFactory myDefaultMessagingFactory;
    }
}
