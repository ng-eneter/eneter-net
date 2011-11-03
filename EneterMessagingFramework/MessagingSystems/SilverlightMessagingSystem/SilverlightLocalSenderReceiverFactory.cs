/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightLocalSenderReceiverFactory : ILocalSenderReceiverFactory
    {
        public ILocalMessageSender CreateLocalMessageSender(string receiverName)
        {
            using (EneterTrace.Entering())
            {
                return new SilverlightLocalMessageSender(receiverName);
            }
        }

        public ILocalMessageReceiver CreateLocalMessageReceiver(string receiverName)
        {
            using (EneterTrace.Entering())
            {
                return new SilverlightLocalMessageReceiver(receiverName);
            }
        }
    }
}

#endif
