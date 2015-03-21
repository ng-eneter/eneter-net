/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/


#if !SILVERLIGHT

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Implements factory for creating the message bus.
    /// </summary>
    public class MessageBusFactory : IMessageBusFactory
    {
        /// <summary>
        /// Constructs the factory with default parameters.
        /// </summary>
        /// <remarks>
        /// It uses internal MessageBusCustomSerializer which is optimazed to serialize/deserialze only the MessageBusMessage.
        /// </remarks>
        public MessageBusFactory()
            : this(new MessageBusCustomSerializer())
        {
        }

        /// <summary>
        /// Construct the factory.
        /// </summary>
        /// <param name="serializer">Serializer which will be used to serialize/deserialize MessageBusMessage.</param>
        public MessageBusFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Instantiates the message bus.
        /// </summary>
        /// <returns></returns>
        public IMessageBus CreateMessageBus()
        {
            using (EneterTrace.Entering())
            {
                return new MessageBus(mySerializer);
            }
        }

        private ISerializer mySerializer;
    }
}

#endif