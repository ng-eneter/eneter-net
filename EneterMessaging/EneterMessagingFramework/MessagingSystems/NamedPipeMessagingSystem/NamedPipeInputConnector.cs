


#if !NETSTANDARD

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeInputConnector : IInputConnector
    {
        public NamedPipeInputConnector(string inputConnectorAddress, IProtocolFormatter protocolFormatter, int connectionTimeout, int numberOfListeningInstances, PipeSecurity security)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myProtocolFormatter = protocolFormatter;
                myConnectionTimeout = connectionTimeout;
                myNumberOfListeningInstances = numberOfListeningInstances;
                mySecurity = security;
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

                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    try
                    {
                        myMessageHandler = messageHandler;
                        myReceiver = new NamedPipeReceiver(myInputConnectorAddress, myNumberOfListeningInstances, myConnectionTimeout, mySecurity);
                        myReceiver.StartListening(OnRequestMessageReceived);
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
                    foreach (KeyValuePair<string, NamedPipeSender> aClient in myConnectedClients)
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

                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    if (myReceiver != null)
                    {
                        myReceiver.StopListening();
                        myReceiver = null;
                    }

                    myMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    return myReceiver != null && myReceiver.IsListening;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                NamedPipeSender aClientContext;
                using (ThreadLock.Lock(myConnectedClients))
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientContext);
                }

                if (aClientContext == null)
                {
                    throw new InvalidOperationException("The connection with client '" + outputConnectorAddress + "' is not open.");
                }

                try
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                    aClientContext.SendMessage(anEncodedMessage);
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
                    foreach (KeyValuePair<string, NamedPipeSender> aClientContext in myConnectedClients)
                    {
                        try
                        {
                            object anEncodedMessage = myProtocolFormatter.EncodeMessage(aClientContext.Key, message);
                            aClientContext.Value.SendMessage(anEncodedMessage);
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


        private void OnRequestMessageReceived(Stream message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)message);
                if (aProtocolMessage != null)
                {
                    MessageContext aMessageContext = null;

                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            using (ThreadLock.Lock(myConnectedClients))
                            {
                                if (!myConnectedClients.ContainsKey(aProtocolMessage.ResponseReceiverId))
                                {
                                    NamedPipeSender aClientContext = new NamedPipeSender(aProtocolMessage.ResponseReceiverId, myConnectionTimeout);
                                    myConnectedClients[aProtocolMessage.ResponseReceiverId] = aClientContext;
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
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            using (ThreadLock.Lock(myConnectedClients))
                            {
                                NamedPipeSender aClientContext;
                                using (ThreadLock.Lock(myConnectedClients))
                                {
                                    myConnectedClients.TryGetValue(aProtocolMessage.ResponseReceiverId, out aClientContext);
                                    if (aClientContext != null)
                                    {
                                        myConnectedClients.Remove(aProtocolMessage.ResponseReceiverId);
                                    }
                                }

                                if (aClientContext != null)
                                {
                                    aClientContext.Dispose();
                                }
                            }
                        }
                    }

                    aMessageContext = new MessageContext(aProtocolMessage, "");
                    NotifyMessageContext(aMessageContext);
                }
            }
        }

        private void CloseConnection(string outputConnectorAddress, bool notifyFlag)
        {
            using (EneterTrace.Entering())
            {
                NamedPipeSender aClientContext;
                using (ThreadLock.Lock(myConnectedClients))
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientContext);
                    if (aClientContext != null)
                    {
                        myConnectedClients.Remove(outputConnectorAddress);
                    }
                }

                if (aClientContext != null)
                {
                    CloseConnection(outputConnectorAddress, aClientContext);
                }

                if (notifyFlag)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, outputConnectorAddress, null);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                    NotifyMessageContext(aMessageContext);
                }
            }
        }

        private void CloseConnection(string outputConnectorAddress, NamedPipeSender clientContext)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress);
                    clientContext.SendMessage(anEncodedMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning("failed to send the close message.", err);
                }

                clientContext.Dispose();
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
        private string myInputConnectorAddress;
        private int myNumberOfListeningInstances;
        private int myConnectionTimeout;
        private PipeSecurity mySecurity;
        private NamedPipeReceiver myReceiver;
        private Action<MessageContext> myMessageHandler;

        private object myListeningManipulatorLock = new object();
        private Dictionary<string, NamedPipeSender> myConnectedClients = new Dictionary<string, NamedPipeSender>();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif