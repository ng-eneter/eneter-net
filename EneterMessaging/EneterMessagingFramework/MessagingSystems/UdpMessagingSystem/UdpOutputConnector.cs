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

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpOutputConnector : IOutputConnector
    {
        public UdpOutputConnector(string ipAddressAndPort)
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
            }
        }

        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (responseMessageHandler != null)
                    {
                        myResponseReceiver = new UdpReceiver(myServiceEndpoint, false);
                        myResponseReceiver.StartListening(responseMessageHandler);

                        // Get connected socket.
                        myClientSocket = myResponseReceiver.UdpSocket;
                    }
                    else
                    {
                        myClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        myClientSocket.Connect(myServiceEndpoint);
                    }
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
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

        public bool IsStreamWritter { get { return false; } }

        public void SendMessage(object message)
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

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException("toStreamWritter is not supported.");
        }

        private IPEndPoint myServiceEndpoint;
        private Socket myClientSocket;
        private UdpReceiver myResponseReceiver;
        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif