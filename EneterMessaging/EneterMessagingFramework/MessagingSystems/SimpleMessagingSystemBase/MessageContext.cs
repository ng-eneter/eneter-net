/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/



namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class MessageContext
    {
        public MessageContext(object message, string senderAddress, ISender responseSender)
        {
            Message = message;
            SenderAddress = senderAddress;
            ResponseSender = responseSender;
        }

        public object Message { get; private set; }
        public string SenderAddress { get; private set; }
        public ISender ResponseSender { get; private set; }
    }
}
