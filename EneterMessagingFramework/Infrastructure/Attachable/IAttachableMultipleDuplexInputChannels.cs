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
    /// The interface declares methods to attach/detach multiple IDuplexInputChannel.
    /// </summary>
    /// <remarks>
    /// Some comunication components need to attach several channels. E.g. <see cref="IDuplexDispatcher"/>. <br/>
    /// Component using multiple duplex input channels is used in request-response communication and is
    /// able to listen to requests on more channels (addresses) and send back (to the right sender) the response message.
    /// </remarks>
    public interface IAttachableMultipleDuplexInputChannels
    {
        /// <summary>
        /// Attaches the duplex input channel nad starts listening to messages.
        /// </summary>
        void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel);

        /// <summary>
        /// Detaches the duplex input channel.
        /// Detaching the input channel stops listening to the messages.
        /// </summary>
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
