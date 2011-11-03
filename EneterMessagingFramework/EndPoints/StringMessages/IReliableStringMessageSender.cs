/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// The interface declares the reliable string message sender.
    /// The reliable string message sender can send string messages and receive string response messages.
    /// In addition it provides events notifying whether the messages were delivered.
    /// The reliable string message sender can be used only with the reliable string message receiver.
    /// </summary>
    public interface IReliableStringMessageSender : IAttachableReliableOutputChannel
    {
        /// <summary>
        /// The event is invoked when a response message from the reliable string message receiver was received.
        /// </summary>
        event EventHandler<StringResponseReceivedEventArgs> ResponseReceived;

        /// <summary>
        /// The event is invoked when the sent message was delivered.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageDelivered;

        /// <summary>
        /// The event is invoked when the sent message was not delivered within the specified time.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageNotDelivered;

        /// <summary>
        /// Sends the string message.
        /// </summary>
        /// <param name="message">text message</param>
        /// <returns>id of the message</returns>
        string SendMessage(string message);
    }
}
