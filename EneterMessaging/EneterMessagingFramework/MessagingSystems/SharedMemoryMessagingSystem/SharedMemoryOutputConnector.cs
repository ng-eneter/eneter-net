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
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
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
            IProtocolFormatter protocolFormatter,
            TimeSpan connectTimeout,
            TimeSpan sendTimeout,
            int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                myOutputMemoryMappedFileName = outputMemoryMappedFileName;
                myInputMemoryMappedFileName = inputMemoryMappedFileName;
                myProtocolFormatter = protocolFormatter;
                myConnectTimeout = connectTimeout;
                mySendTimeout = sendTimeout;
                myMaxMessageSize = maxMessageSize;
                mySecurity = memoryMappedFileSecurity;
            }
        }


        public void OpenConnection(Action<MessageContext> responseMessageHandler)
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
                    if (responseMessageHandler == null)
                    {
                        throw new InvalidOperationException("responseMessageHandler is null.");
                    }

                    // If first 'OpenConnection' request message is sent and then the connection can be open.
                    // In case of Shared Memory messaging DuplexOutputChannel sends 'OpenConnecion' message
                    // to DuplexInputChannel. When DuplexInputChannel receives the message it creates
                    // shared memory for sending response messages to DuplexOutputChannel.
                    // Only then DuplexOutputChannel can open connection.
                    // Because only then the shared memory for response messages is created.
                    mySender = new SharedMemorySender(myOutputMemoryMappedFileName, true, myConnectTimeout, mySendTimeout, myMaxMessageSize, mySecurity);
                    SendMessage(x => myProtocolFormatter.EncodeOpenConnectionMessage(myInputMemoryMappedFileName, x));

                    myResponseMessageHandler = responseMessageHandler;
                    myReceiver = new SharedMemoryReceiver(myInputMemoryMappedFileName, true, myConnectTimeout, myMaxMessageSize, mySecurity);
                    myReceiver.StartListening(HandleResponseMessage);
                }
            }
        }

        public void CloseConnection(bool sendCloseMessageFlag)
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
                        if (sendCloseMessageFlag)
                        {
                            try
                            {
                                SendMessage(x => myProtocolFormatter.EncodeCloseConnectionMessage(myInputMemoryMappedFileName, x));
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + "failed to send close connection message.", err);
                            }
                        }

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
                    return myReceiver != null && myReceiver.IsListening;
                }
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    SendMessage(x => myProtocolFormatter.EncodeMessage(myInputMemoryMappedFileName, message, x));
                }
            }
        }

        private void SendMessage(Action<Stream> toStreamWritter)
        {
            using (EneterTrace.Entering())
            {
                mySender.SendMessage(toStreamWritter);
            }
        }

        private void HandleResponseMessage(Stream message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)message);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");
                myResponseMessageHandler(aMessageContext);
            }
        }

        private string myOutputMemoryMappedFileName;
        private string myInputMemoryMappedFileName;
        private IProtocolFormatter myProtocolFormatter;
        private int myMaxMessageSize;
        private MemoryMappedFileSecurity mySecurity;

        private object myConnectionManipulatorLock = new object();

        private TimeSpan myConnectTimeout;
        private TimeSpan mySendTimeout;

        private SharedMemorySender mySender;     // Sending request messages.
        private SharedMemoryReceiver myReceiver; // Receiving response messages.

        private Action<MessageContext> myResponseMessageHandler;
        

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif