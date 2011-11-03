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
    /// Declares the one-way router.
    /// </summary>
    /// <remarks>
    /// The router has attached more input channels and more output channels.<br/>
    /// When it receives some message via the input channel, it forwards the message to all configured output channels.<br/>
    /// This the one-way router. Therefore, it can forward messages, but cannot route back response messages.
    /// </remarks>
    public interface IRouter : IAttachableMultipleInputChannels, IAttachableMultipleOutputChannels
    {
        /// <summary>
        /// Configures connection between the input channel and the output channel.
        /// When the connection is established then all messages comming via this input channel will be forwarded
        /// to all output channels configured for that output channel.
        /// </summary>
        /// <param name="inputChannelId">input channel identifier</param>
        /// <param name="outputChannelId">output channel identifier</param>
        void AddConnection(string inputChannelId, string outputChannelId);

        /// <summary>
        /// Removes configured connection between the input channel and the output channel.
        /// </summary>
        /// <param name="inputChannelId">input channel identifier</param>
        /// <param name="outputChannelId">output channel identifier</param>
        void RemoveConnection(string inputChannelId, string outputChannelId);

        /// <summary>
        /// Removes all configured connctions for the given input channel.
        /// </summary>
        /// <param name="inputChannelId">input channel identifier</param>
        void RemoveInputChannelConnections(string inputChannelId);

        /// <summary>
        /// Removes all configured connections for the given output channel.
        /// </summary>
        /// <param name="outputChannelId">output channel identifier</param>
        void RemoveOutputChannelConnections(string outputChannelId);
    }
}
