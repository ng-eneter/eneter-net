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
    /// The interface declares methods to attach/detach one IInputChannel.
    /// </summary>
    /// <remarks>
    /// The input channel is used in one-way communication by a listener to receive messages.
    /// Components that are able to attach the input channel can listen to messages but they cannot send back any response message.
    /// </remarks>
    public interface IAttachableInputChannel
    {
        /// <summary>
        /// Attaches the input channel.
        /// It stores the reference to the input channel and starts the listening.
        /// </summary>
        void AttachInputChannel(IInputChannel inputChannel);

        /// <summary>
        /// Detaches the input channel.
        /// It cleans the reference to the input channel and stops the listening.
        /// </summary>
        void DetachInputChannel();

        /// <summary>
        /// Returns true if the reference to the input channel is stored. <br/>
        /// </summary>
        /// <remarks>
        /// Notice, unlike version 1.0, the true value does not mean the channel is listening.
        /// The method AttachInputChannel() starts also the listening, but if the listening stops (for whatever reason),
        /// the input channel stays attached. To figure out if the input channel is listening use property <see cref="IInputChannel.IsListening"/>.
        /// </remarks>
        bool IsInputChannelAttached { get; }

        /// <summary>
        /// Returns attached input channel.
        /// </summary>
        IInputChannel AttachedInputChannel { get; }
    }
}
