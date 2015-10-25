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
using System.Threading.Tasks;
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

                    if (!endPoint.Address.Equals(IPAddress.Any))
                    {
                        try
                        {
                            mySocket.BindEndpointAsync(aHostName, aPort).AsTask().Wait();

                            // Note: can receive messages from anybody.
                            myIsUnicastFlag = false;
                        }
                        catch (Exception err)
                        {
                            SocketErrorStatus aSocketError = SocketError.GetStatus(err.HResult);
                            throw new InvalidOperationException("DatagramSocket.BindEndpointAsync(..) failed to bind '" + endPoint.ToString() + "' SocketError: " + aSocketError, err);
                        }
                    }
                    else
                    {
                        try
                        {
                            mySocket.BindServiceNameAsync(aPort).AsTask().Wait();
                        }
                        catch (Exception err)
                        {
                            SocketErrorStatus aSocketError = SocketError.GetStatus(err.HResult);
                            throw new InvalidOperationException("DatagramSocket.BindServiceNameAsync(..) failed to bind '" + endPoint.ToString() + "' SocketError: " + aSocketError, err);
                        }
                    }
                }
            }

            public void Connect(IPEndPoint endPoint)
            {
                using (EneterTrace.Entering())
                {
                    HostName aHostName = new HostName(endPoint.Address.ToString());
                    string aPort = endPoint.Port.ToString();
                    try
                    {
                        mySocket.ConnectAsync(aHostName, aPort).AsTask().Wait();

                        // Note: can receive messages only from the address specified in ConnectAsync.
                        myIsUnicastFlag = true;
                    }
                    catch (Exception err)
                    {
                        SocketErrorStatus aSocketError = SocketError.GetStatus(err.HResult);
                        throw new InvalidOperationException("DatagramSocket.ConnectAsync(..) failed to connect '" + endPoint.ToString() + "' SocketError: " + aSocketError, err);
                    }
                }
            }

            public void Close()
            {
                using (EneterTrace.Entering())
                {
                    try
                    {
                        mySocket.Dispose();

                        // Note: this unsubscription sometime throws an exception - did not figure out why :-(.
                        mySocket.MessageReceived -= OnDatagramReceived;
                    }
                    catch
                    {
                    }
                }
            }

            public void SendTo(byte[] message, IPEndPoint endPoint)
            {
                using (EneterTrace.Entering())
                {
                    IOutputStream anOutputStream;
                    if (myIsUnicastFlag)
                    {
                        anOutputStream = mySocket.OutputStream;
                    }
                    else
                    {
                        HostName aHostName = new HostName(endPoint.Address.ToString());
                        string aPort = endPoint.Port.ToString();

                        Task<IOutputStream> aTask = mySocket.GetOutputStreamAsync(aHostName, aPort).AsTask();
                        aTask.Wait();
                        anOutputStream = aTask.Result;
                    }

                    using (DataWriter aWriter = new DataWriter(anOutputStream))
                    {
                        try
                        {
                            aWriter.WriteBytes(message);
                            aWriter.StoreAsync().AsTask().Wait();
                        }
                        catch (Exception err)
                        {
                            SocketErrorStatus aSocketError = SocketError.GetStatus(err.HResult);
                            throw new InvalidOperationException("DatagramSocket failed to send message to '" + endPoint.ToString() + "' SocketError: " + aSocketError, err);
                        }
                        finally
                        {
                            if (myIsUnicastFlag)
                            {
                                // Note: deteach the stream to avoid its dispose.
                                aWriter.DetachStream();
                            }
                        }
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
            private bool myIsUnicastFlag;
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

                        myIsListening = true;
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
