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
    internal class UdpSessionlessOutputConnector : IOutputConnector
    {
        public UdpSessionlessOutputConnector(string ipAddressAndPort, string outpuConnectorAddress, IProtocolFormatter protocolFormatter,
            bool reuseAddressFlag, short ttl)
        {
            using (EneterTrace.Entering())
            {
                Uri anServiceUri;
                try
                {
                    anServiceUri = new Uri(ipAddressAndPort, UriKind.Absolute);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(ipAddressAndPort + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                Uri aClientUri;
                try
                {
                    aClientUri = new Uri(outpuConnectorAddress, UriKind.Absolute);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(outpuConnectorAddress + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                myServiceEndpoint = new IPEndPoint(IPAddress.Parse(anServiceUri.Host), anServiceUri.Port);
                myClientEndPoint = new IPEndPoint(IPAddress.Parse(aClientUri.Host), aClientUri.Port);
                myOutpuConnectorAddress = outpuConnectorAddress;
                myProtocolFormatter = protocolFormatter;
                myReuseAddressFlag = reuseAddressFlag;
                myTtl = ttl;
            }
        }

        public void OpenConnection(Action<MessageContext> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                if (responseMessageHandler == null)
                {
                    throw new ArgumentNullException("responseMessageHandler is null.");
                }

                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    myResponseMessageHandler = responseMessageHandler;

                    // Listen on the client address.
                    myResponseReceiver = UdpReceiver.CreateBoundReceiver(myClientEndPoint, myReuseAddressFlag, true, myTtl);
                    myResponseReceiver.StartListening(OnRequestMessageReceived);
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    myResponseMessageHandler = null;

                    myResponseReceiver.StopListening();
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    return myResponseReceiver != null && myResponseReceiver.IsListening;
                }
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    byte[] anEncodedMessage = (byte[])myProtocolFormatter.EncodeMessage(myOutpuConnectorAddress, message);
                    myResponseReceiver.UdpSocket.SendTo(anEncodedMessage, myServiceEndpoint);
                }
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
                        Action<MessageContext> aMessageHandler = myResponseMessageHandler;
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
        private string myOutpuConnectorAddress;
        private IPEndPoint myClientEndPoint;
        private IPEndPoint myServiceEndpoint;
        private bool myReuseAddressFlag;
        private short myTtl;
        private Action<MessageContext> myResponseMessageHandler;
        private UdpReceiver myResponseReceiver;
        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif