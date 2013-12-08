/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT && !MONO && !COMPACT_FRAMEWORK

using System;
using System.IO;
using System.IO.Pipes;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeOutputConnector : IOutputConnector
    {
        public NamedPipeOutputConnector(string serviceConnectorAddress, string clientConnectorAddress, int timeOut, int numberOfListeningInstances, PipeSecurity security)
        {
            using (EneterTrace.Entering())
            {
                myServiceConnectorAddress = serviceConnectorAddress;
                myClientConnectorAddress = clientConnectorAddress;
                myTimeout = timeOut;
                myNumberOfListeningInstances = numberOfListeningInstances;
                mySecurity = security;
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
                        mySender = new NamedPipeSender(myServiceConnectorAddress, myTimeout);

                        // If it shall receive response messages.
                        if (responseMessageHandler != null)
                        {
                            myResponseReceiver = new NamedPipeReceiver(myClientConnectorAddress, myNumberOfListeningInstances, myTimeout, mySecurity);
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

                    if (mySender != null)
                    {
                        mySender.Dispose();
                        mySender = null;
                    }
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                // If one-way communication.
                if (myResponseReceiver == null)
                {
                    return mySender != null;
                }

                return myResponseReceiver.IsListening;
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

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException("NamedPipeClientConnector does not suport toStreamWritter.");
        }


        private string myServiceConnectorAddress;
        private string myClientConnectorAddress;
        private int myNumberOfListeningInstances;
        private int myTimeout;
        private PipeSecurity mySecurity;
        private NamedPipeSender mySender;
        private NamedPipeReceiver myResponseReceiver;

        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}


#endif