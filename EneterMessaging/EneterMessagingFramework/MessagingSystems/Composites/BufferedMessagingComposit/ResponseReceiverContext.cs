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
        public ResponseReceiverContext(string responseReceiverId, string clientAddress, IDuplexInputChannel duplexInputChannel)
        {
            ResponseReceiverId = responseReceiverId;
            ClientAddress = clientAddress;
            mySender = new ResponseMessageSender(responseReceiverId, duplexInputChannel);

            IsResponseReceiverConnected = false;
        }

        public bool IsResponseReceiverConnected
        {
            get
            {
                return myIsResponseReceiverConnected;
            }

            set
            {
                myIsResponseReceiverConnected = value;

                if (!myIsResponseReceiverConnected)
                {
                    DisconnectionStartedAt = DateTime.Now;
                }
            }
        }

        public void SendResponseMessage(object message)
        {
            mySender.SendResponseMessage(message);
        }

        public void StopSendingOfResponseMessages()
        {
            mySender.StopSending();
        }

        public DateTime DisconnectionStartedAt { get; private set; }

        public string ResponseReceiverId { get; private set; }
        public string ClientAddress { get; set; }

        private ResponseMessageSender mySender;

        private volatile bool myIsResponseReceiverConnected;
        private object myResponseReceiverManipulatorLock = new object();
    }
}
