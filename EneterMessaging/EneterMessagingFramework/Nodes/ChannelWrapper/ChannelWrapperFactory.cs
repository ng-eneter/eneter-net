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
                Serializer = serializer;
                SerializerProvider = null;
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
                return new DuplexChannelWrapper(Serializer);
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
                return new DuplexChannelUnwrapper(outputMessagingSystem, Serializer);
            }
        }

        /// <summary>
        /// Serializer which is used to serialize/deserialize DataWrapper.
        /// </summary>
        public ISerializer Serializer { get; set; }

        /// <summary>
        /// Gets/sets callback for retrieving serializer based on response receiver id.
        /// </summary>
        /// <remarks>
        /// This callback is used by DuplexChannelUnwrapper when it needs to serialize/deserialize the communication with DuplexChannelWrapper.
        /// Providing this callback allows to use a different serializer for each connected client.
        /// This can be used e.g. if the communication with each client needs to be encrypted using a different password.<br/>
        /// <br/>
        /// The default value is null and it means SerializerProvider callback is not used and one serializer which specified in the Serializer property is used for all serialization/deserialization.<br/>
        /// If SerializerProvider is not null then the setting in the Serializer property is ignored.
        /// </remarks>
        public GetSerializerCallback SerializerProvider
        {
            get
            {
                if (Serializer is CallbackSerializer)
                {
                    return ((CallbackSerializer)Serializer).GetSerializerCallback;
                }

                return null;
            }
            set
            {
                Serializer = new CallbackSerializer(value);
            }
        }
    }
}
