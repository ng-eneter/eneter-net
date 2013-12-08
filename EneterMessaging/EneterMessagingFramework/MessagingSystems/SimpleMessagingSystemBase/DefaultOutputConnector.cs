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
    internal class DefaultOutputConnector : IOutputConnector
    {
        public DefaultOutputConnector(string serviceConnectorAddress, string clientConnectorAddress, IMessagingProvider messagingProvider)
        {
            using (EneterTrace.Entering())
            {
                myServiceConnectorAddress = serviceConnectorAddress;
                myClientConnectorAddress = clientConnectorAddress;
                myMessagingProvider = messagingProvider;
            }
        }

        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (responseMessageHandler == null)
                    {
                        throw new ArgumentNullException("Input parameter responseMessageHandler is null.");
                    }

                    myMessagingProvider.RegisterMessageHandler(myClientConnectorAddress, x => responseMessageHandler(new MessageContext(x, "", null)));
                    myIsResponseListenerRegistered = true;
                    myIsConnected = true;
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myIsConnected = false;
                    if (myIsResponseListenerRegistered)
                    {
                        myMessagingProvider.UnregisterMessageHandler(myClientConnectorAddress);
                        myIsResponseListenerRegistered = false;
                    }
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulatorLock)
                {
                    return myIsConnected;
                }
            }
        }

        public bool IsStreamWritter { get { return false; } }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myMessagingProvider.SendMessage(myServiceConnectorAddress, message);
                }
            }
        }

        public void SendMessage(Action<Stream> toStreamWriter)
        {
            throw new NotSupportedException("To stream writer is not supported.");
        }


        private string myServiceConnectorAddress;
        private string myClientConnectorAddress;
        private IMessagingProvider myMessagingProvider;
        private bool myIsConnected;
        private bool myIsResponseListenerRegistered;
        private object myConnectionManipulatorLock = new object();
    }
}
