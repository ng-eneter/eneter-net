/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The interface declares the typed messsage sender that can send the messages in the sequence.
    /// </summary>
    public interface ITypedSequencedMessageSender<_MessageDataType> : IAttachableOutputChannel
    {
        /// <summary>
        /// Sends the message of the specified type as a fragment of a sequence.
        /// The sequence identifier is an id for the sequence and must be same for all sent messages belonging to the sequence.
        /// </summary>
        /// <param name="message">message fragment</param>
        /// <param name="sequenceId">sequence identifier</param>
        /// <param name="isSequenceCompleted">flag indicating whether this is the last fragment of the message and sequence is completed</param>
        void SendMessage(_MessageDataType message, string sequenceId, bool isSequenceCompleted);
    }
}
