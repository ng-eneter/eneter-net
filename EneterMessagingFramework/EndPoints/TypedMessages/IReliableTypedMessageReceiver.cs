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

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// The interface declares the strongly typed reliable message receiver.
    /// The receiver is able to receive messages of the specified type and send back response messages of specified type.
    /// In addition it provides events notifying whether the respone message was delivered.
    /// The reliable typed message receiver can be used only with the reliable typed message sender.
    /// </summary>
    /// <typeparam name="_ResponseType"></typeparam>
    /// <typeparam name="_RequestType"></typeparam>
    public interface IReliableTypedMessageReceiver<_ResponseType, _RequestType> : IAttachableReliableInputChannel
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
        event EventHandler<MessageIdEventArgs> ResponseMessageDelivered;

        /// <summary>
        /// The event is invoked when the response message was not delivered within specified time.
        /// </summary>
        event EventHandler<MessageIdEventArgs> ResponseMessageNotDelivered;

        /// <summary>
        /// Sends the typed response message.
        /// </summary>
        /// <param name="responseReceiverId">identifies the response receiver</param>
        /// <param name="responseMessage">respone message</param>
        /// <returns>id od the sent response message</returns>
        string SendResponseMessage(string responseReceiverId, _ResponseType responseMessage);
    }
}
