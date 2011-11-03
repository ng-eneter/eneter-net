/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

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
        /// Constructs the factory representing the messaging system. <br/>
        /// Every instance of the synchronous messaging system factory represents one messaging system.
        /// It means that two instances of this factory class creates channels for two independent (differnt) messaging system.
        /// </summary>
        public ThreadMessagingSystemFactory()
        {
            using (EneterTrace.Entering())
            {
                myMessagingSystem = new SimpleMessagingSystem(new ThreadMessagingProvider());
            }
        }

        /// <summary>
        /// Creates the output channel sending messages to the specified input channel via the message queue processed by the working thread.
        /// </summary>
        /// <remarks>
        /// The output channel can send messages only to the input channel and not to the duplex input channel.
        /// </remarks>
        /// <param name="channelId">Identifies the receiving input channel.</param>
        /// <returns>output channel</returns>
        public IOutputChannel CreateOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleOutputChannel(channelId, myMessagingSystem);
            }
        }

        /// <summary>
        /// Creates the input channel receiving messages from the output channel via the working thread.
        /// </summary>
        /// <remarks>
        /// The input channel can receive messages only from the output channel and not from the duplex output channel.
        /// </remarks>
        /// <param name="channelId">Identifies this input channel.</param>
        /// <returns>input channel</returns>
        public IInputChannel CreateInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleInputChannel(channelId, myMessagingSystem);
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
                return new SimpleDuplexOutputChannel(channelId, null, this);
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
                return new SimpleDuplexOutputChannel(channelId, responseReceiverId, this);
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
                return new SimpleDuplexInputChannel(channelId, this);
            }
        }


        private IMessagingSystemBase myMessagingSystem;
    }
}
