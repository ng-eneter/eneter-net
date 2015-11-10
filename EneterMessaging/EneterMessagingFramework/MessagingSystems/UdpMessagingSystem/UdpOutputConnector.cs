/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

using System;
using System.Net;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpOutputConnector : IOutputConnector
    {
        public UdpOutputConnector(string ipAddressAndPort, string outpuConnectorAddress, IProtocolFormatter protocolFormatter,
            bool reuseAddressFlag, int responseReceivingPort, short ttl)
        {
            using (EneterTrace.Entering())
            {
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
                myOutputConnectorAddress = outpuConnectorAddress;
                myProtocolFormatter = protocolFormatter;
                myReuseAddressFlag = reuseAddressFlag;
                myResponseReceivingPort = responseReceivingPort;
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
                    try
                    {
                        myResponseMessageHandler = responseMessageHandler;
#if !WINDOWS_PHONE80 && !WINDOWS_PHONE81
                        myResponseReceiver = UdpReceiver.CreateConnectedReceiver(myServiceEndpoint, myReuseAddressFlag, myResponseReceivingPort, myTtl);
#else
                        myResponseReceiver = UdpReceiver.CreateConnectedReceiver(myServiceEndpoint, myTtl);
#endif
                        myResponseReceiver.StartListening(OnResponseMessageReceived);

                        byte[] anEncodedMessage = (byte[])myProtocolFormatter.EncodeOpenConnectionMessage(myOutputConnectorAddress);
                        if (anEncodedMessage != null)
                        {
                            myResponseReceiver.SendTo(anEncodedMessage, myServiceEndpoint);
                        }
                    }
                    catch
                    {
                        CloseConnection();
                        throw;
                    }
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                CleanConnection(true);
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
                    byte[] anEncodedMessage = (byte[])myProtocolFormatter.EncodeMessage(myOutputConnectorAddress, message);
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

                Action<MessageContext> aResponseHandler = myResponseMessageHandler;

                ProtocolMessage aProtocolMessage = null;
                if (datagram != null)
                {
                    aProtocolMessage = myProtocolFormatter.DecodeMessage(datagram);
                }
                else
                {
                    aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, myOutputConnectorAddress, null);
                }

                if (aProtocolMessage != null && aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    CleanConnection(false);
                }

                if (aResponseHandler != null)
                {
                    try
                    {
                        MessageContext aMessageContext = new MessageContext(aProtocolMessage, aSenderAddressStr);
                        aResponseHandler(aMessageContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void CleanConnection(bool sendMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    myResponseMessageHandler = null;

                    if (myResponseReceiver != null)
                    {
                        if (sendMessageFlag)
                        {
                            try
                            {
                                byte[] anEncodedMessage = (byte[])myProtocolFormatter.EncodeCloseConnectionMessage(myOutputConnectorAddress);
                                if (anEncodedMessage != null)
                                {
                                    myResponseReceiver.SendTo(anEncodedMessage, myServiceEndpoint);
                                }
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + "failed to send close connection message.", err);
                            }
                        }

                        myResponseReceiver.StopListening();
                        myResponseReceiver = null;
                    }
                }
            }
        }

        private IProtocolFormatter myProtocolFormatter;
        private string myOutputConnectorAddress;
        private IPEndPoint myServiceEndpoint;
        private bool myReuseAddressFlag;
        private short myTtl;
        private int myResponseReceivingPort;
        private Action<MessageContext> myResponseMessageHandler;
        private UdpReceiver myResponseReceiver;
        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif