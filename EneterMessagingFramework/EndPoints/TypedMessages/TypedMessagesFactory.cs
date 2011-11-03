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
    /// Implements the factory to create strongly typed message senders and receivers.
    /// </summary>
    public class TypedMessagesFactory : ITypedMessagesFactory
    {
        /// <summary>
        /// Constructs the typed messages factory with BinarySerializer.
        /// <br/>
        /// <b>Note: The serializer is XmlStringSerializer in case of Silverlight.</b> 
        /// </summary>
        public TypedMessagesFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the typed message factory with specified serializer.
        /// </summary>
        /// <param name="serializer">serializer</param>
        public TypedMessagesFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Creates the typed message sender.
        /// The sender sends the messages via attached one-way output channel.
        /// </summary>
        public ITypedMessageSender<_MessageDataType> CreateTypedMessageSender<_MessageDataType>()
        {
            using (EneterTrace.Entering())
            {
                return new TypedMessageSender<_MessageDataType>(mySerializer);
            }
        }

        /// <summary>
        /// Creates the typed message receiver.
        /// The receiver receives messages via attached one-way input channel.
        /// </summary>
        public ITypedMessageReceiver<_MessageDataType> CreateTypedMessageReceiver<_MessageDataType>()
        {
            using (EneterTrace.Entering())
            {
                return new TypedMessageReceiver<_MessageDataType>(mySerializer);
            }
        }

        private ISerializer mySerializer;
    }
}
