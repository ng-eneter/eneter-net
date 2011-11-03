/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    /// <summary>
    /// The interface declares methods to attach/detach IReliableInputChannel
    /// </summary>
    /// <remarks>
    /// For more details about the reliable input channel see <see cref="IReliableDuplexInputChannel"/>.
    /// </remarks>
    public interface IAttachableReliableInputChannel
    {
        /// <summary>
        /// Attaches the reliable input channel and starts listening to messages.
        /// </summary>
        /// <param name="reliableInputChannel"></param>
        void AttachReliableInputChannel(IReliableDuplexInputChannel reliableInputChannel);

        /// <summary>
        /// Detaches the reliable input channel and stops listening to messages.
        /// </summary>
        void DetachReliableInputChannel();

        /// <summary>
        /// Returns true if the reliable input channel is attached.
        /// </summary>
        bool IsReliableInputChannelAttached { get; }

        /// <summary>
        /// Returns attached reliable input channel.
        /// </summary>
        IReliableDuplexInputChannel AttachedReliableInputChannel { get; }
    }
}
