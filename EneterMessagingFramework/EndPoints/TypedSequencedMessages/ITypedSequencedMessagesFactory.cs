/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The interface declares the factory to create senders and receivers of sequence of typed messages.
    /// The senders and receivers ensure the correct order of messages in the sequence.
    /// </summary>
    public interface ITypedSequencedMessagesFactory
    {
        /// <summary>
        /// Creates the sender of messages of specified type.
        /// </summary>
        /// <typeparam name="_MessageDataType">The type of the message.</typeparam>
        /// <returns>typed sequenced message sender</returns>
        ITypedSequencedMessageSender<_MessageDataType> CreateTypedSequencedMessageSender<_MessageDataType>();

        /// <summary>
        /// Creates the receiver of messages of specified type.
        /// </summary>
        /// <typeparam name="_MessageDataType">The type of the message.</typeparam>
        /// <returns>typed sequenced message receiver</returns>
        ITypedSequencedMessageReceiver<_MessageDataType> CreateTypedSequencedMessageReceiver<_MessageDataType>();
    }
}
