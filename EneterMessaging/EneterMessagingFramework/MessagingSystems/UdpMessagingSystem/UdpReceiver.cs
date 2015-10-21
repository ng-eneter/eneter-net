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
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpReceiver
    {
        public static UdpReceiver CreateBoundReceiver(IPEndPoint serviceEndPoint, bool reuseAddressFlag, bool allowBroadcast,
            short ttl, IPAddress multicastGroup)
        {
            using (EneterTrace.Entering())
            {
                return new UdpReceiver(serviceEndPoint, reuseAddressFlag, allowBroadcast, ttl, multicastGroup);
            }
        }

        public static UdpReceiver CreateConnectedReceiver(IPEndPoint serviceEndPoint, bool reuseAddressFlag, int responseReceivingPort,
            short ttl, IPAddress multicastGroup)
        {
            using (EneterTrace.Entering())
            {
                return new UdpReceiver(serviceEndPoint, reuseAddressFlag, responseReceivingPort, ttl, multicastGroup);
            }
        }

        // Constructor binding to EndPoint.
        private UdpReceiver(IPEndPoint serviceEndPoint, bool reuseAddressFlag, bool allowBroadcast, short ttl,
            IPAddress multicastGroup)
        {
            using (EneterTrace.Entering())
            {
                myServiceEndpoint = serviceEndPoint;
                myIsBound = true;
                myReuseAddressFlag = reuseAddressFlag;
                myAllowBroadcastFlag = allowBroadcast;
                myTtl = ttl;
                myMulticastGroup = multicastGroup;

                myWorkingThreadDispatcher = new SingleThreadExecutor();
            }
        }

        // Constructor connecting the EndPoint.
        private UdpReceiver(IPEndPoint serviceEndPoint, bool reuseAddressFlag, int responseReceivingPort, short ttl,
            IPAddress multicastGroup)
        {
            using (EneterTrace.Entering())
            {
                myServiceEndpoint = serviceEndPoint;
                myIsBound = false;
                myReuseAddressFlag = reuseAddressFlag;
                myResponseReceivingPort = responseReceivingPort;
                myTtl = ttl;
                myMulticastGroup = multicastGroup;

                myWorkingThreadDispatcher = new SingleThreadExecutor();
            }
        }

        public void StartListening(Action<byte[], EndPoint> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
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

                        mySocket = new Socket(myServiceEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
#if !COMPACT_FRAMEWORK
                        // Note: bigger buffer increases the chance the datagram is not lost.
                        mySocket.ReceiveBufferSize = 1048576;
#else
                        mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1048576);
#endif
                        mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, myReuseAddressFlag);
                        mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, myAllowBroadcastFlag);
                        mySocket.Ttl = myTtl;

                        if (myIsBound)
                        {
                            // Avoid getting exception when some UDP client disconnects.
                            // Note: http://stackoverflow.com/questions/10332630/connection-reset-on-receiving-packet-in-udp-server
                            const int SIO_UDP_CONNRESET = -1744830452;
                            byte[] inValue = new byte[] { 0 };
                            byte[] outValue = new byte[] { 0 };
                            mySocket.IOControl(SIO_UDP_CONNRESET, inValue, outValue);

                            mySocket.Bind(myServiceEndpoint);

                            // Note:  Joining the multicast group must be done after Bind.
                            // Note: There is no need to drop the multicast group before closing the socket.
                            //       When the socket is closed the multicast groups are dropped automatically.
                            JoinMulticastGroup();
                        }
                        else
                        {
                            // If the client shall bind incoming responses to a specified port.
                            if (myResponseReceivingPort > 0)
                            {
                                // Note: IPAddress.Any will be updated once the connection is established.
                                mySocket.Bind(new IPEndPoint(IPAddress.Any, myResponseReceivingPort));
                            }

                            JoinMulticastGroup();

                            mySocket.Connect(myServiceEndpoint);
                        }

                        myListeningThread = new Thread(DoListening);
                        myListeningThread.Start();

                        // Wait until the listening thread is ready.
                        myListeningToResponsesStartedEvent.WaitOne(5000);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);

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
                using (ThreadLock.Lock(myListeningManipulatorLock))
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
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToStopThreadId + myListeningThread.ManagedThreadId.ToString());

                            try
                            {
                                myListeningThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.FailedToAbortThread, err);
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
                using (ThreadLock.Lock(myListeningManipulatorLock))
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
                            if (myIsBound && err.SocketErrorCode == SocketError.Interrupted)
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

                        myWorkingThreadDispatcher.Execute(() =>
                            {
                                try
                                {
                                    myMessageHandler(aDatagram, aSenderEndPoint);
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
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedInListeningLoop, err);
                    }
                }

                // If the listening got interrupted.
                if (!myStopListeningRequested)
                {
                    myWorkingThreadDispatcher.Execute(() =>
                        {
                            try
                            {
                                myMessageHandler(null, null);
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

        private void JoinMulticastGroup()
        {
            using (EneterTrace.Entering())
            {
                if (myMulticastGroup != null)
                {
                    if (myMulticastGroup.AddressFamily == AddressFamily.InterNetwork)
                    {
                        MulticastOption aMulticastOption = new MulticastOption(myMulticastGroup);
                        mySocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, aMulticastOption);
                    }
                    else if (myMulticastGroup.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        IPv6MulticastOption aMulticastOption = new IPv6MulticastOption(myMulticastGroup);
                        mySocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, aMulticastOption);
                    }
                }
            }
        }

        private bool myIsBound;
        private EndPoint myServiceEndpoint;
        private IPAddress myMulticastGroup;
        private Socket mySocket;
        private bool myReuseAddressFlag;
        private bool myAllowBroadcastFlag;
        private int myResponseReceivingPort;
        private short myTtl;

        private volatile bool myIsListening;
        private volatile bool myStopListeningRequested;
        private object myListeningManipulatorLock = new object();
        private Thread myListeningThread;
        private ManualResetEvent myListeningToResponsesStartedEvent = new ManualResetEvent(false);
        private Action<byte[], EndPoint> myMessageHandler;
        private SingleThreadExecutor myWorkingThreadDispatcher;

        private string TracedObject
        {
            get
            {

                return (myIsBound) ? "UdpReceiver (request receiver) " : "UdpReceiver (response receiver) ";
            }
        }
    }
}

#endif