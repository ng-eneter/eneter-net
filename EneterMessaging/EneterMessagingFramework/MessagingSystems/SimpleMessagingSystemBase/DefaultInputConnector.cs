/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using System.IO;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultInputConnector : IInputConnector
    {
        private class DefaultResponseSender : ISender
        {
            public DefaultResponseSender(string clientConnectorAddress, IMessagingProvider messagingProvider)
            {
                using (EneterTrace.Entering())
                {
                    myClientConnectorAddress = clientConnectorAddress;
                    myMessagingProvider = messagingProvider;
                }
            }

            public bool IsStreamWritter { get { return false; } }

            public void SendMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    myMessagingProvider.SendMessage(myClientConnectorAddress, message);
                }
            }

            public void SendMessage(Action<Stream> toStreamWriter)
            {
                throw new NotSupportedException("To stream writer is not supported.");
            }

            private string myClientConnectorAddress;
            private IMessagingProvider myMessagingProvider;
        }



        public DefaultInputConnector(string serviceConnectorAddress, IMessagingProvider messagingProvider)
        {
            using (EneterTrace.Entering())
            {
                myServiceConnectorAddress = serviceConnectorAddress;
                myMessagingProvider = messagingProvider;
            }
        }


        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    if (messageHandler == null)
                    {
                        throw new ArgumentNullException("Input parameter messageHandler is null.");
                    }

                    myMessagingProvider.RegisterMessageHandler(myServiceConnectorAddress, x => messageHandler(new MessageContext(x, "", null)));
                    myIsListeningFlag = true;
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    myIsListeningFlag = false;
                    myMessagingProvider.UnregisterMessageHandler(myServiceConnectorAddress);
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListeningManipulatorLock)
                {
                    return myIsListeningFlag;
                }
            }
        }

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            using (EneterTrace.Entering())
            {
                ISender aResponseSender = new DefaultResponseSender(responseReceiverAddress, myMessagingProvider);
                return aResponseSender;
            }
        }

        private string myServiceConnectorAddress;
        private IMessagingProvider myMessagingProvider;
        private object myListeningManipulatorLock = new object();
        private bool myIsListeningFlag;
    }
}
