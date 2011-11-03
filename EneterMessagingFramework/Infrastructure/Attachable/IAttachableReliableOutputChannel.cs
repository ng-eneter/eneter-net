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
    /// The interface declares methods to attach/detach IReliableOutputChannel
    /// </summary>
    /// <remarks>
    /// For more details about the reliable output channel see <see cref="IReliableDuplexOutputChannel"/>.
    /// </remarks>
    public interface IAttachableReliableOutputChannel
    {
        /// <summary>
        /// Attaches the reliable output channel and starts listening to response messages.
        /// </summary>
        /// <param name="reliableOutputChannel"></param>
        void AttachReliableOutputChannel(IReliableDuplexOutputChannel reliableOutputChannel);

        /// <summary>
        /// Detaches the reliable output channel and stops listening to response messages.
        /// </summary>
        void DetachReliableOutputChannel();

        /// <summary>
        /// Returns true if the reliable output channel is attached.
        /// </summary>
        bool IsReliableOutputChannelAttached { get; }

        /// <summary>
        /// Returns attached reliable output channel.
        /// </summary>
        IReliableDuplexOutputChannel AttachedReliableOutputChannel { get; }
    }
}
