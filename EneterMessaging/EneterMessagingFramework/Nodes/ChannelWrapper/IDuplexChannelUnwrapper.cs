/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    /// <summary>
    /// Declares the duplex channel unwrapper.
    /// </summary>
    /// <remarks>
    /// The duplex channel wrapper is listening to more duplex input channels. When it receives some message,
    /// it wraps the message and sends it via the only duplex output channel.
    /// On the other side the message is received by duplex channel unwrapper. The unwrapper unwraps the message
    /// and uses the duplex output channel to forward the message to the correct receiver.<br/>
    /// The receiver can also send the response message. Then it goes the same way back.<br/>
    /// Notice, the 'duplex channel unwrapper' can communication only with 'duplex channel wrapper'.
    /// It cannot communicate with one-way 'channel wrapper'.
    /// </remarks>
    public interface IDuplexChannelUnwrapper : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the duplex channel wrapper opened the connection with this
        /// unwrapper via its duplex output channel.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when the duplex channel wrapper closed the connection with this
        /// unwrapper via its duplex output channel.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Returns response receiver id of the client connected to the unwrapper.
        /// </summary>
        /// <param name="responseReceiverId">responseRecieverId from unwrapped message</param>
        /// <returns>responseReceiverId of the client connected to the channel unwrapper. Returns null if it does not exist.</returns>
        string GetAssociatedResponseReceiverId(string responseReceiverId);
    }
}
