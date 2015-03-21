/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using System;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    public class MultiTypedMessagesFactory : IMultiTypedMessagesFactory
    {
        public MultiTypedMessagesFactory()
            : this(TimeSpan.FromMilliseconds(-1), new XmlStringSerializer())
        {
        }

        public MultiTypedMessagesFactory(ISerializer serializer)
            : this(TimeSpan.FromMilliseconds(-1), serializer)
        {
        }

        public MultiTypedMessagesFactory(TimeSpan responseReceiveTimeout)
            : this(responseReceiveTimeout, new XmlStringSerializer())
        {
        }

        public MultiTypedMessagesFactory(TimeSpan responseReceiveTimeout, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myResponseReceiveTimeout = responseReceiveTimeout;
                mySerializer = serializer;
                SyncDuplexTypedSenderThreadMode = new SyncDispatching();
            }
        }

        public IMultiTypedMessageSender CreateMultiTypedMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new MultiTypedMessageSender(mySerializer);
            }
        }

        public ISyncMultitypedMessageSender CreateSyncMultiTypedMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new SyncMultiTypedMessageSender(myResponseReceiveTimeout, mySerializer, SyncDuplexTypedSenderThreadMode);
            }
        }

        public IMultiTypedMessageReceiver CreateMultiTypedMessageReceiver()
        {
            using (EneterTrace.Entering())
            {
                return new MultiTypedMessageReceiver(mySerializer);
            }
        }


        public IThreadDispatcherProvider SyncDuplexTypedSenderThreadMode { get; set; }
        
        private ISerializer mySerializer;
        private TimeSpan myResponseReceiveTimeout;
    }
}
