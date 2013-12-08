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

            SetConnectionState(false);
        }

        public void SetConnectionState(bool isConnected)
        {
            lock (myResponseReceiverManipulatorLock)
            {
                LastConnectionChangeTime = DateTime.Now;
                myIsResponseReceiverConnected = isConnected;
            }
        }

        public bool IsResponseReceiverConnected
        {
            get
            {
                lock (myResponseReceiverManipulatorLock)
                {
                    return myIsResponseReceiverConnected;
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

        

        public string ResponseReceiverId { get; private set; }
        public string ClientAddress { get; set; }
        public DateTime LastConnectionChangeTime { get; private set; }

        private ResponseMessageSender mySender;

        private bool myIsResponseReceiverConnected;
        private object myResponseReceiverManipulatorLock = new object();
    }
}
