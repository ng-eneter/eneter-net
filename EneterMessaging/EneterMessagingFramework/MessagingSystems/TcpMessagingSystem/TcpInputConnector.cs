/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using System.Collections.Generic;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpInputConnector : IInputConnector
    {
        private class TClientContext
        {
            public TClientContext(Stream clientStream)
            {
                using (EneterTrace.Entering())
                {
                    myClientStream = clientStream;
                }
            }

            public void CloseConnection()
            {
                using (EneterTrace.Entering())
                {
                    IsClosedFromService = true;

                    if (myClientStream != null)
                    {
                        myClientStream.Close();
                    }
                }
            }

            public void SendResponseMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    lock (mySenderLock)
                    {
                        byte[] aMessage = (byte[])message;
                        myClientStream.Write(aMessage, 0, aMessage.Length);
                    }
                }
            }

            public bool IsClosedFromService { get; private set; }

            private Stream myClientStream;
            private object mySenderLock = new object();
        }

        public TcpInputConnector(string ipAddressAndPort, IProtocolFormatter protocolFormatter, ISecurityFactory securityFactory,
            int sendTimeout,
            int receiveTimeout,
            int sendBuffer,
            int receiveBuffer)
        {
            using (EneterTrace.Entering())
            {
                using (EneterTrace.Entering())
                {
                    Uri aUri;
                    try
                    {
                        aUri = new Uri(ipAddressAndPort);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(ipAddressAndPort + ErrorHandler.InvalidUriAddress, err);
                        throw;
                    }

                    myTcpListenerProvider = new TcpListenerProvider(IPAddress.Parse(aUri.Host), aUri.Port);
                    myProtocolFormatter = protocolFormatter;
                    mySecurityStreamFactory = securityFactory;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                    mySendBuffer = sendBuffer;
                    myReceiveBuffer = receiveBuffer;

                    // Check if protocol encodes open and close messages.
                    myProtocolUsesOpenConnectionMessage = myProtocolFormatter.EncodeOpenConnectionMessage("test") != null;
                    myProtocolUsesCloseConnectionMessage = myProtocolFormatter.EncodeCloseConnectionMessage("test") != null;
                }
            }
        }


        public void StartListening(Action<MessageContext> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myMessageHandler = messageHandler;
                    myTcpListenerProvider.StartListening(HandleConnection);
                }
                catch
                {
                    myMessageHandler = null;
                    throw;
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                myTcpListenerProvider.StopListening();
                myMessageHandler = null;
            }
        }

        public bool IsListening { get { return myTcpListenerProvider.IsListening; } }

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

        private void HandleConnection(TcpClient tcpClient)
        {
            using (EneterTrace.Entering())
            {
                IPEndPoint anEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                string aClientIp = (anEndPoint != null) ? anEndPoint.Address.ToString() : "";

                Stream anInputOutputStream = null;

                TClientContext aClientContext = null;
                string aClientId = null;

                try
                {
#if !COMPACT_FRAMWORK
                    tcpClient.SendTimeout = mySendTimeout;
                    tcpClient.ReceiveTimeout = myReceiveTimeout;
                    tcpClient.SendBufferSize = mySendBuffer;
                    tcpClient.ReceiveBufferSize = myReceiveBuffer;
#endif

                    // If the security communication is required, then wrap the network stream into the security stream.
                    anInputOutputStream = mySecurityStreamFactory.CreateSecurityStreamAndAuthenticate(tcpClient.GetStream());

                    // If protocol formatter does not use OpenConnection message.
                    if (!myProtocolUsesOpenConnectionMessage)
                    {
                        aClientContext = new TClientContext(anInputOutputStream);

                        // Generate client id.
                        aClientId = Guid.NewGuid().ToString();
                        lock (myConnectedClients)
                        {
                            myConnectedClients[aClientId] = aClientContext;
                        }

                        ProtocolMessage anOpenConnectionProtocolMessage = new ProtocolMessage(EProtocolMessageType.OpenConnectionRequest, aClientId, null);
                        MessageContext aMessageContext = new MessageContext(anOpenConnectionProtocolMessage, aClientIp);
                        myMessageHandler(aMessageContext);
                    }

                    // While the stop of listening is not requested and the connection is not closed.
                    bool aConnectionIsOpen = true;
                    while (aConnectionIsOpen)
                    {
                        ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)anInputOutputStream);
                        if (aProtocolMessage != null)
                        {
                            MessageContext aMessageContext = new MessageContext(aProtocolMessage, aClientIp);

                            // If protocol formatter uses open connection message to create the connection.
                            if (myProtocolUsesOpenConnectionMessage && aClientContext == null)
                            {
                                if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                                {
                                    aClientId = !string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId) ? aProtocolMessage.ResponseReceiverId : Guid.NewGuid().ToString();
                                    aClientContext = new TClientContext(anInputOutputStream);

                                    lock (myConnectedClients)
                                    {
                                        myConnectedClients[aClientId] = aClientContext;
                                    }
                                }
                            }

                            // For security reasons ignore close connection message in TCP.
                            // Note: So that it is not possible that somebody will just send a close message which will have id of somebody else.
                            //       The TCP connection will be closed when the client closes the socket.
                            if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                            {
                            }
                            else
                            {
                                myMessageHandler(aMessageContext);
                            }
                        }
                        else
                        {
                            aConnectionIsOpen = false;
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
                    if (aClientContext != null && !aClientContext.IsClosedFromService)
                    {
                        ProtocolMessage aCloseProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, aClientId, null);
                        MessageContext aMessageContext = new MessageContext(aCloseProtocolMessage, aClientIp);

                        // Notify duplex input channel about the disconnection.
                        myMessageHandler(aMessageContext);
                    }

                    if (anInputOutputStream != null)
                    {
                        anInputOutputStream.Close();
                    }
                }
            }
        }


        private TcpListenerProvider myTcpListenerProvider;
        private ISecurityFactory mySecurityStreamFactory;

        private IProtocolFormatter myProtocolFormatter;
        private bool myProtocolUsesOpenConnectionMessage;
        private bool myProtocolUsesCloseConnectionMessage;

        private Action<MessageContext> myMessageHandler;
        private int mySendTimeout;
        private int myReceiveTimeout;
        private int mySendBuffer;
        private int myReceiveBuffer;

        private Dictionary<string, TClientContext> myConnectedClients = new Dictionary<string, TClientContext>();

        
    }
}

#endif