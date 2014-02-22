/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    /// <summary>
    /// Factory for creating channel wrapper and unwrapper.
    /// </summary>
    public class ChannelWrapperFactory : IChannelWrapperFactory
    {
        /// <summary>
        /// Constructs the channel wrapper factory with XmlStringSerializer.
        /// </summary>
        public ChannelWrapperFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the channel wrapper factory with specified serializer.
        /// </summary>
        /// <param name="serializer">serializer used for wrapping channels with data messages.</param>
        public ChannelWrapperFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Creates duplex channel wrapper.
        /// </summary>
        /// <returns></returns>
        public IDuplexChannelWrapper CreateDuplexChannelWrapper()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexChannelWrapper(mySerializer);
            }
        }

        /// <summary>
        /// Creates duplex channel unwrapper.
        /// </summary>
        /// <returns></returns>
        public IDuplexChannelUnwrapper CreateDuplexChannelUnwrapper(IMessagingSystemFactory outputMessagingSystem)
        {
            using (EneterTrace.Entering())
            {
                return new DuplexChannelUnwrapper(outputMessagingSystem, mySerializer);
            }
        }

        private ISerializer mySerializer;
    }
}
