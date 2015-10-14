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
        public SharedMemoryInputConnector(string inputConnectorAddress, IProtocolFormatter protocolFormatter, TimeSpan sendResponseTimeout, int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
                myMaxMessageSize = maxMessageSize;
                mySendResponseTimeout = sendResponseTimeout;
                mySecurity = memoryMappedFileSecurity;

                // Note: openTimeout is not used if SharedMemoryReceiver creates memory mapped file.
                TimeSpan aDummyOpenTimout = TimeSpan.Zero;
                myReceiver = new SharedMemoryReceiver(inputConnectorAddress, false, aDummyOpenTimout, myMaxMessageSize, mySecurity);
            }
        }

        public void StartListening(Action<MessageContext> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                if (messageHandler == null)
                {
                    throw new ArgumentNullException("messageHandler is null.");
                }

                using (ThreadLock.Lock(myListenerManipulatorLock))
                {
                    try
                    {
                        myMessageHandler = messageHandler;
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
                using (ThreadLock.Lock(myConnectedClients))
                {
                    foreach (KeyValuePair<string, SharedMemorySender> aClient in myConnectedClients)
                    {
                        try
                        {
                            CloseConnection(aClient.Key, aClient.Value);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                        }
                    }

                    myConnectedClients.Clear();
                }

                using (ThreadLock.Lock(myListenerManipulatorLock))
                {
                    myReceiver.StopListening();
                    myMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (ThreadLock.Lock(myListenerManipulatorLock))
                {
                    return myReceiver.IsListening;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                SharedMemorySender aClientSender;
                using (ThreadLock.Lock(myConnectedClients))
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientSender);
                }

                if (aClientSender == null)
                {
                    throw new InvalidOperationException("The connection with client '" + outputConnectorAddress + "' is not open.");
                }

                try
                {
                    aClientSender.SendMessage(x => myProtocolFormatter.EncodeMessage(outputConnectorAddress, message, x));
                }
                catch
                {
                    CloseConnection(outputConnectorAddress, true);
                    throw;
                }
            }
        }

        public void SendBroadcast(object message)
        {
            using (EneterTrace.Entering())
            {
                List<string> aDisconnectedClients = new List<string>();

                using (ThreadLock.Lock(myConnectedClients))
                {
                    // Send the response message to all connected clients.
                    foreach (KeyValuePair<string, SharedMemorySender> aClientContext in myConnectedClients)
                    {
                        try
                        {
                            aClientContext.Value.SendMessage(x => myProtocolFormatter.EncodeMessage(aClientContext.Key, message, x));
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                            aDisconnectedClients.Add(aClientContext.Key);

                            // Note: Exception is not rethrown because if sending to one client fails it should not
                            //       affect sending to other clients.
                        }
                    }
                }

                // Disconnect failed clients.
                foreach (String anOutputConnectorAddress in aDisconnectedClients)
                {
                    CloseConnection(anOutputConnectorAddress, true);
                }
            }
        }

        // When service disconnects a client.
        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                CloseConnection(outputConnectorAddress, false);
            }
        }

        private void HandleRequestMessage(Stream message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(message);

                if (aProtocolMessage != null)
                {
                    // If it is open connection then add the new connected client.
                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            using (ThreadLock.Lock(myConnectedClients))
                            {
                                if (!myConnectedClients.ContainsKey(aProtocolMessage.ResponseReceiverId))
                                {
                                    TimeSpan aDummyOpenTimeout = TimeSpan.Zero;
                                    SharedMemorySender aClientSender = new SharedMemorySender(aProtocolMessage.ResponseReceiverId, false, aDummyOpenTimeout, mySendResponseTimeout, myMaxMessageSize, mySecurity);

                                    myConnectedClients[aProtocolMessage.ResponseReceiverId] = aClientSender;
                                }
                                else
                                {
                                    EneterTrace.Warning(TracedObject + "could not open connection for client '" + aProtocolMessage.ResponseReceiverId + "' because the client with same id is already connected.");
                                }
                            }
                        }
                        else
                        {
                            EneterTrace.Warning(TracedObject + "could not connect a client because response recevier id was not available in open connection message.");
                        }
                    }
                    // When a client closes connection with the service.
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            using (ThreadLock.Lock(myConnectedClients))
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
                    }

                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");
                    NotifyMessageContext(aMessageContext);
                }
            }
        }

        private void CloseConnection(string outputConnectorAddress, bool notifyFlag)
        {
            using (EneterTrace.Entering())
            {
                SharedMemorySender aClientSender;
                using (ThreadLock.Lock(myConnectedClients))
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientSender);
                    if (aClientSender != null)
                    {
                        myConnectedClients.Remove(outputConnectorAddress);
                    }
                }

                if (aClientSender != null)
                {
                    CloseConnection(outputConnectorAddress, aClientSender);
                }

                if (notifyFlag)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, outputConnectorAddress, null);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                    NotifyMessageContext(aMessageContext);
                }
            }
        }

        private void CloseConnection(string outputConnectorAddress, SharedMemorySender clientSender)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    clientSender.SendMessage(x => myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress, x));
                }
                catch (Exception err)
                {
                    EneterTrace.Warning("failed to send the close message.", err);
                }

                clientSender.Dispose();
            }
        }

        private void NotifyMessageContext(MessageContext messageContext)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    Action<MessageContext> aMessageHandler = myMessageHandler;
                    if (aMessageHandler != null)
                    {
                        aMessageHandler(messageContext);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }


        private IProtocolFormatter myProtocolFormatter;
        private int myMaxMessageSize;
        private MemoryMappedFileSecurity mySecurity;

        private TimeSpan mySendResponseTimeout;

        private object myListenerManipulatorLock = new object();
        private SharedMemoryReceiver myReceiver;

        private Action<MessageContext> myMessageHandler;

        private Dictionary<string, SharedMemorySender> myConnectedClients = new Dictionary<string, SharedMemorySender>();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif