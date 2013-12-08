/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if NET4 || NET45

using System;
using System.IO.MemoryMappedFiles;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    internal class SharedMemoryInputConnector : IInputConnector
    {
        public SharedMemoryInputConnector(string inputMemoryMappedFileName, int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                myMaxMessageSize = maxMessageSize;
                mySecurity = memoryMappedFileSecurity;

                myReceiver = new SharedMemoryReceiver(inputMemoryMappedFileName, false, myMaxMessageSize, mySecurity);
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
                    myReceiver.StopListening();
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListenerManipulatorLock)
                {
                    return myReceiver.IsListening;
                }
            }
        }

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            using (EneterTrace.Entering())
            {
                return new SharedMemorySender(responseReceiverAddress, false, myMaxMessageSize, mySecurity);
            }
        }


        private int myMaxMessageSize;
        private MemoryMappedFileSecurity mySecurity;

        private object myListenerManipulatorLock = new object();
        private SharedMemoryReceiver myReceiver;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif