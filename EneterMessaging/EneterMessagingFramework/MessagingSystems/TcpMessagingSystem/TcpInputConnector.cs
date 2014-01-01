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

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpInputConnector : IInputConnector
    {
        private class ResponseSender : ISender, IDisposable
        {
            public ResponseSender(Stream clientStream)
            {
                using (EneterTrace.Entering())
                {
                    myClientStream = clientStream;
                }
            }

            public void Dispose()
            {
                using (EneterTrace.Entering())
                {
                    if (myClientStream != null)
                    {
                        myClientStream.Close();
                    }
                }
            }

            public bool IsStreamWritter { get { return false; } }

            public void SendMessage(object message)
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

            public void SendMessage(Action<Stream> toStreamWritter)
            {
                throw new NotSupportedException("Sending via the stream is not supported.");
            }

            private Stream myClientStream;
            private object mySenderLock = new object();
        }

        public TcpInputConnector(string ipAddressAndPort, ISecurityFactory securityFactory,
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
                    mySecurityStreamFactory = securityFactory;
                    mySendTimeout = sendTimeout;
                    myReceiveTimeout = receiveTimeout;
                    mySendBuffer = sendBuffer;
                    myReceiveBuffer = receiveBuffer;
                }
            }
        }


        public void StartListening(Func<MessageContext, bool> messageHandler)
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

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            throw new NotSupportedException("CreateResponseSender is not supported in TcpServiceConnector.");
        }


        private void HandleConnection(TcpClient tcpClient)
        {
            using (EneterTrace.Entering())
            {
                IPEndPoint anEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                string aClientIp = (anEndPoint != null) ? anEndPoint.Address.ToString() : "";

                Stream anInputOutputStream = null;

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

                    ResponseSender aResponseSender = new ResponseSender(anInputOutputStream);
                    MessageContext aMessageContext = new MessageContext(anInputOutputStream, aClientIp, aResponseSender);

                    // While the stop of listening is not requested and the connection is not closed.
                    bool aConnectionIsOpen = true;
                    while (aConnectionIsOpen)
                    {
                        aConnectionIsOpen = myMessageHandler(aMessageContext);
                    }

                }
                finally
                {
                    if (anInputOutputStream != null)
                    {
                        anInputOutputStream.Close();
                    }
                }
            }
        }


        private TcpListenerProvider myTcpListenerProvider;
        private ISecurityFactory mySecurityStreamFactory;
        private Func<MessageContext, bool> myMessageHandler;
        private int mySendTimeout;
        private int myReceiveTimeout;
        private int mySendBuffer;
        private int myReceiveBuffer;

    }
}

#endif