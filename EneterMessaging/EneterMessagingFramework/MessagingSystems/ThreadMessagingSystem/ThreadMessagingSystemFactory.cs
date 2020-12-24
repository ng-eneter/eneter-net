

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.ThreadMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages to the particular working thread.
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
        /// Creates the output channel sending messages to the input channel and receiving response messages by using the working thread.
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
        /// Creates the output channel sending messages to the input channel and receiving response messages by using the working thread.
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
        /// Creates the input channel receiving messages from the output channel and sending back response messages by using the working thread.
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
