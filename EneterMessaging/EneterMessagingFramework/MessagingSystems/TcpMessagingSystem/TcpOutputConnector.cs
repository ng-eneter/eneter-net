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
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;


namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpOutputConnector : IOutputConnector
    {
        public TcpOutputConnector(string ipAddressAndPort, ISecurityFactory clientSecurityFactory,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout,
            int sendBuffer,
            int receiveBuffer)
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

                myClientSecurityFactory = clientSecurityFactory;
                myConnectTimeout = (connectTimeout != 0) ? connectTimeout : -1;
                mySendTimeout = sendTimeout;
                myReceiveTimeout = receiveTimeout;
                mySendBuffer = sendBuffer;
                myReceiveBuffer = receiveBuffer;
            }
        }


        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myOpenConnectionLock)
                {
                    if (IsConnected)
                    {
                        throw new InvalidOperationException(TracedObject + ErrorHandler.IsAlreadyConnected);
                    }

                    try
                    {
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
									// This call also resolves the host name.
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
                        if (responseMessageHandler != null)
                        {
                            myStopReceivingRequestedFlag = false;

                            myResponseMessageHandler = responseMessageHandler;

                            myResponseReceiverThread = new Thread(DoResponseListening);
                            myResponseReceiverThread.Start();

                            // Wait until thread listening to response messages is running.
                            myListeningToResponsesStartedEvent.WaitOne(1000);
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
                lock (myOpenConnectionLock)
                {
                    myStopReceivingRequestedFlag = true;

                    if (myClientStream != null)
                    {
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
                if (myResponseMessageHandler != null)
                {
                    return myIsListeningToResponses;
                }

                return myClientStream != null;
            }
        }

        public bool IsStreamWritter { get { return false; } }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myOpenConnectionLock)
                {
                    byte[] aMessage = (byte[])message;
                    myClientStream.Write(aMessage, 0, aMessage.Length);
                }
            }
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException("toStreamWritter is not supported.");
        }


        private void DoResponseListening()
        {
            using (EneterTrace.Entering())
            {
                myIsListeningToResponses = true;
                myListeningToResponsesStartedEvent.Set();

                try
                {
                    MessageContext aContext = new MessageContext(myClientStream, myIpAddress, null);

                    while (!myStopReceivingRequestedFlag)
                    {
                        if (!myResponseMessageHandler(aContext))
                        {
                            // Handler requests stop receiving.
                            myStopReceivingRequestedFlag = true;
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

                // If this closing is not caused by CloseConnection method.
                if (!myStopReceivingRequestedFlag)
                {
                    // Try to clean the connection.
                    CloseConnection();
                }
            }
        }



        private Uri myUri;
        private TcpClient myTcpClient;
        private ISecurityFactory myClientSecurityFactory;
        private int myConnectTimeout;
        private int mySendTimeout;
        private int myReceiveTimeout;
        private int mySendBuffer;
        private int myReceiveBuffer;
        private Stream myClientStream;
        private string myIpAddress;
        private object myOpenConnectionLock = new object();

        private Func<MessageContext, bool> myResponseMessageHandler;
        private Thread myResponseReceiverThread;
        private volatile bool myStopReceivingRequestedFlag;
        private volatile bool myIsListeningToResponses;
        private ManualResetEvent myListeningToResponsesStartedEvent = new ManualResetEvent(false);

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif