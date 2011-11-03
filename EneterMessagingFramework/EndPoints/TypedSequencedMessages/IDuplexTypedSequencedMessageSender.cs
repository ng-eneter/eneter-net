/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The interface declares the duplex sender that sends sequences of strongly typed messages.
    /// The sender is able to send sequences of typed messages and receive sequences of typed response messages.
    /// </summary>
    /// <typeparam name="_ResponseType">The type of receiving response messages.</typeparam>
    /// <typeparam name="_RequestType">The type of sending messages.</typeparam>
    public interface IDuplexTypedSequencedMessageSender<_ResponseType, _RequestType> : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// The event is invoked when the response message was received.
        /// </summary>
        event EventHandler<TypedSequencedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;

        /// <summary>
        /// Sends typed message.
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="sequenceId">identifies the sequence the message is part of</param>
        /// <param name="isSequenceCompleted">true - indicates the sequence is completed</param>
        void SendMessage(_RequestType message, string sequenceId, bool isSequenceCompleted);
    }
}
