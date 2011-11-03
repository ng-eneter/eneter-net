/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The event when a typed sequenced message is received.
    /// </summary>
    /// <typeparam name="_RequestType">The type of the message.</typeparam>
    public sealed class TypedSequencedRequestReceivedEventArgs<_RequestType> : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="responseReceiverId">identifies the client where the response message can be sent</param>
        /// <param name="dataFragment">message fragment</param>
        public TypedSequencedRequestReceivedEventArgs(string responseReceiverId, RequestMessageFragment<_RequestType> dataFragment)
        {
            ResponseReceiverId = responseReceiverId;
            SequenceId = dataFragment.SequenceId;
            IsSequenceCompleted = dataFragment.IsFinal;
            RequestMessage = dataFragment.FragmentData;
        }

        /// <summary>
        /// Constructs the event from the exception.
        /// </summary>
        /// <param name="responseReceiverId">identifies the client where the response message can be sent</param>
        /// <param name="receivingError">error detected during receiving the message</param>
        public TypedSequencedRequestReceivedEventArgs(string responseReceiverId, Exception receivingError)
        {
            ResponseReceiverId = responseReceiverId;
            SequenceId = "";
            IsSequenceCompleted = false; ;
            RequestMessage = default(_RequestType);

            ReceivingError = receivingError;
        }

        /// <summary>
        /// Returns the client identifier where the response can be sent.
        /// </summary>
        public string ResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns the sequence identifier.
        /// </summary>
        public string SequenceId { get; private set; }

        /// <summary>
        /// Returns true if the sequence is completed.
        /// </summary>
        public bool IsSequenceCompleted { get; private set; }

        /// <summary>
        /// Returns message inputData of the specified type.
        /// </summary>
        public _RequestType RequestMessage { get; private set; }

        /// <summary>
        /// Returns the error detected during receiving of the message.
        /// E.g. during the deserialization of the message.
        /// </summary>
        public Exception ReceivingError { get; private set; }
    }
}
