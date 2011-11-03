/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Implements the factory to create reliable typed message sender and receiver.
    /// </summary>
    public class ReliableTypedMessagesFactory : IReliableTypedMessagesFactory
    {
        /// <summary>
        /// Constructs the factory with xml string serializer. <br/>
        /// </summary>
        public ReliableTypedMessagesFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory with specified serializer.
        /// </summary>
        /// <param name="serializer">serializer</param>
        public ReliableTypedMessagesFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Creates the reliable message sender.
        /// </summary>
        /// <typeparam name="_ResponseType">type of response message</typeparam>
        /// <typeparam name="_RequestType">type of request message</typeparam>
        /// <returns>reliable typed message sender</returns>
        public IReliableTypedMessageSender<_ResponseType, _RequestType> CreateReliableDuplexTypedMessageSender<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new ReliableDuplexTypedMessageSender<_ResponseType, _RequestType>(mySerializer);
            }
        }

        /// <summary>
        /// Creates the reliable message receiver.
        /// </summary>
        /// <typeparam name="_ResponseType">type of response message</typeparam>
        /// <typeparam name="_RequestType">type of request message</typeparam>
        /// <returns>reliable typed message receiver</returns>
        public IReliableTypedMessageReceiver<_ResponseType, _RequestType> CreateReliableDuplexTypedMessageReceiver<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new ReliableDuplexTypedMessageReceiver<_ResponseType, _RequestType>(mySerializer);
            }
        }

        private ISerializer mySerializer;
    }
}
