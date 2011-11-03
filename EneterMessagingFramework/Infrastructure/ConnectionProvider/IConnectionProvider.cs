/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.ConnectionProvider
{
    /// <summary>
    /// The interface declares methods for convenient attaching of channels and
    /// for connecting senders and receivers with channels.
    /// </summary>
    public interface IConnectionProvider
    {
        /// <summary>
        /// Creates and attaches the input channel to the component.
        /// </summary>
        /// <param name="inputComponent">Component.</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        void Attach(IAttachableInputChannel inputComponent, string channelId);

        /// <summary>
        /// Creates and attaches the input channel to the component that can have attached multiple input channels.
        /// </summary>
        /// <param name="inputComponent">Component</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        void Attach(IAttachableMultipleInputChannels inputComponent, string channelId);

        /// <summary>
        /// Creates and attaches the output channel to the component.
        /// </summary>
        /// <param name="outputComponent">Component.</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        void Attach(IAttachableOutputChannel outputComponent, string channelId);

        /// <summary>
        /// Creates and attaches the output channel to the component that can have attached multiple output channels.
        /// </summary>
        /// <param name="outputComponent">Component.</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        void Attach(IAttachableMultipleOutputChannels outputComponent, string channelId);

        /// <summary>
        /// Creates and attaches the duplex input channel to the component.
        /// </summary>
        /// <param name="inputComponent">Component.</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        void Attach(IAttachableDuplexInputChannel inputComponent, string channelId);

        /// <summary>
        /// Creates and attaches the duplex input channel to the component that can have attached multiple duplex input channels.
        /// </summary>
        /// <param name="inputComponent">Component.</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        void Attach(IAttachableMultipleDuplexInputChannels inputComponent, string channelId);

        /// <summary>
        /// Creates and attaches the duplex output channel to the component.
        /// </summary>
        /// <param name="outputComponent">Component.</param>
        /// <param name="channelId">Channel Id of the channel that will be created.</param>
        void Attach(IAttachableDuplexOutputChannel outputComponent, string channelId);

        /// <summary>
        /// Creates the input channel and output channel and then connects the receiver and sender.
        /// </summary>
        /// <param name="inputComponent">Input channel attachable component (receiver)</param>
        /// <param name="outputComponent">Output channel attachabel component (sender)</param>
        /// <param name="channelId">channel id</param>
        void Connect(IAttachableInputChannel inputComponent, IAttachableOutputChannel outputComponent, string channelId);

        /// <summary>
        /// Creates the input channel and output channel and then connects the receiver and sender.
        /// </summary>
        /// <param name="inputComponent">input channel attachable component (receiver)</param>
        /// <param name="outputComponent">multiple output channel attachable component (sender)</param>
        /// <param name="channelId">channel id</param>
        void Connect(IAttachableInputChannel inputComponent, IAttachableMultipleOutputChannels outputComponent, string channelId);

        /// <summary>
        /// Creates the input channel and output channel and then connects the receiver and sender.
        /// </summary>
        /// <param name="inputComponent">input channel attachable component (receiver)</param>
        /// <param name="outputComponent">multiple output channel attachable component (sender)</param>
        /// <param name="channelId">channel id</param>
        void Connect(IAttachableMultipleInputChannels inputComponent, IAttachableMultipleOutputChannels outputComponent, string channelId);

        /// <summary>
        /// Creates the input channel and output channel and then connects the receiver and sender.
        /// </summary>
        /// <param name="inputComponent">input channel attachable component (receiver)</param>
        /// <param name="outputComponent">multiple output channel attachable component (sender)</param>
        /// <param name="channelId">channel id</param>
        void Connect(IAttachableMultipleInputChannels inputComponent, IAttachableOutputChannel outputComponent, string channelId);

        /// <summary>
        /// Creates the duplex input channel and duplex output channel and then connects the receiver and sender.
        /// </summary>
        /// <param name="inputComponent">duplex input channel attachable component (receiver)</param>
        /// <param name="outputComponent">duplex output channel attachable component (sender)</param>
        /// <param name="channelId">channel id</param>
        void Connect(IAttachableDuplexInputChannel inputComponent, IAttachableDuplexOutputChannel outputComponent, string channelId);

        /// <summary>
        /// Creates the duplex input channel and duplex output channel and then connects the receiver and sender.
        /// </summary>
        /// <param name="inputComponent">duplex input channel attachable component (receiver)</param>
        /// <param name="outputComponent">duplex output channel attachable component (sender)</param>
        /// <param name="channelId">channel id</param>
        void Connect(IAttachableMultipleDuplexInputChannels inputComponent, IAttachableDuplexOutputChannel outputComponent, string channelId);

        /// <summary>
        /// Returns the messaging system used by the connection provider.
        /// </summary>
        IMessagingSystemFactory MessagingSystem { get; }
    }
}
