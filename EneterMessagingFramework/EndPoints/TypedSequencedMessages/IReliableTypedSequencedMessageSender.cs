/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The interface declares the reliable sender that sends sequences of typed messages.
    /// The sender is able to send sequences of typed messages and receive sequences of typed response messages.
    /// In addition, the sender provides events notifying whether the message was delivered.
    /// The reliable typed sequenced message sender can be used only with reliable typed sequenced message receiver.
    /// </summary>
    /// <typeparam name="_ResponseType"></typeparam>
    /// <typeparam name="_RequestType"></typeparam>
    public interface IReliableTypedSequencedMessageSender<_ResponseType, _RequestType> : IAttachableReliableOutputChannel
    {
        /// <summary>
        /// The event is invoked when the response message was received.
        /// </summary>
        event EventHandler<TypedSequencedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;

        /// <summary>
        /// The event is invoked when the message was delivered.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageDelivered;

        /// <summary>
        /// The event is invoked when the message was not delivered within the specified time.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageNotDelivered;

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="sequenceId">identifies the sequence the message is part of</param>
        /// <param name="isSequenceCompleted">true - indicates the sequence is completed</param>
        /// <returns>message id</returns>
        string SendMessage(_RequestType message, string sequenceId, bool isSequenceCompleted);
    }
}
