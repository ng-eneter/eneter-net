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
    /// The event when the typed sequenced message is received.
    /// </summary>
    /// <typeparam name="_MessageDataType">message type</typeparam>
    public sealed class TypedSequencedMessageReceivedEventArgs<_MessageDataType> : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="dataFragment">message fragment</param>
        public TypedSequencedMessageReceivedEventArgs(TypedMessageFragment<_MessageDataType> dataFragment)
        {
            SequenceId = dataFragment.SequenceId;
            IsSequenceCompleted = dataFragment.IsFinal;
            MessageData = dataFragment.FragmentData;
        }

        /// <summary>
        /// Constructs the event from the exception.
        /// </summary>
        /// <param name="receivingError">error detected during receiving of the message</param>
        public TypedSequencedMessageReceivedEventArgs(Exception receivingError)
        {
            SequenceId = "";
            IsSequenceCompleted = false; ;
            MessageData = default(_MessageDataType);

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
        public _MessageDataType MessageData { get; private set; }

        /// <summary>
        /// Returns the error detected during receiving of the message.
        /// E.g. during the deserialization of the message.
        /// </summary>
        public Exception ReceivingError { get; private set; }
    }
}
