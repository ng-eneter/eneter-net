/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

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
                    IsClosedByService = true;
                    myClientStream.Close();
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

            public bool IsClosedByService { get; private set; }

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
                }
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

                lock (myListeningManipulatorLock)
                {
                    try
                    {
                        myMessageHandler = messageHandler;
                        myTcpListenerProvider.StartListening(HandleConnection);
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
                lock (myListeningManipulatorLock)
                {
                    myTcpListenerProvider.StopListening();
                    myMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListeningManipulatorLock)
                {
                    return myTcpListenerProvider.IsListening;
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

                if (aClientContext == null)
                {
                    throw new InvalidOperationException("The connection with client '" + outputConnectorAddress + "' is not open.");
                }

                object anEncodedMessage = myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                aClientContext.SendResponseMessage(anEncodedMessage);
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
                    aClientContext = new TClientContext(anInputOutputStream);

                    // If current protocol formatter does not support OpenConnection message
                    // then open the connection now.
                    if (!myProtocolUsesOpenConnectionMessage)
                    {
                        // Generate client id.
                        aClientId = Guid.NewGuid().ToString();
                        lock (myConnectedClients)
                        {
                            myConnectedClients[aClientId] = aClientContext;
                        }

                        ProtocolMessage anOpenConnectionProtocolMessage = new ProtocolMessage(EProtocolMessageType.OpenConnectionRequest, aClientId, null);
                        MessageContext aMessageContext = new MessageContext(anOpenConnectionProtocolMessage, aClientIp);

                        try
                        {
                            myMessageHandler(aMessageContext);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }

                    // While the stop of listening is not requested and the connection is not closed.
                    while (true)
                    {
                        ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)anInputOutputStream);

                        // If the stream was not closed.
                        if (aProtocolMessage != null)
                        {
                            // Note: Due to security reasons ignore close connection message in TCP.
                            //       So that it is not possible that somebody will just send a close message which will have id of somebody else.
                            //       The TCP connection will be closed when the client closes the socket.
                            if (aProtocolMessage.MessageType != EProtocolMessageType.CloseConnectionRequest)
                            {
                                MessageContext aMessageContext = new MessageContext(aProtocolMessage, aClientIp);

                                // If open connection message is received and the current protocol formatter uses open connection message
                                // then create the connection now.
                                if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest &&
                                    myProtocolUsesOpenConnectionMessage)
                                {
                                    // Note: if client id is already set then it means this client has already open connection.
                                    if (string.IsNullOrEmpty(aClientId))
                                    {
                                        aClientId = !string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId) ? aProtocolMessage.ResponseReceiverId : Guid.NewGuid().ToString();

                                        lock (myConnectedClients)
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

                                try
                                {
                                    // Ensure that nobody will try to use id of somebody else.
                                    aMessageContext.ProtocolMessage.ResponseReceiverId = aClientId;

                                    // Notify message.
                                    myMessageHandler(aMessageContext);
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                }
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
                        lock (myConnectedClients)
                        {
                            myConnectedClients.Remove(aClientId);
                        }
                    }

                    // If the disconnection comes from the client (and not from the service).
                    if (aClientContext != null && !aClientContext.IsClosedByService)
                    {
                        ProtocolMessage aCloseProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, aClientId, null);
                        MessageContext aMessageContext = new MessageContext(aCloseProtocolMessage, aClientIp);

                        // Notify duplex input channel about the disconnection.
                        try
                        {
                            myMessageHandler(aMessageContext);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
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

        private Action<MessageContext> myMessageHandler;
        private int mySendTimeout;
        private int myReceiveTimeout;
        private int mySendBuffer;
        private int myReceiveBuffer;

        private object myListeningManipulatorLock = new object();

        private Dictionary<string, TClientContext> myConnectedClients = new Dictionary<string, TClientContext>();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif