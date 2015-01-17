/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    public class MultiTypedMessagesFactory : IMultiTypedMessagesFactory
    {
        public MultiTypedMessagesFactory()
            : this(new XmlStringSerializer())
        {
        }

        public MultiTypedMessagesFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        public IMultiTypedMessageSender CreateMultiTypedMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new MultiTypedMessageSender(mySerializer);
            }
        }

        public IMultiTypedMessageReceiver CreateMultiTypedMessageReceiver()
        {
            using (EneterTrace.Entering())
            {
                return new MultiTypedMessageReceiver(mySerializer);
            }
        }

        private ISerializer mySerializer;
    }
}
