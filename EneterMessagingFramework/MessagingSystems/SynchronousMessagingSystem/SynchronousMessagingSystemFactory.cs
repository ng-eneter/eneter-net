/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem
{
    /// <summary>
    /// The factory class implements the messaging system delivering messages synchronously in the caller thread.
    /// It creates output and input channels using the caller thread to deliver messages.
    /// <br/><br/>
    /// Different instances of SynchronousMessagingSystemFactory are independent and so they
    /// are different messaging systems. Therefore if you want to send/receive a message through this messaging system
    /// then output and input channels must be created with the same instance of SynchronousMessagingSystemFactory.
    /// </summary>
    public class SynchronousMessagingSystemFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the factory representing the messaging system.
        /// Note: Every instance of the synchronous messaging system factory represents one messaging system.
        ///       It means that two instances of this factory class creates channels for two independent messaging system.
        /// </summary>
        public SynchronousMessagingSystemFactory()
        {
            using (EneterTrace.Entering())
            {
                myMessagingSystem = new SimpleMessagingSystem(new SynchronousMessagingProvider());
            }
        }

        /// <summary>
        /// Creates the output channel sending messages to specified input channel using the synchronous local call.
        /// </summary>
        /// <param name="channelId">identifies the receiving input channel</param>
        /// <returns>output channel</returns>
        public IOutputChannel CreateOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleOutputChannel(channelId, myMessagingSystem);
            }
        }

        /// <summary>
        /// Creates the input channel receiving messages on the specified channel id via the synchronous local call.
        /// </summary>
        /// <param name="channelId">identifies this input channel</param>
        /// <returns>input channel</returns>
        public IInputChannel CreateInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleInputChannel(channelId, myMessagingSystem);
            }
        }

        /// <summary>
        /// Creates the duplex output channel communicating with the specified duplex input channel using synchronous local call.
        /// The duplex output channel can send messages and receive response messages.
        /// </summary>
        /// <param name="channelId">identifies the receiving duplex input channel</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleDuplexOutputChannel(channelId, null, this);
            }
        }

        /// <summary>
        /// Creates the duplex output channel communicating with the specified duplex input channel using synchronous local call.
        /// The duplex output channel can send messages and receive response messages.
        /// </summary>
        /// <param name="channelId">identifies the receiving duplex input channel</param>
        /// <param name="responseReceiverId">identifies the response receiver of this duplex output channel</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleDuplexOutputChannel(channelId, responseReceiverId, this);
            }
        }

        /// <summary>
        /// Creates the duplex input channel listening to messages on the specified channel id.
        /// The duplex input channel can send response messages back to the duplex output channel.
        /// </summary>
        /// <param name="channelId">identifies this duplex input channel</param>
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
