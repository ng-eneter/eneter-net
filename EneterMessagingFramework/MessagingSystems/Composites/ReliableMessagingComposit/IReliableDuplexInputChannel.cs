/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit
{

    /// <summary>
    /// Declares the reliable duplex input channel.
    /// </summary>
    /// <remarks>
    /// It behaves like <see cref="IDuplexInputChannel"/> and can also be used everywhere where <see cref="IDuplexInputChannel"/>
    /// is required.<br/>
    /// In addition, the reliable duplex input channel provides events <see cref="ResponseMessageDelivered"/> and
    /// <see cref="ResponseMessageNotDelivered"/>.
    /// The method <see cref="SendResponseMessage"/> returns unique id of the sent message.
    /// <br/><br/>
    /// The reliable duplex input channel can receive messages from <see cref="IReliableDuplexOutputChannel"/>.
    /// It cannot receive messages from <see cref="IDuplexOutputChannel"/>.
    /// </remarks>
    public interface IReliableDuplexInputChannel : IDuplexInputChannel, ICompositeDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the response message was delivered to the reliable duplex output channel.
        /// </summary>
        event EventHandler<MessageIdEventArgs> ResponseMessageDelivered;

        /// <summary>
        /// The event is invoked if the response message was not delivered until the specified time.
        /// </summary>
        event EventHandler<MessageIdEventArgs> ResponseMessageNotDelivered;

        /// <summary>
        /// Sends the response message.
        /// </summary>
        /// <param name="responseReceiverId">identifies the receiver of the response message</param>
        /// <param name="message">message</param>
        /// <returns>id of the message</returns>
        new string SendResponseMessage(string responseReceiverId, object message);
    }
}
