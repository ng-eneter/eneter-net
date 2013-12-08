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
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpReceiver
    {
        public UdpReceiver(IPEndPoint serviceEndPoint, bool isService)
        {
            using (EneterTrace.Entering())
            {
                myServiceEndpoint = serviceEndPoint;
                myIsService = isService;
                myWorkingThreadDispatcher = new SyncDispatcher();
            }
        }

        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    if (messageHandler == null)
                    {
                        throw new ArgumentNullException("The input parameter messageHandler is null.");
                    }

                    try
                    {
                        myStopListeningRequested = false;
                        myMessageHandler = messageHandler;

                        mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
#if !COMPACT_FRAMEWORK
                        // Note: bigger buffer increases the chance the datagram is not lost.
                        mySocket.ReceiveBufferSize = 1048576;
#else
                        mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1048576);
#endif
                        if (myIsService)
                        {
                            // Avoid getting exception when some UDP client disconnects.
                            // Note: http://stackoverflow.com/questions/10332630/connection-reset-on-receiving-packet-in-udp-server
                            const int SIO_UDP_CONNRESET = -1744830452;
                            byte[] inValue = new byte[] { 0 };
                            byte[] outValue = new byte[] { 0 };
                            mySocket.IOControl(SIO_UDP_CONNRESET, inValue, outValue);

                            mySocket.Bind(myServiceEndpoint);
                        }
                        else
                        {
                            mySocket.Connect(myServiceEndpoint);
                        }

                        myListeningThread = new Thread(DoListening);
                        myListeningThread.Start();

                        // Wait until the listening thread is ready.
                        myListeningToResponsesStartedEvent.WaitOne(5000);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);

                        try
                        {
                            // Clear after failed start
                            StopListening();
                        }
                        catch
                        {
                            // We tried to clean after failure. The exception can be ignored.
                        }

                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    // Stop the thread listening to messages from the shared memory.
                    myStopListeningRequested = true;

                    // Note: this receiver needs to close the socket here
                    //       because it will release the waiting in the listener thread.
                    if (mySocket != null)
                    {
                        mySocket.Close();
                        mySocket = null;
                    }

#if !COMPACT_FRAMEWORK
                    if (myListeningThread != null && myListeningThread.ThreadState != ThreadState.Unstarted)
#else
                    if (myListeningThread != null)
#endif               	
                    {
                        if (!myListeningThread.Join(3000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myListeningThread.ManagedThreadId.ToString());

                            try
                            {
                                myListeningThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                            }
                        }
                    }
                    myListeningThread = null;

                    myMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListeningManipulatorLock)
                {
                    return myIsListening;
                }
            }
        }

        public Socket UdpSocket { get { return mySocket; } }

        private void DoListening()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    byte[] aBuf = new byte[65536];
                    IPEndPoint aSenderEndPointTmp = new IPEndPoint(IPAddress.Any, 0);

                    myIsListening = true;
                    myListeningToResponsesStartedEvent.Set();

                    // Loop until the stop is requested.
                    while (!myStopListeningRequested)
                    {
                        // Wait for a message.
                        // IPEndPoint is then set to the sender of the message.
                        EndPoint aSenderEndPoint = (EndPoint)aSenderEndPointTmp;
                        int aReceivedLength = 0;
                        try
                        {
                            aReceivedLength = mySocket.ReceiveFrom(aBuf, ref aSenderEndPoint);
                        }
                        catch (SocketException err)
                        {
                            // If this is service then continue in the loop if the exception
                            // occured because one of clients got disconnected.
#if !COMPACT_FRAMEWORK
                            if (myIsService && err.SocketErrorCode == SocketError.Interrupted)
#else
                            if (myIsService && err.ErrorCode == 10004)
#endif
                            {
                                continue;
                            }

                            throw;
                        }

                        byte[] aDatagram;
                        if (aReceivedLength == aBuf.Length)
                        {
                            aDatagram = aBuf;
                            aBuf = new byte[aDatagram.Length];
                        }
                        else
                        {
                            aDatagram = new byte[aReceivedLength];
                            Buffer.BlockCopy(aBuf, 0, aDatagram, 0, aReceivedLength);
                        }

                        myWorkingThreadDispatcher.Invoke(() =>
                            {
                                try
                                {
                                    MessageContext aMessageContext;

                                    // If this is service then we need to create the sender for response messages.
                                    if (myIsService)
                                    {
                                        // Get the sender IP address.
                                        string aSenderIp = (aSenderEndPoint != null) ? ((IPEndPoint)aSenderEndPoint).Address.ToString() : "";

                                        // Create the response sender.
                                        UdpSender aResponseSender = new UdpSender(mySocket, aSenderEndPoint);

                                        aMessageContext = new MessageContext(aDatagram, aSenderIp, aResponseSender);
                                    }
                                    else
                                    {
                                        aMessageContext = new MessageContext(aDatagram, "", null);
                                    }

                                    myMessageHandler(aMessageContext);
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                }
                            });
                    }
                }
                catch (Exception err)
                {
                    // If the error did not occur because of StopListening().
                    if (!myStopListeningRequested)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DoListeningFailure, err);
                    }
                }

                // If the listening got interrupted.
                if (!myStopListeningRequested)
                {
                    myWorkingThreadDispatcher.Invoke(() =>
                        {
                            try
                            {
                                myMessageHandler(null);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        });
                }

                myIsListening = false;
                myListeningToResponsesStartedEvent.Reset();
            }
        }

        private bool myIsService;
        private EndPoint myServiceEndpoint;
        private Socket mySocket;
        private volatile bool myIsListening;
        private volatile bool myStopListeningRequested;
        private object myListeningManipulatorLock = new object();
        private Thread myListeningThread;
        private ManualResetEvent myListeningToResponsesStartedEvent = new ManualResetEvent(false);
        private Func<MessageContext, bool> myMessageHandler;
        private IDispatcher myWorkingThreadDispatcher;

        private string TracedObject
        {
            get
            {

                return (myIsService) ? "UdpReceiver (request receiver) " : "UdpReceiver (response receiver) ";
            }
        }
    }
}

#endif