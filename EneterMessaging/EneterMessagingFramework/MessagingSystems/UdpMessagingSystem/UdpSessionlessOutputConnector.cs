/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

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
            bool reuseAddressFlag, short ttl, bool allowBroadcast, string multicastGroup, bool multicastLoopbackFlag)
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
                int aClientPort = (aClientUri.Port < 0) ? 0 : aClientUri.Port;
                myClientEndPoint = new IPEndPoint(IPAddress.Parse(aClientUri.Host), aClientPort);
                myOutpuConnectorAddress = outpuConnectorAddress;
                myProtocolFormatter = protocolFormatter;
                myReuseAddressFlag = reuseAddressFlag;
                myTtl = ttl;
                myAllowBroadcastFlag = allowBroadcast;
                if (!string.IsNullOrEmpty(multicastGroup))
                {
                    myMulticastGroup = IPAddressExt.ParseMulticastGroup(multicastGroup);
                }
                myMulticastLoopbackFlag = multicastLoopbackFlag;
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
#if !WINDOWS_PHONE80 && !WINDOWS_PHONE81
                    myResponseReceiver = UdpReceiver.CreateBoundReceiver(myClientEndPoint, myReuseAddressFlag, myTtl, myAllowBroadcastFlag, myMulticastGroup, myMulticastLoopbackFlag);
#else
                    myResponseReceiver = UdpReceiver.CreateBoundReceiver(myClientEndPoint, myTtl, myMulticastGroup);
#endif
                    myResponseReceiver.StartListening(OnResponseMessageReceived);
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
                    myResponseReceiver.SendTo(anEncodedMessage, myServiceEndpoint);
                }
            }
        }

        private void OnResponseMessageReceived(byte[] datagram, EndPoint senderAddress)
        {
            using (EneterTrace.Entering())
            {
                if (datagram == null && senderAddress == null)
                {
                    // The listening got interrupted so nothing to do.
                    return;
                }

                // Get the sender IP address.
                string aSenderAddressStr = (senderAddress != null) ? ((IPEndPoint)senderAddress).ToString() : "";

                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(datagram);

                if (aProtocolMessage != null)
                {
                    if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        // Note: this output connector is used for multicast or broadcast communication.
                        //       Therefore the closing the connection by one of input channels has no sense in this context.
                        return;
                    }

                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, aSenderAddressStr);

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
        private IPAddress myMulticastGroup;
        private bool myReuseAddressFlag;
        private short myTtl;
        private bool myAllowBroadcastFlag;
        private bool myMulticastLoopbackFlag;
        private Action<MessageContext> myResponseMessageHandler;
        private UdpReceiver myResponseReceiver;
        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif