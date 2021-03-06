﻿

#if !NET35 && NETFRAMEWORK

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
        public SharedMemoryOutputConnector(string inputConnectorAddress, string outputConnectorAddress,
            IProtocolFormatter protocolFormatter,
            TimeSpan connectTimeout,
            TimeSpan sendTimeout,
            int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myOutputConnectorAddress = outputConnectorAddress;
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
                // If it shall listen to responses then check the responseMessageHandler.
                if (responseMessageHandler == null)
                {
                    throw new InvalidOperationException("responseMessageHandler is null.");
                }

                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    try
                    {
                        // Send open connection message to open the connection.
                        // When DuplexInputChannel receives the open connection message it creates
                        // shared memory for sending response messages.
                        mySender = new SharedMemorySender(myInputConnectorAddress, true, myConnectTimeout, mySendTimeout, myMaxMessageSize, mySecurity);
                        mySender.SendMessage(x => myProtocolFormatter.EncodeOpenConnectionMessage(myOutputConnectorAddress, x));

                        // The input connector has opened the shared memory for responses
                        // so we can start listening from it.
                        // (It will open existing memory mapped file.)
                        myResponseMessageHandler = responseMessageHandler;
                        myReceiver = new SharedMemoryReceiver(myOutputConnectorAddress, true, myConnectTimeout, myMaxMessageSize, mySecurity);
                        myReceiver.StartListening(HandleResponseMessage);
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
                CleanConnection(true);
            }
        }

        public bool IsConnected
        {
            get
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
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
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    mySender.SendMessage(x => myProtocolFormatter.EncodeMessage(myOutputConnectorAddress, message, x));
                }
            }
        }

        private void HandleResponseMessage(Stream message)
        {
            using (EneterTrace.Entering())
            {
                Action<MessageContext> aResponseHandler = myResponseMessageHandler;

                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)message);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                if (aProtocolMessage != null && aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    CleanConnection(false);
                }

                try
                {
                    aResponseHandler(aMessageContext);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        private void CleanConnection(bool sendMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    if (myReceiver != null)
                    {
                        myReceiver.StopListening();
                        myReceiver = null;
                    }

                    if (mySender != null)
                    {
                        if (sendMessageFlag)
                        {
                            try
                            {
                                mySender.SendMessage(x => myProtocolFormatter.EncodeCloseConnectionMessage(myOutputConnectorAddress, x));
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

        private string myInputConnectorAddress;
        private string myOutputConnectorAddress;
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