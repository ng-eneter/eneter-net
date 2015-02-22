/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if NET4 || NET45

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    internal class SharedMemoryInputConnector : IInputConnector
    {
        public SharedMemoryInputConnector(string inputMemoryMappedFileName, IProtocolFormatter protocolFormatter, TimeSpan sendResponseTimeout, int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
                myMaxMessageSize = maxMessageSize;
                mySendResponseTimeout = sendResponseTimeout;
                mySecurity = memoryMappedFileSecurity;

                // Note: openTimeout is not used if SharedMemoryReceiver creates memory mapped file.
                TimeSpan aDummyOpenTimout = TimeSpan.Zero;
                myReceiver = new SharedMemoryReceiver(inputMemoryMappedFileName, false, aDummyOpenTimout, myMaxMessageSize, mySecurity);
            }
        }

        public void StartListening(Action<MessageContext> messageHandler)
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
                        myRequestMessageHandler = messageHandler;
                        myReceiver.StartListening(HandleRequestMessage);
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
                    myRequestMessageHandler = null;
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

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectedClients)
                {
                    SharedMemorySender aClientSender;
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientSender);

                    if (aClientSender != null)
                    {
                        aClientSender.SendMessage(x => myProtocolFormatter.EncodeMessage(outputConnectorAddress, message, x));
                    }
                }
            }
        }

        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectedClients)
                {
                    SharedMemorySender aClientSender;
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientSender);

                    if (aClientSender != null)
                    {
                        try
                        {
                            aClientSender.SendMessage(x => myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress, x));
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning("failed to send the close message.", err);
                        }

                        myConnectedClients.Remove(outputConnectorAddress);
                        aClientSender.Dispose();
                    }
                }
            }
        }

        private void HandleRequestMessage(Stream message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(message);

                if (aProtocolMessage != null)
                {
                    if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                    {
                        // If it is open connection then add the new connected client.
                        if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                        {
                            lock (myConnectedClients)
                            {
                                if (!myConnectedClients.ContainsKey(aProtocolMessage.ResponseReceiverId))
                                {
                                    TimeSpan aDummyOpenTimeout = TimeSpan.Zero;
                                    SharedMemorySender aClientSender = new SharedMemorySender(aProtocolMessage.ResponseReceiverId, false, aDummyOpenTimeout, mySendResponseTimeout, myMaxMessageSize, mySecurity);

                                    myConnectedClients[aProtocolMessage.ResponseReceiverId] = aClientSender;
                                }
                            }
                        }
                        else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                        {
                            lock (myConnectedClients)
                            {
                                SharedMemorySender aClientSender;
                                myConnectedClients.TryGetValue(aProtocolMessage.ResponseReceiverId, out aClientSender);

                                if (aClientSender != null)
                                {
                                    myConnectedClients.Remove(aProtocolMessage.ResponseReceiverId);

                                    aClientSender.Dispose();
                                }
                            }
                        }

                        MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");
                        myRequestMessageHandler(aMessageContext);
                    }
                }
            }
        }


        private IProtocolFormatter myProtocolFormatter;
        private int myMaxMessageSize;
        private MemoryMappedFileSecurity mySecurity;

        private TimeSpan mySendResponseTimeout;

        private object myListenerManipulatorLock = new object();
        private SharedMemoryReceiver myReceiver;

        private Action<MessageContext> myRequestMessageHandler;

        private Dictionary<string, SharedMemorySender> myConnectedClients = new Dictionary<string, SharedMemorySender>();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif