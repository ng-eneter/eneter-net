﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT && !WINDOWS_PHONE_70

using System;
using System.Collections.Generic;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

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

            public bool IsClosedFromService { get; private set; }
            public IWebSocketClientContext myClient;
        }

        public WebSocketInputConnector(string wsUriAddress, IProtocolFormatter protocolFormatter, ISecurityFactory securityFactory, int sendTimeout, int receiveTimeout)
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
                lock (myListenerManipulatorLock)
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
                lock (myListenerManipulatorLock)
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
                lock (myListenerManipulatorLock)
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
                lock (myConnectedClients)
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientContext);
                }

                if (aClientContext != null)
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                    aClientContext.SendResponseMessage(anEncodedMessage);
                }
            }
        }

        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                TClientContext aClientContext;
                lock (myConnectedClients)
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientContext);
                }

                if (aClientContext != null)
                {
                    aClientContext.CloseConnection();
                }
            }
        }

        private void HandleConnection(IWebSocketClientContext client)
        {
            using (EneterTrace.Entering())
            {
                string aClientIp = (client.ClientEndPoint != null) ? client.ClientEndPoint.Address.ToString() : "";

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
                        lock (myConnectedClients)
                        {
                            myConnectedClients[aClientId] = aClientContext;
                        }

                        ProtocolMessage anOpenConnectionProtocolMessage = new ProtocolMessage(EProtocolMessageType.OpenConnectionRequest, aClientId, null);
                        MessageContext aMessageContext = new MessageContext(anOpenConnectionProtocolMessage, aClientIp);
                        myMessageHandler(aMessageContext);
                    }

                    bool isConnectionOpen = true;
                    while (isConnectionOpen)
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
                                if (myProtocolUsesOpenConnectionMessage)
                                {
                                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                                    {
                                        aClientId = !string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId) ? aProtocolMessage.ResponseReceiverId : Guid.NewGuid().ToString();

                                        lock (myConnectedClients)
                                        {
                                            myConnectedClients[aClientId] = aClientContext;
                                        }
                                    }
                                }

                                // Notify message.
                                myMessageHandler(aMessageContext);
                            }
                        }
                        else
                        {
                            isConnectionOpen = false;
                        }
                    }
                }
                finally
                {
                    // Remove client from connected clients.
                    if (aClientId != null)
                    {
                        lock (myConnectedClients)
                        {
                            myConnectedClients.Remove(aClientId);
                        }
                    }

                    // If the disconnection comes from the client (and not from the service).
                    if (!aClientContext.IsClosedFromService)
                    {
                        ProtocolMessage aCloseProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, aClientId, null);
                        MessageContext aMessageContext = new MessageContext(aCloseProtocolMessage, aClientIp);

                        // Notify duplex input channel about the disconnection.
                        myMessageHandler(aMessageContext);
                    }

                    client.CloseConnection();
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
    }
}


#endif