/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// The interface declares the reliable string message receiver.
    /// The reliable string message receiver can receiver string messages and response string messages.
    /// In addition it provides events notifying whether the response messages were delivered.
    /// The reliable string message receiver can be used only with the reliable string message sender.
    /// </summary>
    public interface IReliableStringMessageReceiver : IAttachableReliableInputChannel
    {
        /// <summary>
        /// The event is invoked when the message was received.
        /// </summary>
        event EventHandler<StringRequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// The event is invoked when a reliable string message sender opened the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when a reliable string message sender closed the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// The event is invoked when the response message was delivered.
        /// </summary>
        event EventHandler<MessageIdEventArgs> ResponseMessageDelivered;

        /// <summary>
        /// The event is invoked when the respone message was not delivered within the specified time.
        /// </summary>
        event EventHandler<MessageIdEventArgs> ResponseMessageNotDelivered;

        /// <summary>
        /// Sends the response message back to the connected reliable string message sender.
        /// </summary>
        /// <param name="responseReceiverId">identifies the duplex string message sender that will receive the response</param>
        /// <param name="responseMessage">response message</param>
        /// <returns>id of the respones message</returns>
        string SendResponseMessage(string responseReceiverId, string responseMessage);
    }
}
