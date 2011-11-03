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
    /// The interface declares methods to attach/detach one IDuplexInputChannel.
    /// </summary>
    /// <remarks>
    /// The duplex input channel is used in the request-response communication by a listener
    /// to receive request messages and send back response messages.
    /// </remarks>
    public interface IAttachableDuplexInputChannel
    {
        /// <summary>
        /// Attaches the duplex input channel and starts listening to messages.
        /// </summary>
        void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel);

        /// <summary>
        /// Detaches the duplex input channel and stops listening to messages.
        /// </summary>
        void DetachDuplexInputChannel();

        /// <summary>
        /// Returns true if the duplex input channel is attached.
        /// </summary>
        bool IsDuplexInputChannelAttached { get; }

        /// <summary>
        /// Retutns attached duplex input channel.
        /// </summary>
        IDuplexInputChannel AttachedDuplexInputChannel { get; }
    }
}
