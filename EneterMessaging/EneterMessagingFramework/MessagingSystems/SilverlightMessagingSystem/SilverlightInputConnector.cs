/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightInputConnector : IInputConnector
    {
        public SilverlightInputConnector(string inputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(inputConnectorAddress))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                myInputConnectorAddress = inputConnectorAddress;
            }
        }

        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myListenerManipulatorLock)
                {
                    if (IsListening)
                    {
                        throw new InvalidOperationException(TracedObject + ErrorHandler.IsAlreadyListening);
                    }

                    try
                    {
                        myReceiver = new SilverlightReceiver(myInputConnectorAddress);
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
                lock (myListenerManipulatorLock)
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
            get
            {
                lock (myListenerManipulatorLock)
                {
                    return myReceiver != null && myReceiver.IsListening;
                }
            }
        }

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            using (EneterTrace.Entering())
            {
                return new SilverlightSender(responseReceiverAddress);
            }
        }


        private string myInputConnectorAddress;
        private object myListenerManipulatorLock = new object();
        private SilverlightReceiver myReceiver;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif