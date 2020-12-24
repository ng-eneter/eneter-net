

using System;
using System.Collections.Generic;
using System.Net;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpInputConnector : IInputConnector
    {
        private class TClientContext
        {
            public TClientContext(UdpReceiver udpReceiver, IPEndPoint clientEndPoint)
            {
                using (EneterTrace.Entering())
                {
                    myUdpReceiver = udpReceiver;
                    myClientEndPoint = clientEndPoint;
                }
            }

            public void CloseConnection()
            {
                using (EneterTrace.Entering())
                {
                    // Note: we do not close the udp socket because it is used globaly for all connected clients.
                }
            }

            public void SendResponseMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    byte[] aMessageData = (byte[])message;
                    myUdpReceiver.SendTo(aMessageData, myClientEndPoint);
                }
            }

            private UdpReceiver myUdpReceiver;
            private IPEndPoint myClientEndPoint;
        }

        public UdpInputConnector(string ipAddressAndPort, IProtocolFormatter protocolFormatter, bool reuseAddress, short ttl,
            int maxAmountOfConnections)
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
                myReuseAddressFlag = reuseAddress;
                myTtl = ttl;
                myMaxAmountOfConnections = maxAmountOfConnections;
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
                        myReceiver = UdpReceiver.CreateBoundReceiver(myServiceEndpoint, myReuseAddressFlag, myTtl, false, null, false);
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
                    foreach (KeyValuePair<string, TClientContext> aClient in myConnectedClients)
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
                using (ThreadLock.Lock(myListenerManipulatorLock))
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

        private void OnRequestMessageReceived(byte[] datagram, EndPoint clientAddress)
        {
            using (EneterTrace.Entering())
            {
                if (datagram == null && clientAddress == null)
                {
                    // The listening got interrupted so nothing to do.
                    return;
                }

                // Get the sender IP address.
                string aClientIp = (clientAddress != null) ? ((IPEndPoint)clientAddress).ToString() : "";

                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(datagram);

                if (aProtocolMessage != null)
                {
                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            using (ThreadLock.Lock(myConnectedClients))
                            {
                                if (myMaxAmountOfConnections > -1 && myConnectedClients.Count >= myMaxAmountOfConnections)
                                {
                                    TClientContext aClientContext = new TClientContext(myReceiver, (IPEndPoint)clientAddress);
                                    CloseConnection(aProtocolMessage.ResponseReceiverId, aClientContext);

                                    EneterTrace.Warning(TracedObject + "could not open connection for client '" + aProtocolMessage.ResponseReceiverId + "' because the maximum number of connections = '" + myMaxAmountOfConnections + "' was exceeded.");
                                    return;
                                }

                                if (!myConnectedClients.ContainsKey(aProtocolMessage.ResponseReceiverId))
                                {
                                    TClientContext aClientContext = new TClientContext(myReceiver, (IPEndPoint)clientAddress);
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
                                myConnectedClients.Remove(aProtocolMessage.ResponseReceiverId);
                            }
                        }
                    }

                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, aClientIp);
                    NotifyMessageContext(aMessageContext);
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
                    myConnectedClients.Remove(outputConnectorAddress);
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

        private void CloseConnection(string outputConnectorAddress, TClientContext clientContext)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress);
                    if (anEncodedMessage != null)
                    {
                        clientContext.SendResponseMessage(anEncodedMessage);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                }

                clientContext.CloseConnection();
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
        private IPEndPoint myServiceEndpoint;
        private bool myReuseAddressFlag;
        private short myTtl;
        private int myMaxAmountOfConnections;
        private UdpReceiver myReceiver;
        private object myListenerManipulatorLock = new object();
        private Action<MessageContext> myMessageHandler;
        private Dictionary<string, TClientContext> myConnectedClients = new Dictionary<string, TClientContext>();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}