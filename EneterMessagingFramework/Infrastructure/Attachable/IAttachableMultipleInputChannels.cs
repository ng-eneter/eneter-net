/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


using System.Collections.Generic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Nodes.Dispatcher;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    /// <summary>
    /// The interface declares methods to attach/detach multiple IInputChannel.
    /// </summary>
    /// <remarks>
    /// Some comunication components need to attach several channels.
    /// Components using multiple input channels are used in one-way communication.
    /// They are able to listen to messages on more input channels (addresses).
    /// But they are not able to send back response messages. <br/>
    /// E.g. <see cref="IDispatcher"/>
    /// </remarks>
    public interface IAttachableMultipleInputChannels
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
        /// Detaches the input channel.
        /// </summary>
        void DetachInputChannel(string channelId);

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
        /// Returns attached input channels.
        /// </summary>
        IEnumerable<IInputChannel> AttachedInputChannels { get; }
    }
}
