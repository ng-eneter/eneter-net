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
    /// Event when the typed sequenced response message is received.
    /// </summary>
    /// <typeparam name="_ResponseType"></typeparam>
    public sealed class TypedSequencedResponseReceivedEventArgs<_ResponseType> : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="dataFragment">message fragment</param>
        public TypedSequencedResponseReceivedEventArgs(ResponseMessageFragment<_ResponseType> dataFragment)
        {
            SequenceId = dataFragment.SequenceId;
            IsSequenceCompleted = dataFragment.IsFinal;
            ResponseMessage = dataFragment.FragmentData;
        }

        /// <summary>
        /// Constructs the event from the exception.
        /// </summary>
        /// <param name="receivingError">error detected during receiving of the response message</param>
        public TypedSequencedResponseReceivedEventArgs(Exception receivingError)
        {
            SequenceId = "";
            IsSequenceCompleted = false; ;
            ResponseMessage = default(_ResponseType);

            ReceivingError = receivingError;
        }

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
        public _ResponseType ResponseMessage { get; private set; }

        /// <summary>
        /// Returns the error detected during receiving of the message.
        /// E.g. during the deserialization of the message.
        /// </summary>
        public Exception ReceivingError { get; private set; }
    }
}
