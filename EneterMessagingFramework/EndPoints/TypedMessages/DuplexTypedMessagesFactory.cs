/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Implements the factory to create duplex strongly typed message sender and receiver.
    /// </summary>
    public class DuplexTypedMessagesFactory : IDuplexTypedMessagesFactory
    {
        /// <summary>
        /// Constructs the factory with xml serializer. <br/>
        /// </summary>
        public DuplexTypedMessagesFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the method factory with specified serializer.
        /// </summary>
        /// <param name="serializer">serializer</param>
        public DuplexTypedMessagesFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Creates duplex typed message sender.
        /// </summary>
        /// <typeparam name="_ResponseType">Type of receiving response messages.</typeparam>
        /// <typeparam name="_RequestType">Type of sending messages.</typeparam>
        /// <returns>duplex typed message sender</returns>
        public IDuplexTypedMessageSender<_ResponseType, _RequestType> CreateDuplexTypedMessageSender<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexTypedMessageSender<_ResponseType, _RequestType>(mySerializer);
            }
        }

        /// <summary>
        /// Creates duplex typed message receiver.
        /// </summary>
        /// <typeparam name="_ResponseType">Type of response messages.</typeparam>
        /// <typeparam name="_RequestType">Type of receiving messages.</typeparam>
        /// <returns>duplex typed message receiver</returns>
        public IDuplexTypedMessageReceiver<_ResponseType, _RequestType> CreateDuplexTypedMessageReceiver<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexTypedMessageReceiver<_ResponseType, _RequestType>(mySerializer);
            }
        }


        private ISerializer mySerializer;
    }
}
