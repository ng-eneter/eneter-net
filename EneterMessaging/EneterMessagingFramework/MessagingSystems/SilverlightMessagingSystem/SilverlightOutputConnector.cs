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
    internal class SilverlightOutputConnector : IOutputConnector
    {
        public SilverlightOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myOutputConnectorAddress = outputConnectorAddress;
            }
        }

        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (IsConnected)
                    {
                        throw new InvalidOperationException(TracedObject + ErrorHandler.IsAlreadyConnected);
                    }

                    try
                    {
                        mySender = new SilverlightSender(myInputConnectorAddress);

                        // If it shall receive response messages.
                        if (responseMessageHandler != null)
                        {
                            myResponseReceiver = new SilverlightReceiver(myOutputConnectorAddress);
                            myResponseReceiver.StartListening(responseMessageHandler);
                        }
                    }
                    catch
                    {
                        CloseConnection();
                        throw;
                    }
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (myResponseReceiver != null)
                    {
                        myResponseReceiver.StopListening();
                        myResponseReceiver = null;
                    }

                    mySender = null;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulatorLock)
                {
                    // If one-way communication.
                    if (myResponseReceiver == null)
                    {
                        return mySender != null;
                    }

                    return myResponseReceiver.IsListening;
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
                    mySender.SendMessage(message);
                }
            }
        }

        public void SendMessage(Action<System.IO.Stream> toStreamWritter)
        {
            throw new NotSupportedException(TracedObject + "does not suport toStreamWritter.");
        }


        private object myConnectionManipulatorLock = new object();
        private string myInputConnectorAddress;
        private string myOutputConnectorAddress;
        private ISender mySender;
        private SilverlightReceiver myResponseReceiver;


        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif