/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/


#if !SILVERLIGHT

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Implements the factory for creating the message bus.
    /// </summary>
    public class MessageBusFactory : IMessageBusFactory
    {
        /// <summary>
        /// Constructs the factory with default parameters.
        /// </summary>
        /// <remarks>
        /// Default EneterProtocolFormatter is used.
        /// </remarks>
        public MessageBusFactory()
            : this(new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Construct the factory.
        /// </summary>
        /// <param name="protocolFormatter">protocol formatter used for the communication between channels.</param>
        public MessageBusFactory(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
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
                return new MessageBus(myProtocolFormatter);
            }
        }

        private IProtocolFormatter myProtocolFormatter;
    }
}

#endif