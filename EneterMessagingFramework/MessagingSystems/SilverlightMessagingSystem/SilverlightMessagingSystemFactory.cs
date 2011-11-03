/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    /// <summary>
    /// Implements the messaging system delivering messages between Silverlight applications.
    /// </summary>
    /// <remarks>
    /// It creates output and input channels using the Silverlight messaging.
    /// This messaging system improves the Silverlight messaging. It allows to send messages of unlimited size and from any thread.
    /// The input channels receive messages always in the Silverlight thread.
    /// </remarks>
    public class SilverlightMessagingSystemFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory.
        /// </summary>
        public SilverlightMessagingSystemFactory()
        {
            using (EneterTrace.Entering())
            {
                myLocalSenderReceiverFactory = new SilverlightLocalSenderReceiverFactory();
            }
        }

        /// <summary>
        /// The constructor allowing to mock real silverlight messaging.
        /// The constructor can be used for unit-test purposes.
        /// </summary>
        public SilverlightMessagingSystemFactory(ILocalSenderReceiverFactory testingLocalSenderReceiverFactory)
        {
            using (EneterTrace.Entering())
            {
                myLocalSenderReceiverFactory = testingLocalSenderReceiverFactory;
            }
        }

        /// <summary>
        /// Creates the output channel sending messages to specified input channel by using Silverlight messaging.
        /// </summary>
        /// <remarks>
        /// The output channel can send messages only to the input channel and not to the duplex input channel.
        /// </remarks>
        /// <param name="channelId">id representing the receiver</param>
        /// <returns>output channel</returns>
        public IOutputChannel CreateOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SilverlightOutputChannel(channelId, myLocalSenderReceiverFactory);
            }
        }

        /// <summary>
        /// Creates the input channel receiving messages from output channel by using Silverlight messaging.
        /// </summary>
        /// <remarks>
        /// The input channel can receive messages only from the output channel and not from the duplex output channel.
        /// </remarks>
        /// <param name="channelId">id, the input channel listens to</param>
        /// <returns>input channel</returns>
        public IInputChannel CreateInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SilverlightInputChannel(channelId, myLocalSenderReceiverFactory);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using Silverlight messaging.
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
        /// <param name="channelId">id representing the receiving duplex input channel</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleDuplexOutputChannel(channelId, null, this, mySerializer);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using Silverlight messaging.
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
        /// <param name="channelId">id representing the receiving duplex input channel</param>
        /// <param name="responseReceiverId">identifies the response receiver of this duplex output channel</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleDuplexOutputChannel(channelId, responseReceiverId, this, mySerializer);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using Silverlight messaging.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// </remarks>
        /// <param name="channelId">id, the duplex input channel is listening to</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleDuplexInputChannel(channelId, this, mySerializer);
            }
        }


        private ILocalSenderReceiverFactory myLocalSenderReceiverFactory;
        private ISerializer mySerializer = new XmlStringSerializer();
    }
}

#endif
