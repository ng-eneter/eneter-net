/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


#if !SILVERLIGHT && !MONO && !COMPACT_FRAMEWORK

using System;
using System.IO.Pipes;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeInputConnector : IInputConnector
    {
        public NamedPipeInputConnector(string serviceConnectorAddress, int timeOut, int numberOfListeningInstances, PipeSecurity security)
        {
            using (EneterTrace.Entering())
            {
                myServiceConnectorAddress = serviceConnectorAddress;
                myTimeout = timeOut;
                myNumberOfListeningInstances = numberOfListeningInstances;
                mySecurity = security;
            }
        }

        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    if (IsListening)
                    {
                        throw new InvalidOperationException(TracedObject + ErrorHandler.IsAlreadyListening);
                    }

                    try
                    {
                        myReceiver = new NamedPipeReceiver(myServiceConnectorAddress, myNumberOfListeningInstances, myTimeout, mySecurity);
                        myReceiver.StartListening(messageHandler);
                    }
                    catch
                    {
                        StopListening();
                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    if (myReceiver != null)
                    {
                        myReceiver.StopListening();
                        myReceiver = null;
                    }
                }
            }
        }

        public bool IsListening
        {
            get { return myReceiver != null && myReceiver.IsListening; }
        }

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            using (EneterTrace.Entering())
            {
                NamedPipeSender aResponseSender = new NamedPipeSender(responseReceiverAddress, myTimeout);
                return aResponseSender;
            }
        }


        private string myServiceConnectorAddress;
        private int myNumberOfListeningInstances;
        private int myTimeout;
        private PipeSecurity mySecurity;
        private NamedPipeReceiver myReceiver;

        private object myListeningManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif