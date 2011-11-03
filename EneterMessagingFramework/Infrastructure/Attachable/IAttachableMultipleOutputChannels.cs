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
    /// The interface declares methods to attach/detach multiple IOutputChannel.
    /// </summary>
    /// <remarks>
    /// Some comunication components need to attach several channels.
    /// Components using multiple output channels are used in one-way communication.
    /// They are able to send messages to several receivers. E.g. <see cref="IDispatcher"/>
    /// </remarks>
    public interface IAttachableMultipleOutputChannels
    {
        /// <summary>
        /// Attaches the output channel.
        /// </summary>
        void AttachOutputChannel(IOutputChannel outputChannel);

        /// <summary>
        /// Detaches all output channels.
        /// </summary>
        void DetachOutputChannel();

        /// <summary>
        /// Detaches the output channel.
        /// </summary>
        void DetachOutputChannel(string channelId);

        /// <summary>
        /// Returns true if at least one output channel is attached.
        /// </summary>
        /// <remarks>
        /// If the output channel is attached it means the object that has attached the channel
        /// can send messages.
        /// Multiple output channel attachable means that the object can send messages to more receivers.
        /// </remarks>
        bool IsOutputChannelAttached { get; }

        /// <summary>
        /// Returns attached output channels.
        /// </summary>
        IEnumerable<IOutputChannel> AttachedOutputChannels { get; }
    }
}
