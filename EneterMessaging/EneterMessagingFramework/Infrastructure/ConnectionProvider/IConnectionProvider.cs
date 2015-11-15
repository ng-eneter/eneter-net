/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;

namespace Eneter.Messaging.Infrastructure.ConnectionProvider
{
    /// <summary>
    /// The interface declares methods for convenient attaching of channels and
    /// for connecting senders and receivers with channels.
    /// </summary>
    [Obsolete("This interface is deprecated and will be removed in the future. Instead of that please create channels using IMessagingSystemFactory and attach them to components via methods AttachDuplexOutputChannel or AttachDuplexInputChannel.")]
    public interface IConnectionProvider
    {
        /// <summary>
        /// Creates and attaches the duplex input channel to the component.
        /// </summary>
        /// <param name="inputComponent">Component.</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        [Obsolete("This method is deprecated and will be removed in the future. Instead of that please create channels using IMessagingSystemFactory and attach them to components via methods AttachDuplexOutputChannel or AttachDuplexInputChannel.")]
        void Attach(IAttachableDuplexInputChannel inputComponent, string channelId);

        /// <summary>
        /// Creates and attaches the duplex input channel to the component that can have attached multiple duplex input channels.
        /// </summary>
        /// <param name="inputComponent">Component.</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        [Obsolete("This method is deprecated and will be removed in the future. Instead of that please create channels using IMessagingSystemFactory and attach them to components via methods AttachDuplexOutputChannel or AttachDuplexInputChannel.")]
        void Attach(IAttachableMultipleDuplexInputChannels inputComponent, string channelId);

        /// <summary>
        /// Creates and attaches the duplex output channel to the component.
        /// </summary>
        /// <param name="outputComponent">Component.</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        [Obsolete("This method is deprecated and will be removed in the future. Instead of that please create channels using IMessagingSystemFactory and attach them to components via methods AttachDuplexOutputChannel or AttachDuplexInputChannel.")]
        void Attach(IAttachableDuplexOutputChannel outputComponent, string channelId);

        /// <summary>
        /// Creates the duplex input channel and duplex output channel and then connects the receiver and sender.
        /// </summary>
        /// <param name="inputComponent">duplex input channel attachable component (receiver)</param>
        /// <param name="outputComponent">duplex output channel attachable component (sender)</param>
        /// <param name="channelId">channel id</param>
        [Obsolete("This method is deprecated and will be removed in the future. Instead of that please create channels using IMessagingSystemFactory and attach them to components via methods AttachDuplexOutputChannel or AttachDuplexInputChannel.")]
        void Connect(IAttachableDuplexInputChannel inputComponent, IAttachableDuplexOutputChannel outputComponent, string channelId);

        /// <summary>
        /// Creates the duplex input channel and duplex output channel and then connects the receiver and sender.
        /// </summary>
        /// <param name="inputComponent">duplex input channel attachable component (receiver)</param>
        /// <param name="outputComponent">duplex output channel attachable component (sender)</param>
        /// <param name="channelId">channel id</param>
        [Obsolete("This method is deprecated and will be removed in the future. Instead of that please create channels using IMessagingSystemFactory and attach them to components via methods AttachDuplexOutputChannel or AttachDuplexInputChannel.")]
        void Connect(IAttachableMultipleDuplexInputChannels inputComponent, IAttachableDuplexOutputChannel outputComponent, string channelId);

        /// <summary>
        /// Returns the messaging system used by the connection provider.
        /// </summary>
        [Obsolete("This property is deprecated and will be removed in the future. Instead of that please create channels using IMessagingSystemFactory and attach them to components via methods AttachDuplexOutputChannel or AttachDuplexInputChannel.")]
        IMessagingSystemFactory MessagingSystem { get; }
    }
}
