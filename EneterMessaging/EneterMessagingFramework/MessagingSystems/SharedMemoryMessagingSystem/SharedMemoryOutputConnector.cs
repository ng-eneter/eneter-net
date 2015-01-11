/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if NET4 || NET45

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    /// <summary>
    /// Sends request messages and receives resposne messages via shared memory.
    /// </summary>
    /// <remarks>
    /// The communication via the shared memory is realized using two memory mapped files:
    /// • memory mapped file for sending request messages
    /// • memory mapped file for receiving response messages
    /// Both memory mapped files (and coresponding synchronization wait handlers) are created by SharedMemoryInputConnector.
    /// Therefore SharedMemoryOutputConnector just opens existing memory mapped files. 
    /// </remarks>
    internal class SharedMemoryOutputConnector : IOutputConnector
    {
        public SharedMemoryOutputConnector(string outputMemoryMappedFileName, string inputMemoryMappedFileName,
            TimeSpan connectTimeout,
            TimeSpan sendTimeout,
            int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                myOutputMemoryMappedFileName = outputMemoryMappedFileName;
                myInputMemoryMappedFileName = inputMemoryMappedFileName;
                myConnectTimeout = connectTimeout;
                mySendTimeout = sendTimeout;
                myMaxMessageSize = maxMessageSize;
                mySecurity = memoryMappedFileSecurity;
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

                    // If it shall listen to responses then check the responseMessageHandler.
                    if (!String.IsNullOrEmpty(myInputMemoryMappedFileName))
                    {
                        if (responseMessageHandler == null)
                        {
                            throw new InvalidOperationException("responseMessageHandler is null.");
                        }
                    }

                    try
                    {
                        if (mySender == null)
                        {
                            mySender = new SharedMemorySender(myOutputMemoryMappedFileName, true, myConnectTimeout, mySendTimeout, myMaxMessageSize, mySecurity);
                        }

                        // If it shall listen to response messages.
                        if (responseMessageHandler != null)
                        {
                            myReceiver = new SharedMemoryReceiver(myInputMemoryMappedFileName, true, myConnectTimeout, myMaxMessageSize, mySecurity);
                            myReceiver.StartListening(responseMessageHandler);
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
                    if (myReceiver != null)
                    {
                        myReceiver.StopListening();
                        myReceiver = null;
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
                lock (myConnectionManipulatorLock)
                {
                    // If responses are expected then listener is supposed to run in order to say the connection is open.
                    if (!String.IsNullOrEmpty(myInputMemoryMappedFileName))
                    {
                        return myReceiver != null && myReceiver.IsListening;
                    }

                    return mySender != null;
                }
            }
        }

        public bool IsStreamWritter { get { return true; } }

        public void SendMessage(object message)
        {
            throw new NotSupportedException("Sending of byte[] is not supported.");
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    // Note: DuplexOutputChannel needs first to send open request so that shared memory input channel
                    //       can create the shared memory for response messages. And only then the connection can be open.
                    //       So it should be possible to send a message before OpenConnection is called.
                    if (mySender == null)
                    {
                        mySender = new SharedMemorySender(myOutputMemoryMappedFileName, true, myConnectTimeout, mySendTimeout, myMaxMessageSize, mySecurity);
                    }

                    mySender.SendMessage(toStreamWritter);
                }
            }
        }


        private string myOutputMemoryMappedFileName;
        private string myInputMemoryMappedFileName;
        private int myMaxMessageSize;
        private MemoryMappedFileSecurity mySecurity;

        private object myConnectionManipulatorLock = new object();

        private TimeSpan myConnectTimeout;
        private TimeSpan mySendTimeout;

        private SharedMemorySender mySender;     // Sending request messages.
        private SharedMemoryReceiver myReceiver; // Receiving response messages.
        

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif