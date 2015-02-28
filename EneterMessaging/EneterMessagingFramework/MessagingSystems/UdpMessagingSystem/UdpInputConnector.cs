/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System;
using System.Net;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using System.Collections.Generic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using System.Net.Sockets;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpInputConnector : IInputConnector
    {
        private class TClientContext
        {
            public TClientContext(Socket udpSocket, EndPoint clientAddress)
            {
                using (EneterTrace.Entering())
                {
                    myUdpSocket = udpSocket;
                    myClientAddress = clientAddress;
                }
            }

            public void CloseConnection()
            {
                using (EneterTrace.Entering())
                {
                    IsClosedFromService = true;

                    // Note: we do not close the udp socket because it is used globaly for all connected clients.
                }
            }

            public void SendResponseMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    byte[] aMessageData = (byte[])message;
                    myUdpSocket.SendTo(aMessageData, myClientAddress);
                }
            }
            public bool IsClosedFromService { get; private set; }

            private Socket myUdpSocket;
            private EndPoint myClientAddress;
        }

        public UdpInputConnector(string ipAddressAndPort, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(ipAddressAndPort))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                Uri anServiceUri;
                try
                {
                    // just check if the address is valid
                    anServiceUri = new Uri(ipAddressAndPort, UriKind.Absolute);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(ipAddressAndPort + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                myServiceEndpoint = new IPEndPoint(IPAddress.Parse(anServiceUri.Host), anServiceUri.Port);
                myProtocolFormatter = protocolFormatter;
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

                lock (myListenerManipulatorLock)
                {
                    try
                    {
                        myMessageHandler = messageHandler;
                        myReceiver = new UdpReceiver(myServiceEndpoint, true);
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
                lock (myListenerManipulatorLock)
                {
                    if (myReceiver != null)
                    {
                        myReceiver.StopListening();
                    }
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListenerManipulatorLock)
                {
                    return myReceiver != null && myReceiver.IsListening;
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
                    myConnectedClients.Remove(outputConnectorAddress);
                }

                if (aClientContext != null)
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress);
                    aClientContext.SendResponseMessage(anEncodedMessage);
                    aClientContext.CloseConnection();
                }
                else
                {
                    throw new InvalidOperationException("Could not send close connection message because the response receiver '" + outputConnectorAddress + "' was not found.");
                }
            }
        }

        private void OnRequestMessageReceived(byte[] datagram, EndPoint clientAddress)
        {
            using (EneterTrace.Entering())
            {
                // Get the sender IP address.
                string aClientIp = (clientAddress != null) ? ((IPEndPoint)clientAddress).Address.ToString() : "";

                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(datagram);

                if (aProtocolMessage != null)
                {
                    MessageContext aMessageContext = null;

                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            lock (myConnectedClients)
                            {
                                if (!myConnectedClients.ContainsKey(aProtocolMessage.ResponseReceiverId))
                                {
                                    TClientContext aClientContext = new TClientContext(myReceiver.UdpSocket, clientAddress);
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
                            lock (myConnectedClients)
                            {
                                myConnectedClients.Remove(aProtocolMessage.ResponseReceiverId);
                            }
                        }
                    }

                    try
                    {
                        aMessageContext = new MessageContext(aProtocolMessage, aClientIp);
                        myMessageHandler(aMessageContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private IProtocolFormatter myProtocolFormatter;
        private IPEndPoint myServiceEndpoint;
        private UdpReceiver myReceiver;
        private object myListenerManipulatorLock = new object();
        private Action<MessageContext> myMessageHandler;
        private Dictionary<string, TClientContext> myConnectedClients = new Dictionary<string, TClientContext>();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif