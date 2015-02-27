/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;


namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpOutputConnector : IOutputConnector
    {
        public TcpOutputConnector(string ipAddressAndPort, string outputConnectorAddress, IProtocolFormatter protocolFormatter, ISecurityFactory clientSecurityFactory, int connectTimeout, int sendTimeout, int receiveTimeout, int sendBuffer, int receiveBuffer)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myUri = new Uri(ipAddressAndPort, UriKind.Absolute);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(ipAddressAndPort + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                myOutputConnectorAddress = outputConnectorAddress;
                myClientSecurityFactory = clientSecurityFactory;
                myProtocolFormatter = protocolFormatter;
                myConnectTimeout = (connectTimeout != 0) ? connectTimeout : -1;
                mySendTimeout = sendTimeout;
                myReceiveTimeout = receiveTimeout;
                mySendBuffer = sendBuffer;
                myReceiveBuffer = receiveBuffer;
            }
        }

        public void OpenConnection(Action<MessageContext> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (responseMessageHandler == null)
                    {
                        throw new ArgumentNullException("responseMessageHandler is null.");
                    }

                    AddressFamily anAddressFamily = (myUri.HostNameType == UriHostNameType.IPv6) ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                    myTcpClient = new TcpClient(anAddressFamily);
                    myTcpClient.NoDelay = true;
#if !COMPACT_FRAMEWORK
                    myTcpClient.SendTimeout = mySendTimeout;
                    myTcpClient.ReceiveTimeout = myReceiveTimeout;
                    myTcpClient.SendBufferSize = mySendBuffer;
                    myTcpClient.ReceiveBufferSize = myReceiveBuffer;
#endif

                    // Note: TcpClient and Socket do not have a possibility to set the connection timeout.
                    //       Therefore it must be workerounded a little bit.
                    Exception anException = null;
                    ManualResetEvent aConnectionCompletedEvent = new ManualResetEvent(false);
                    ThreadPool.QueueUserWorkItem(x =>
                        {
                            try
                            {
#if !COMPACT_FRAMEWORK
								// This call also resolves host names.
								myTcpClient.Connect(myUri.Host, myUri.Port);
#else
                                // Compact framework has problems with resolving host names.
                                // Therefore directly IPAddress is used.
                                myTcpClient.Connect(IPAddress.Parse(myUri.Host), myUri.Port);
#endif
                            }
                            catch (Exception err)
                            {
                                anException = err;
                            }
                            aConnectionCompletedEvent.Set();
                        });
                    if (!aConnectionCompletedEvent.WaitOne(myConnectTimeout))
                    {
                        throw new TimeoutException(TracedObject + "failed to open connection within " + myConnectTimeout + " ms.");
                    }
                    if (anException != null)
                    {
                        throw anException;
                    }

                    myIpAddress = (myTcpClient.Client.LocalEndPoint != null) ? myTcpClient.Client.LocalEndPoint.ToString() : "";

                    myClientStream = myClientSecurityFactory.CreateSecurityStreamAndAuthenticate(myTcpClient.GetStream());

                    // If it shall listen to response messages.
                    myStopReceivingRequestedFlag = false;

                    myResponseMessageHandler = responseMessageHandler;

                    myResponseReceiverThread = new Thread(DoResponseListening);
                    myResponseReceiverThread.Start();

                    // Wait until thread listening to response messages is running.
                    myListeningToResponsesStartedEvent.WaitOne(1000);

                    byte[] anEncodedMessage = (byte[])myProtocolFormatter.EncodeOpenConnectionMessage(myOutputConnectorAddress);
                    myClientStream.Write(anEncodedMessage, 0, anEncodedMessage.Length);
                }
            }
        }

        public void CloseConnection(bool sendCloseMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myStopReceivingRequestedFlag = true;

                    if (myClientStream != null)
                    {
                        // Note: do not send a close message in TCP. Just close the socket.

                        myClientStream.Close();
                        myClientStream = null;
                    }

                    if (myTcpClient != null)
                    {
                        myTcpClient.Close();
                        myTcpClient = null;
                    }

                    if (myResponseReceiverThread != null && Thread.CurrentThread.ManagedThreadId != myResponseReceiverThread.ManagedThreadId)
                    {
#if COMPACT_FRAMEWORK
                        // N.A.
#else
                        if (myResponseReceiverThread.ThreadState != ThreadState.Unstarted)
#endif
                        {
                            if (!myResponseReceiverThread.Join(3000))
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myResponseReceiverThread.ManagedThreadId);

                                try
                                {
                                    myResponseReceiverThread.Abort();
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                                }
                            }
                        }
                        myResponseReceiverThread = null;
                    }

                    myResponseMessageHandler = null;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulatorLock)
                {
                    return myIsListeningToResponses;
                }
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    byte[] anEncodedMessage = (byte[])myProtocolFormatter.EncodeMessage(myOutputConnectorAddress, message);
                    myClientStream.Write(anEncodedMessage, 0, anEncodedMessage.Length);
                }
            }
        }

        private void DoResponseListening()
        {
            using (EneterTrace.Entering())
            {
                myIsListeningToResponses = true;
                myListeningToResponsesStartedEvent.Set();

                try
                {
                    while (!myStopReceivingRequestedFlag)
                    {
                        ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)myClientStream);
                        if (aProtocolMessage != null)
                        {
                            MessageContext aMessageContext = new MessageContext(aProtocolMessage, myIpAddress);
                            myResponseMessageHandler(aMessageContext);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception err)
                {
                    // If it is not an exception caused by closing the socket.
                    if (!myStopReceivingRequestedFlag)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DoListeningFailure, err);
                    }
                }

                myIsListeningToResponses = false;
                myListeningToResponsesStartedEvent.Reset();

                // If the connection was closed from service.
                if (!myStopReceivingRequestedFlag)
                {
                    // Notify
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, myOutputConnectorAddress, null);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, myIpAddress);
                    myResponseMessageHandler(aMessageContext);

                    // Try to clean the connection.
                    CloseConnection(false);
                }
            }
        }



        private Uri myUri;
        private string myOutputConnectorAddress;
        private TcpClient myTcpClient;
        private ISecurityFactory myClientSecurityFactory;
        private IProtocolFormatter myProtocolFormatter;
        private int myConnectTimeout;
        private int mySendTimeout;
        private int myReceiveTimeout;
        private int mySendBuffer;
        private int myReceiveBuffer;
        private Stream myClientStream;
        private string myIpAddress;
        private object myConnectionManipulatorLock = new object();

        private Action<MessageContext> myResponseMessageHandler;
        private Thread myResponseReceiverThread;
        private volatile bool myStopReceivingRequestedFlag;
        private volatile bool myIsListeningToResponses;
        private ManualResetEvent myListeningToResponsesStartedEvent = new ManualResetEvent(false);

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif