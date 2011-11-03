/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// Implements the factory to create senders and receivers of sequence of typed messages.
    /// The senders and receivers ensure the correct order of messages in the sequence.
    /// </summary>
    public class TypedSequencedMessagesFactory : ITypedSequencedMessagesFactory
    {
        /// <summary>
        /// Constructs the sequences typed messages factory with xml string serializer.
        /// </summary>
        public TypedSequencedMessagesFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the sequenced typed message factory with specified serializer.
        /// </summary>
        /// <param name="serializer">serializer</param>
        public TypedSequencedMessagesFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Creates the sender of messages of specified type.
        /// </summary>
        /// <typeparam name="_MessageDataType">The type of the message.</typeparam>
        /// <returns>typed sequenced message sender</returns>
        public ITypedSequencedMessageSender<_MessageDataType> CreateTypedSequencedMessageSender<_MessageDataType>()
        {
            using (EneterTrace.Entering())
            {
                return new TypedSequencedMessageSender<_MessageDataType>(mySerializer);
            }
        }

        /// <summary>
        /// Creates the receiver of messages of specified type.
        /// </summary>
        /// <typeparam name="_MessageDataType">The type of the message.</typeparam>
        /// <returns>typed sequenced message receiver</returns>
        public ITypedSequencedMessageReceiver<_MessageDataType> CreateTypedSequencedMessageReceiver<_MessageDataType>()
        {
            using (EneterTrace.Entering())
            {
                return new TypedSequencedMessageReceiver<_MessageDataType>(mySerializer);
            }
        }


        private ISerializer mySerializer;
    }
}
