/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System;
using System.Net;
using System.Net.Sockets;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpOutputConnector : IOutputConnector
    {
        public UdpOutputConnector(string ipAddressAndPort, string outpuConnectorAddress, IProtocolFormatter protocolFormatter)
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
                myOutpuConnectorAddress = outpuConnectorAddress;
                myProtocolFormatter = protocolFormatter;
            }
        }

        public void OpenConnection(Action<MessageContext> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myResponseMessageHandler = responseMessageHandler;
                    myResponseReceiver = new UdpReceiver(myServiceEndpoint, false);
                    myResponseReceiver.StartListening(OnResponseMessageReceived);

                    // Get connected socket.
                    myClientSocket = myResponseReceiver.UdpSocket;

                    object anEncodedMessage = myProtocolFormatter.EncodeOpenConnectionMessage(myOutpuConnectorAddress);
                    SendMessage(anEncodedMessage);
                }
            }
        }

        public void CloseConnection(bool sendCloseMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (sendCloseMessageFlag)
                    {
                        try
                        {
                            object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(myOutpuConnectorAddress);
                            SendMessage(anEncodedMessage);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to send close connection message.", err);
                        }
                    }

                    if (myResponseReceiver != null)
                    {
                        myResponseReceiver.StopListening();
                        myResponseReceiver = null;
                    }

                    if (myClientSocket != null)
                    {
                        myClientSocket.Close();
                        myClientSocket = null;
                    }
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulatorLock)
                {
                    // If one-way communication.
                    if (myResponseReceiver == null)
                    {
                        return myClientSocket != null;
                    }

                    return myResponseReceiver.IsListening;
                }
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                object anEncodedMessage = myProtocolFormatter.EncodeMessage(myOutpuConnectorAddress, message);
                SendMessage(anEncodedMessage);
            }
        }

        private void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (!IsConnected)
                    {
                        throw new InvalidOperationException(TracedObject + ErrorHandler.SendMessageNotConnectedFailure);
                    }

                    byte[] aMessageData = (byte[])message;
                    myClientSocket.SendTo(aMessageData, myServiceEndpoint);
                }
            }
        }


        private void OnResponseMessageReceived(byte[] datagram, EndPoint dummy)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(datagram);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");
                myResponseMessageHandler(aMessageContext);
            }
        }

        private IProtocolFormatter myProtocolFormatter;
        private string myOutpuConnectorAddress;
        private IPEndPoint myServiceEndpoint;
        private Action<MessageContext> myResponseMessageHandler;
        private Socket myClientSocket;
        private UdpReceiver myResponseReceiver;
        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif