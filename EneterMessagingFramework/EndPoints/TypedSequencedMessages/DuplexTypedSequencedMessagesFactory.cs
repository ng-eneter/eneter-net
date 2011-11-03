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
    /// Implements the factory to create duplex typed sequenced message sender and receiver.
    /// </summary>
    public class DuplexTypedSequencedMessagesFactory : IDuplexTypedSequencedMessagesFactory
    {
        /// <summary>
        /// Constructs the factory with the xml string serializer.
        /// </summary>
        public DuplexTypedSequencedMessagesFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory with the specified serializer.
        /// </summary>
        /// <param name="serializer"></param>
        public DuplexTypedSequencedMessagesFactory(ISerializer serializer)
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
        public IDuplexTypedSequencedMessageSender<_ResponseType, _RequestType> CreateDuplexTypedSequencedMessageSender<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexTypedSequencedMessageSender<_ResponseType, _RequestType>(mySerializer);
            }
        }

        /// <summary>
        /// Creates the duplex typed sequences message receiver.
        /// </summary>
        /// <typeparam name="_ResponseType">The type of sending response messages.</typeparam>
        /// <typeparam name="_RequestType">The type of receiving messages.</typeparam>
        /// <returns>duplex typed sequenced message receiver</returns>
        public IDuplexTypedSequencedMessageReceiver<_ResponseType, _RequestType> CreateDuplexTypedSequencedMessageReceiver<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexTypedSequencedMessageReceiver<_ResponseType, _RequestType>(mySerializer);
            }
        }


        private ISerializer mySerializer;
    }
}
