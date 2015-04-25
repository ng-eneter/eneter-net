/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


using System.Collections.Generic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    /// <summary>
    /// Interface for components which want to attach multiple IDuplexInputChannel.
    /// </summary>
    /// <remarks>
    /// Communication components implementing this interface can attach multiple duplex input channels and listens via them to messages.
    /// </remarks>
    public interface IAttachableMultipleDuplexInputChannels
    {
        /// <summary>
        /// Attaches the duplex input channel nad starts listening to messages.
        /// </summary>
        /// <param name="duplexInputChannel">duplex input channel to be attached</param>
        void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel);

        /// <summary>
        /// Detaches the duplex input channel.
        /// </summary>
        /// <remarks>
        /// Detaching the input channel stops listening to the messages.
        /// It releases listening threads.
        /// </remarks>
        void DetachDuplexInputChannel();

        /// <summary>
        /// Returns true if the duplex input channel is attached.
        /// </summary>
        bool IsDuplexInputChannelAttached { get; }

        /// <summary>
        /// Detaches the duplex input channel.
        /// </summary>
        void DetachDuplexInputChannel(string channelId);

        /// <summary>
        /// Returns attached input channels.
        /// </summary>
        IEnumerable<IDuplexInputChannel> AttachedDuplexInputChannels { get; }
    }
}
