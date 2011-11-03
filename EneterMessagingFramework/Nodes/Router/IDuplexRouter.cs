/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.Nodes.Router
{
    /// <summary>
    /// Declares the duplex router.
    /// </summary>
    /// <remarks>
    /// The duplex router has attached more duplex input channels and uses more duplex output channels.<br/>
    /// When it receives some message via the duplex input channel, it forwards the message to all configured duplex output channels.<br/>
    /// This router is bidirectional. Therefore, it can forward messages and also route back response messages from receivers.
    /// </remarks>
    public interface IDuplexRouter : IAttachableMultipleDuplexInputChannels
    {
        /// <summary>
        /// Adds the connection configuration to the router. It means when the duplex input channel receives a
        /// message then the message will be forwarded to the specified duplex output channel too.
        /// </summary>
        /// <param name="duplexInputChannelId"></param>
        /// <param name="duplexOutputChannelId"></param>
        void AddConnection(string duplexInputChannelId, string duplexOutputChannelId);

        /// <summary>
        /// Removes the connection configuration from the router.
        /// </summary>
        /// <param name="duplexInputChannelId"></param>
        /// <param name="duplexOutputChannelId"></param>
        void RemoveConnection(string duplexInputChannelId, string duplexOutputChannelId);

        /// <summary>
        /// Removes all configurations from the router.
        /// </summary>
        void RemoveAllConnections();
    }
}
