/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

#if WINDOWS_PHONE80 || WINDOWS_PHONE81

using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Diagnostic;
using System;
using System.Net;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpReceiver
    {
        private class UdpSocketWrapper
        {
            public event TypedEventHandler<DatagramSocket, DatagramSocketMessageReceivedEventArgs> MessageReceived;

            public UdpSocketWrapper()
            {
                using (EneterTrace.Entering())
                {
                    mySocket = new DatagramSocket();
                    mySocket.MessageReceived += OnDatagramReceived;
                }
            }

            public void Bind(IPEndPoint endPoint)
            {
                using (EneterTrace.Entering())
                {
                    HostName aHostName = new HostName(endPoint.Address.ToString());
                    string aPort = endPoint.Port.ToString();
                    mySocket.BindEndpointAsync(aHostName, aPort).AsTask().Wait();
                }
            }

            public void Connect(IPEndPoint endPoint)
            {
                using (EneterTrace.Entering())
                {
                    HostName aHostName = new HostName(endPoint.Address.ToString());
                    string aPort = endPoint.Port.ToString();
                    mySocket.ConnectAsync(aHostName, aPort).AsTask().Wait();
                }
            }

            public void Close()
            {
                using (EneterTrace.Entering())
                {
                    mySocket.Dispose();
                    mySocket.MessageReceived += OnDatagramReceived;
                }
            }

            public void SendTo(byte[] message, IPEndPoint endPoint)
            {
                using (EneterTrace.Entering())
                {
                    IOutputStream anOutputStream = mySocket.OutputStream;
                    using (DataWriter aWriter = new DataWriter(anOutputStream))
                    {
                        aWriter.WriteBytes(message);
                        aWriter.StoreAsync().AsTask().Wait();
                    }
                }
            }

            public void JoinMulticast(IPAddress multicastGroup)
            {
                using (EneterTrace.Entering())
                {
                    if (multicastGroup != null)
                    {
                        HostName aMulticastGroup = new HostName(multicastGroup.ToString());
                        mySocket.JoinMulticastGroup(aMulticastGroup);
                    }
                }
            }

            public short Ttl
            {
                get { return mySocket.Control.OutboundUnicastHopLimit; }
                set { mySocket.Control.OutboundUnicastHopLimit = (byte)value; }
            }

            private void OnDatagramReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
            {
                if (MessageReceived != null)
                {
                    MessageReceived(sender, args);
                }
            }

            private DatagramSocket mySocket;
        }


        public static UdpReceiver CreateBoundReceiver(IPEndPoint endPointToBind, short ttl, IPAddress multicastGroup)
        {
            using (EneterTrace.Entering())
            {
                return new UdpReceiver(null, endPointToBind, ttl, multicastGroup);
            }
        }

        public static UdpReceiver CreateConnectedReceiver(IPEndPoint endPointToConnect, short ttl)
        {
            using (EneterTrace.Entering())
            {
                return new UdpReceiver(endPointToConnect, null, ttl, null);
            }
        }


        private UdpReceiver(IPEndPoint endPointToConnect, IPEndPoint endPointToBind, short ttl, IPAddress multicastGroup)
        {
            using (EneterTrace.Entering())
            {
                myEndPointToConnect = endPointToConnect;
                myEndPointToBind = endPointToBind;
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
                        myMessageHandler = messageHandler;

                        mySocketWrapper = new UdpSocketWrapper();
                        mySocketWrapper.MessageReceived += OnDatagramReceived;

                        // Note: TTL can be set only for unicast in Windows Phone.
                        mySocketWrapper.Ttl = myTtl;

                        if (myEndPointToBind != null || myEndPointToConnect != null)
                        {
                            if (myEndPointToBind != null)
                            {
                                mySocketWrapper.Bind(myEndPointToBind);

                                // Note:  Joining the multicast group must be done after Bind.
                                mySocketWrapper.JoinMulticast(myMulticastGroup);
                            }
                            else
                            {
                                mySocketWrapper.Connect(myEndPointToConnect);
                            }
                        }
                        else
                        // This is in case of one-way sender.
                        {
                            myIsListening = true;
                        }
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
                    if (mySocketWrapper != null)
                    {
                        mySocketWrapper.Close();
                        mySocketWrapper.MessageReceived -= OnDatagramReceived;
                        mySocketWrapper = null;
                    }

                    myMessageHandler = null;
                    myIsListening = false;
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

        public void SendTo(byte[] message, IPEndPoint endPoint)
        {
            //using (EneterTrace.Entering())
            {
                mySocketWrapper.SendTo(message, endPoint);
            }
        }

        private void OnDatagramReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                IPAddress aRemoteIpAddress = IPAddress.Parse(e.RemoteAddress.RawName);
                int aPort = int.Parse(e.RemotePort);
                IPEndPoint aSenderEndPoint = new IPEndPoint(aRemoteIpAddress, aPort);

                uint aDataSize = e.GetDataReader().UnconsumedBufferLength;
                byte[] aDatagram = new byte[aDataSize];
                e.GetDataReader().ReadBytes(aDatagram);

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

        private IPEndPoint myEndPointToBind;
        private IPEndPoint myEndPointToConnect;
        private IPAddress myMulticastGroup;
        private UdpSocketWrapper mySocketWrapper;
        private short myTtl;

        private volatile bool myIsListening;
        private object myListeningManipulatorLock = new object();
        private Action<byte[], EndPoint> myMessageHandler;
        private SingleThreadExecutor myWorkingThreadDispatcher;

        private string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }
    }
}

#endif
