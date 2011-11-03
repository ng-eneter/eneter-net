/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    /// <summary>
    /// The interface declares methods to attach/detach one IOutputChannel.
    /// </summary>
    /// <remarks>
    /// The output channel is used in one-way communication by a sender to send messages.
    /// Components that are able to attach the output channel can send messages but they cannot receive any response messages.
    /// </remarks>
    public interface IAttachableOutputChannel
    {
        /// <summary>
        /// Attaches the output channel.
        /// </summary>
        void AttachOutputChannel(IOutputChannel outputChannel);

        /// <summary>
        /// Detaches the output channel.
        /// </summary>
        void DetachOutputChannel();

        /// <summary>
        /// Returns true if the output channel is attached.
        /// </summary>
        /// <remarks>
        /// If the output channel is attached it means the object that has attached the channel
        /// can send messages.
        /// </remarks>
        bool IsOutputChannelAttached { get; }

        /// <summary>
        /// Returns attached output channel.
        /// </summary>
        IOutputChannel AttachedOutputChannel { get; }
    }
}
