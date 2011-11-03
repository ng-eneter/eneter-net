/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The interface declares the duplex receiver for sequence of typed messages.
    /// The receiver is able to receive sequence of messages of specified type and response the sequence of messages
    /// of specified type. <br/>
    /// It is guaranteed the sequence is received in the same order as was sent. <br/>
    /// <b>Note: Be aware that if the 'thread pool messaging system' is chosen the incoming messages
    /// are processed in more threads in parallel. Therefore the 'thread pool messaging system'
    /// cannot guarantee the order of incoming messages. <br/>
    /// Consider to use the 'thread messaging system' instead.
    /// </b>
    /// </summary>
    /// <typeparam name="_ResponseType">The type of sending response messages.</typeparam>
    /// <typeparam name="_RequestType">The type of the receiving messages.</typeparam>
    public interface IDuplexTypedSequencedMessageReceiver<_ResponseType, _RequestType> : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the message was received.
        /// </summary>
        event EventHandler<TypedSequencedRequestReceivedEventArgs<_RequestType>> MessageReceived;

        /// <summary>
        /// The event is invoked when the duplex typed sequenced message sender opened the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when the duplex typed sequenced message sender closed the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Sends the response message to the specified duplex typed sequenced message sender.
        /// </summary>
        /// <param name="responseReceiverId">specifies the duplex typed sequenced message sender that will receive the response</param>
        /// <param name="responseMessage">message</param>
        /// <param name="sequenceId">identifies the sequence the message is part of</param>
        /// <param name="isSequenceCompleted">true - indicates the sequence is completed</param>
        void SendResponseMessage(string responseReceiverId, _ResponseType responseMessage, string sequenceId, bool isSequenceCompleted);
    }
}
