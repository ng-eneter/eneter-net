/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit
{
    /// <summary>
    /// Declares the reliable duplex output channel.
    /// </summary>
    /// <remarks>
    /// It behaves like <see cref="IDuplexOutputChannel"/> and can also be used everywhere where <see cref="IDuplexOutputChannel"/>
    /// is required.<br/>
    /// In addition, the reliable duplex output channel provides events <see cref="MessageDelivered"/> and <see cref="MessageNotDelivered"/>.
    /// The method <see cref="SendMessage"/> returns the message id.
    /// <br/><br/>
    /// The reliable duplex output channel can send messages to <see cref="IReliableDuplexInputChannel"/>.
    /// It cannot send messages to <see cref="IDuplexInputChannel"/>.
    /// </remarks>
    public interface IReliableDuplexOutputChannel : IDuplexOutputChannel, ICompositeDuplexOutputChannel
    {
        /// <summary>
        /// The event is invoked when the message was delivered to the reliable duplex input channel.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageDelivered;

        /// <summary>
        /// The event is invoked if the message was not delivered until the specified time.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageNotDelivered;

        /// <summary>
        /// Sends the message to the reliable duplex output channel.
        /// </summary>
        /// <param name="message">message</param>
        /// <returns>id of the message</returns>
        new string SendMessage(object message);
    }
}
