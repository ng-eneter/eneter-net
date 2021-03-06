﻿


using System;
using System.Collections.Generic;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal class WebSocketInputConnector : IInputConnector
    {
        private class TClientContext
        {
            public TClientContext(IWebSocketClientContext client)
            {
                using (EneterTrace.Entering())
                {
                    myClient = client;
                }
            }

            public void CloseConnection()
            {
                using (EneterTrace.Entering())
                {
                    IsClosedFromService = true;
                    myClient.CloseConnection();
                }
            }

            public void SendResponseMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    myClient.SendMessage(message);
                }
            }

            public string ClientIp { get { return (myClient.ClientEndPoint != null) ? myClient.ClientEndPoint.ToString() : ""; } }

            public bool IsClosedFromService { get; private set; }
            public IWebSocketClientContext myClient;
        }

        public WebSocketInputConnector(string wsUriAddress, IProtocolFormatter protocolFormatter, ISecurityFactory securityFactory, int sendTimeout, int receiveTimeout,
            bool reuseAddressFlag,
            int maxAmountOfConnections)
        {
            using (EneterTrace.Entering())
            {
                Uri aUri;
                try
                {
                    aUri = new Uri(wsUriAddress);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(wsUriAddress + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                myProtocolFormatter = protocolFormatter;
                myListener = new WebSocketListener(aUri, securityFactory);
                myListener.ReuseAddress = reuseAddressFlag;
                myListener.MaxAmountOfClients = maxAmountOfConnections;

                mySendTimeout = sendTimeout;
                myReceiveTimeout = receiveTimeout;

                // Check if protocol encodes open and close messages.
                myProtocolUsesOpenConnectionMessage = myProtocolFormatter.EncodeOpenConnectionMessage("test") != null;
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
                        myListener.StartListening(HandleConnection);
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
                    foreach (KeyValuePair<string, TClientContext> aClient in myConnectedClients)
                    {
                        try
                        {
                            aClient.Value.CloseConnection();
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
                    myListener.StopListening();
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
                    return myListener.IsListening;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                TClientContext aClientContext;
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
                    aClientContext.SendResponseMessage(anEncodedMessage);
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
                    foreach (KeyValuePair<string, TClientContext> aClientContext in myConnectedClients)
                    {
                        try
                        {
                            object anEncodedMessage = myProtocolFormatter.EncodeMessage(aClientContext.Key, message);
                            aClientContext.Value.SendResponseMessage(anEncodedMessage);
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

        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                CloseConnection(outputConnectorAddress, false);
            }
        }

        private void HandleConnection(IWebSocketClientContext client)
        {
            using (EneterTrace.Entering())
            {
                string aClientIp = (client.ClientEndPoint != null) ? client.ClientEndPoint.ToString() : "";

                TClientContext aClientContext = new TClientContext(client);
                string aClientId = null;
                try
                {
                    client.SendTimeout = mySendTimeout;
                    client.ReceiveTimeout = myReceiveTimeout;

                    // If protocol formatter does not use OpenConnection message.
                    if (!myProtocolUsesOpenConnectionMessage)
                    {
                        aClientId = Guid.NewGuid().ToString();
                        using (ThreadLock.Lock(myConnectedClients))
                        {
                            myConnectedClients[aClientId] = aClientContext;
                        }

                        ProtocolMessage anOpenConnectionProtocolMessage = new ProtocolMessage(EProtocolMessageType.OpenConnectionRequest, aClientId, null);
                        MessageContext aMessageContext = new MessageContext(anOpenConnectionProtocolMessage, aClientIp);
                        NotifyMessageContext(aMessageContext);
                    }

                    while (true)
                    {
                        // Block until a message is received or the connection is closed.
                        WebSocketMessage aWebSocketMessage = client.ReceiveMessage();

                        if (aWebSocketMessage != null && myMessageHandler != null)
                        {
                            ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)aWebSocketMessage.InputStream);

                            // Note: security reasons ignore close connection message in WebSockets.
                            //       So that it is not possible that somebody will just send a close message which will have id of somebody else.
                            //       The connection will be closed when the client closes the socket.
                            if (aProtocolMessage != null && aProtocolMessage.MessageType != EProtocolMessageType.CloseConnectionRequest)
                            {
                                MessageContext aMessageContext = new MessageContext(aProtocolMessage, aClientIp);

                                // If protocol formatter uses open connection message to create the connection.
                                if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest &&
                                    myProtocolUsesOpenConnectionMessage)
                                {
                                    // Note: if client id is already set then it means this client has already open connection.
                                    if (string.IsNullOrEmpty(aClientId))
                                    {
                                        aClientId = !string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId) ? aProtocolMessage.ResponseReceiverId : Guid.NewGuid().ToString();

                                        using (ThreadLock.Lock(myConnectedClients))
                                        {
                                            if (!myConnectedClients.ContainsKey(aClientId))
                                            {
                                                myConnectedClients[aClientId] = aClientContext;
                                            }
                                            else
                                            {
                                                // Note: if the client id already exists then the connection cannot be open
                                                //       and the connection with this  client will be closed.
                                                EneterTrace.Warning(TracedObject + "could not open connection for client '" + aClientId + "' because the client with same id is already connected.");
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        EneterTrace.Warning(TracedObject + "the client '" + aClientId + "' has already open connection.");
                                    }
                                }

                                // Notify message.
                                // Ensure that nobody will try to use id of somebody else.
                                aMessageContext.ProtocolMessage.ResponseReceiverId = aClientId;
                                NotifyMessageContext(aMessageContext);
                            }
                            else if (aProtocolMessage == null)
                            {
                                // Client disconnected. Or the client shall be disconnected because of incorrect message format.
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    // Remove client from connected clients.
                    if (aClientId != null)
                    {
                        using (ThreadLock.Lock(myConnectedClients))
                        {
                            myConnectedClients.Remove(aClientId);
                        }
                    }

                    // If the disconnection does not come from the service
                    // and the client was successfuly connected then notify about the disconnection.
                    if (!aClientContext.IsClosedFromService && aClientId != null)
                    {
                        ProtocolMessage aCloseProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, aClientId, null);
                        MessageContext aMessageContext = new MessageContext(aCloseProtocolMessage, aClientIp);

                        // Notify duplex input channel about the disconnection.
                        NotifyMessageContext(aMessageContext);
                    }

                    client.CloseConnection();
                }
            }
        }

        private void CloseConnection(string outputConnectorAddress, bool notifyFlag)
        {
            using (EneterTrace.Entering())
            {
                TClientContext aClientContext;
                using (ThreadLock.Lock(myConnectedClients))
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientContext);
                }

                if (aClientContext != null)
                {
                    aClientContext.CloseConnection();
                }

                if (notifyFlag)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, outputConnectorAddress, null);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                    NotifyMessageContext(aMessageContext);
                }
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
        private bool myProtocolUsesOpenConnectionMessage;

        private WebSocketListener myListener;
        private Action<MessageContext> myMessageHandler;
        private object myListenerManipulatorLock = new object();
        private int mySendTimeout;
        private int myReceiveTimeout;
        private Dictionary<string, TClientContext> myConnectedClients = new Dictionary<string, TClientContext>();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}