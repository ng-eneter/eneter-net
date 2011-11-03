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
    /// The interface declares methods to attach/detach one IDuplexOutputChannel.
    /// </summary>
    /// <remarks>
    /// The duplex output channel is used in the request-response communication by a sender
    /// to send request messages and receive response messages.
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
        /// <remarks>
        /// Notice, unlike version 1.0, the value 'true' does not mean the connection is open. If the duplex output
        /// channel was successfuly attached but the connection was broken, the channel stays attached but the connection is not open.
        /// To detect if the attached channel is listening to response messages, check the property <see cref="IDuplexOutputChannel.IsConnected"/>.
        /// </remarks>
        bool IsDuplexOutputChannelAttached { get; }

        /// <summary>
        /// Returns attached duplex output channel.
        /// </summary>
        IDuplexOutputChannel AttachedDuplexOutputChannel { get; }
    }
}
