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
        public SharedMemoryInputConnector(string inputMemoryMappedFileName, TimeSpan sendResponseTimeout, int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                myMaxMessageSize = maxMessageSize;
                mySendResponseTimeout = sendResponseTimeout;
                mySecurity = memoryMappedFileSecurity;

                // Note: openTimeout is not used if SharedMemoryReceiver creates memory mapped file.
                TimeSpan aDummyOpenTimout = TimeSpan.Zero;
                myReceiver = new SharedMemoryReceiver(inputMemoryMappedFileName, false, aDummyOpenTimout, myMaxMessageSize, mySecurity);
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

        // Note: It creates memory mapped file for each connected output connector (client).
        //       The memory mapped file is used to send response messages to the output connector.
        //       The output connector opens it as an existing memory mapped file.
        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            using (EneterTrace.Entering())
            {
                TimeSpan aDummyOpenTimeout = TimeSpan.Zero;
                return new SharedMemorySender(responseReceiverAddress, false, aDummyOpenTimeout, mySendResponseTimeout, myMaxMessageSize, mySecurity);
            }
        }


        private int myMaxMessageSize;
        private MemoryMappedFileSecurity mySecurity;

        private TimeSpan mySendResponseTimeout;

        private object myListenerManipulatorLock = new object();
        private SharedMemoryReceiver myReceiver;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif