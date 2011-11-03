/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// The interface declares the strongly typed reliable message sender.
    /// The reliable sender can send typed messages and receive typed response messages.
    /// In addition it provides events notifying whether the message was delivered.
    /// The reliable typed message sender can be used only with the reliable typed message receiver.
    /// </summary>
    /// <typeparam name="_ResponseType">type of the response message</typeparam>
    /// <typeparam name="_RequestType">type of the message</typeparam>
    public interface IReliableTypedMessageSender<_ResponseType, _RequestType> : IAttachableReliableOutputChannel
    {
        /// <summary>
        /// The event is invoked when the response message is received.
        /// </summary>
        event EventHandler<TypedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;

        /// <summary>
        /// The event is invoked when the message was delivered.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageDelivered;

        /// <summary>
        /// The event is invoked if the event is not delivered within a specified time.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageNotDelivered;
        
        /// <summary>
        /// Sends the message to the reliable typed message receiver.
        /// </summary>
        /// <param name="message">message of desired type</param>
        /// <returns>id of the message</returns>
        string SendRequestMessage(_RequestType message);
    }
}
