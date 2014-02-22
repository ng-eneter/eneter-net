/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Reliable typed message receiver (it confirms whether the message was received).
    /// </summary>
    /// <remarks>
    /// Declares the reliable message receiver that can send messages of specified type and sends back response messages of specified type.
    /// Reliable means it provides events notifying whether the response message was delivered or not.
    /// The reliable typed message receiver can be used only with the reliable typed message sender.
    /// </remarks>
    /// <typeparam name="_ResponseType">type of the response message</typeparam>
    /// <typeparam name="_RequestType">type of the message</typeparam>
    public interface IReliableTypedMessageReceiver<_ResponseType, _RequestType> : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the message is received.
        /// </summary>
        event EventHandler<TypedRequestReceivedEventArgs<_RequestType>> MessageReceived;

        /// <summary>
        /// The event is invoked when the reliable typed message sender opened connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when the reliable typed message sender was disconnected.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// The event is invoked when the response message was delivered.
        /// </summary>
        event EventHandler<ReliableMessageIdEventArgs> ResponseMessageDelivered;

        /// <summary>
        /// The event is invoked when the response message was not delivered within specified time.
        /// </summary>
        event EventHandler<ReliableMessageIdEventArgs> ResponseMessageNotDelivered;

        /// <summary>
        /// Sends the response message of specified type.
        /// </summary>
        /// <param name="responseReceiverId">identifies the response receiver</param>
        /// <param name="responseMessage">respone message</param>
        /// <returns>id of the message. The id can be then used to check if the message was received.</returns>
        string SendResponseMessage(string responseReceiverId, _ResponseType responseMessage);
    }
}
