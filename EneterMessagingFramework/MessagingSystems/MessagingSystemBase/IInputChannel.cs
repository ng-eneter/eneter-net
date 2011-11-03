/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Declares the input channel that can receive messages from the output channel.
    /// </summary>
    /// <remarks>
    /// Notice, the input channel can receive messages only from the output channel.
    /// It cannot receive messages from the duplex output channel.
    /// </remarks>
    public interface IInputChannel
    {
        /// <summary>
        /// The event is invoked when a message was received.
        /// </summary>
        event EventHandler<ChannelMessageEventArgs> MessageReceived;

        /// <summary>
        /// Returns id of the channel.
        /// The channel id represents the address the receiver is listening to.
        /// </summary>
        string ChannelId { get; }

        /// <summary>
        /// Starts listening.
        /// </summary>
        void StartListening();

        /// <summary>
        /// Stops listening.
        /// </summary>
        void StopListening();

        /// <summary>
        /// Returns true if the input channel is listening.
        /// </summary>
        bool IsListening { get; }
    }
}
