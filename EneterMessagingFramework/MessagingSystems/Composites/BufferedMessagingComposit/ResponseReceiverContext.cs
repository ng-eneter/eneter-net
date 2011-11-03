/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class ResponseReceiverContext
    {
        public ResponseReceiverContext(string responseReceiverId, IDuplexInputChannel duplexInputChannel, Action<string, bool> lastActivityUpdater)
        {
            ResponseReceiverId = responseReceiverId;
            mySender = new ResponseMessageSender(responseReceiverId, duplexInputChannel, lastActivityUpdater);
            LastActivityTime = DateTime.Now;
        }

        public void UpdateLastActivityTime()
        {
            LastActivityTime = DateTime.Now;
        }

        public void SendResponseMessage(object message)
        {
            mySender.SendResponseMessage(message);
        }

        public void StopSendingOfResponseMessages()
        {
            mySender.StopSending();
        }

        public string ResponseReceiverId { get; private set; }
        public DateTime LastActivityTime { get; private set; }

        private ResponseMessageSender mySender;
    }
}
