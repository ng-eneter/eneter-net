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

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The interface declares the reliable receiver for sequence of typed messages.
    /// The receiver is able to receive sequence of messages of specified type and response the sequence of messages
    /// of specified type. <br/>
    /// It is guaranteed the sequence is received in the same order as was sent. <br/>
    /// In addition, the reliable typed sequenced message reciever provides events notifying whether the response message was delivered.
    /// The reliable typed sequenced message reciever can be used only with the reliable typed sequenced message sender.
    /// <br/>
    /// <b>Note: Be aware that if the 'thread pool messaging system' is chosen the incoming messages
    /// are processed in more threads in parallel. Therefore the 'thread pool messaging system'
    /// cannot guarantee the order of incoming messages.</b> <br/>
    /// Consider to use the 'thread messaging system' instead.
    /// </summary>
    /// <typeparam name="_ResponseType"></typeparam>
    /// <typeparam name="_RequestType"></typeparam>
    public interface IReliableTypedSequencedMessageReceiver<_ResponseType, _RequestType> : IAttachableReliableInputChannel
    {
        /// <summary>
        /// The event is invoked when the message was received.
        /// </summary>
        event EventHandler<TypedSequencedRequestReceivedEventArgs<_RequestType>> MessageReceived;

        /// <summary>
        /// The event is invoked when the reliable typed sequenced message sender opened the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when the reliable typed sequenced message sender closed the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// The event is invoked when the respponse message was delivered.
        /// </summary>
        event EventHandler<MessageIdEventArgs> ResponseMessageDelivered;

        /// <summary>
        /// The event is invoked when the response message is not delivered within specified time.
        /// </summary>
        event EventHandler<MessageIdEventArgs> ResponseMessageNotDelivered;

        /// <summary>
        /// Sends the response message.
        /// </summary>
        /// <param name="responseReceiverId">identifies the response receiver</param>
        /// <param name="responseMessage">message</param>
        /// <param name="sequenceId">identifies the sequence the message is part of</param>
        /// <param name="isSequenceCompleted">true - indicates the sequence is completed</param>
        /// <returns>message id</returns>
        string SendResponseMessage(string responseReceiverId, _ResponseType responseMessage, string sequenceId, bool isSequenceCompleted);
    }
}
