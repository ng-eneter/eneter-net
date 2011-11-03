/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// Implements the factory to create reliable typed sequenced message sender and receiver.
    /// </summary>
    public class ReliableTypedSequencedMessagesFactory : IReliableTypedSequencedMessagesFactory
    {
        /// <summary>
        /// Constructs the factory with the xml string serializer.
        /// </summary>
        public ReliableTypedSequencedMessagesFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory with the specified serializer.
        /// </summary>
        /// <param name="serializer"></param>
        public ReliableTypedSequencedMessagesFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Creates the duplex typed sequenced message sender.
        /// </summary>
        /// <typeparam name="_ResponseType">The type of receiving response messages.</typeparam>
        /// <typeparam name="_RequestType">The type of sending messages.</typeparam>
        /// <returns>duplex typed sequenced message sender</returns>
        public IReliableTypedSequencedMessageSender<_ResponseType, _RequestType> CreateReliableTypedSequencedMessageSender<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new ReliableDuplexTypedSequencedMessageSender<_ResponseType, _RequestType>(mySerializer);
            }
        }

        /// <summary>
        /// Creates the duplex typed sequences message receiver.
        /// </summary>
        /// <typeparam name="_ResponseType">The type of sending response messages.</typeparam>
        /// <typeparam name="_RequestType">The type of receiving messages.</typeparam>
        /// <returns>duplex typed sequenced message receiver</returns>
        public IReliableTypedSequencedMessageReceiver<_ResponseType, _RequestType> CreateReliableTypedSequencedMessageReceiver<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new ReliableDuplexTypedSequencedMessageReceiver<_ResponseType, _RequestType>(mySerializer);
            }
        }


        private ISerializer mySerializer;
    }
}
