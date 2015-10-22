/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using System;
using System.Net;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpSessionlessInputConnector : IInputConnector
    {
        public UdpSessionlessInputConnector(string ipAddressAndPort, IProtocolFormatter protocolFormatter,
            bool reuseAddressFlag, short ttl, string multicastGroup)
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

                int aPort = (anServiceUri.Port < 0) ? 0 : anServiceUri.Port;
                myServiceEndpoint = new IPEndPoint(IPAddress.Parse(anServiceUri.Host), aPort);
                myProtocolFormatter = protocolFormatter;
                myReuseAddressFlag = reuseAddressFlag;
                myTtl = ttl;
                if (!string.IsNullOrEmpty(multicastGroup))
                {
                    myMulticastGroup = IPAddressExt.ParseMulticastGroup(multicastGroup);
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

                using (ThreadLock.Lock(myListenerManipulatorLock))
                {
                    try
                    {
                        myMessageHandler = messageHandler;
                        myReceiver = UdpReceiver.CreateBoundReceiver(myServiceEndpoint, myReuseAddressFlag, true, myTtl, myMulticastGroup);
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
                Uri aUri = new Uri(outputConnectorAddress);
                IPAddress anIpAddress = IPAddress.Parse(aUri.Host);
                IPEndPoint anEndpoint = new IPEndPoint(anIpAddress, aUri.Port);
                byte[] anEncodedMessage = (byte[]) myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                myReceiver.UdpSocket.SendTo(anEncodedMessage, anEndpoint);
            }
        }

        public void SendBroadcast(object message)
        {
            using (EneterTrace.Entering())
            {
                throw new NotSupportedException("Instead use SendResponseMessage(\"udp://255.255.255.255:xxxx\", message);");
            }
        }

        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                // n.a. becaue this is the connectionless input connector
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
                    aProtocolMessage.ResponseReceiverId = "udp://" + aClientIp + "/";
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, aClientIp);

                    try
                    {
                        Action<MessageContext> aMessageHandler = myMessageHandler;
                        if (aMessageHandler != null)
                        {
                            aMessageHandler(aMessageContext);
                        }
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
        private IPAddress myMulticastGroup;
        private bool myReuseAddressFlag;
        private short myTtl;
        private UdpReceiver myReceiver;
        private object myListenerManipulatorLock = new object();
        private Action<MessageContext> myMessageHandler;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif