/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.Nodes.Dispatcher
{
    /// <summary>
    /// Declares the duplex dispatcher.
    /// </summary>
    /// <remarks>
    /// The duplex dispatcher has attached more duplex input channels and uses more duplex output channels.<br/>
    /// When it receives some message via the duplex input channel it forwards the message to all duplex output channels.<br/>
    /// The duplex dispatcher allows the bidirectional communication. It means, receivers to whom the message was forwarded can
    /// sand back response messages. Therefore, the sender can get response messages from all receivers.
    /// </remarks>
    public interface IDuplexDispatcher : IAttachableMultipleDuplexInputChannels
    {
        /// <summary>
        /// Adds the duplex output channel id to the dispatcher. The dispatcher will then start to forward
        /// the incoming messages also to this channel.
        /// </summary>
        /// <param name="channelId"></param>
        void AddDuplexOutputChannel(string channelId);

        /// <summary>
        /// Removes the duplex output channel from the dispatcher.
        /// </summary>
        /// <param name="channelId"></param>
        void RemoveDuplexOutputChannel(string channelId);

        /// <summary>
        /// Removes all duplex output channels from the dispatcher.
        /// </summary>
        void RemoveAllDuplexOutputChannels();

        /// <summary>
        /// Returns response receiver id of the client connected to the dispatcher.
        /// </summary>
        /// <param name="responseReceiverId">responseRecieverId after dispatching</param>
        /// <returns>responseReceiverId of the client connected to the dispatcher. Returns null if it does not exist.</returns>
        string GetAssociatedResponseReceiverId(string responseReceiverId);
    }
}
