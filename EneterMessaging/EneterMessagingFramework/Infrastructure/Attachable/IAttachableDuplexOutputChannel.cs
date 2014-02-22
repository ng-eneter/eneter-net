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
    /// Interface for components which want to attach one IDuplexOutputChannel.
    /// </summary>
    /// <remarks>
    /// Communication components implementing this interface can attach the duplex output channel and
    /// sends messages and receive response messages.
    /// </remarks>
    public interface IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// Attaches the duplex output channel and opens the connection for listening to response messages.
        /// </summary>
        /// <param name="duplexOutputChannel">Duplex output channel to be attached.</param>
        void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel);

        /// <summary>
        /// Detaches the duplex output channel and stops listening to response messages.
        /// </summary>
        void DetachDuplexOutputChannel();

        /// <summary>
        /// Returns true if the reference to the duplex output channel is stored. <br/>
        /// </summary>
        bool IsDuplexOutputChannelAttached { get; }

        /// <summary>
        /// Returns attached duplex output channel.
        /// </summary>
        IDuplexOutputChannel AttachedDuplexOutputChannel { get; }
    }
}
