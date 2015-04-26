/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if SILVERLIGHT && !WINDOWS_PHONE_70

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    /// <summary>
    /// Provides TcpClient style class for Silverlight platform.
    /// </summary>
    internal class TcpClient : IDisposable
    {
        private class NetworkStream : Stream
        {
            public NetworkStream(Socket acceptedSocket)
            {
                using (EneterTrace.Entering())
                {
                    mySocket = acceptedSocket;

                    ConnectTimeout = 30000;
                    SendTimeout = 30000;
                    ReceiveTimeout = -1;

                    CreateSendingSocketAsyncEventArgs();
                    CreateReceiveSocketAsyncEventArgs();

                    ActivateReceiving();
                }
            }

            public NetworkStream(AddressFamily addressFamily)
            {
                using (EneterTrace.Entering())
                {
                    if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6)
                    {
                        throw new ArgumentException("Incorrect addressFamily value. Only AddressFamily.InterNetwork or AddressFamily.InterNetworkV6 is supported.");
                    }

                    mySocket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

                    ConnectTimeout = 30000;
                    SendTimeout = 30000;
                    ReceiveTimeout = -1;

                    CreateSendingSocketAsyncEventArgs();
                    CreateReceiveSocketAsyncEventArgs();
                }
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return true; } }
            public override void Flush() { }
            public override long Length { get { return 0; } }
            public override long Position { get { return 0; } set { } }
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                using (EneterTrace.Entering())
                {
                    return myReceiveStream.Read(buffer, offset, count);
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                using (EneterTrace.Entering())
                {
                    mySendCompletedSignal.Reset();
                    mySendingSocketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
                    mySocket.SendAsync(mySendingSocketAsyncEventArgs);

                    if (!mySendCompletedSignal.WaitOne(SendTimeout))
                    {
                        throw new TimeoutException("Failed to send the message within the timeout: " + mySendTimeout);
                    }

                    if (mySendingSocketAsyncEventArgs.SocketError != SocketError.Success)
                    {
                        throw new InvalidOperationException("Failed to send the message because of " + mySendingSocketAsyncEventArgs.SocketError.ToString());
                    }
                }
            }

            public override void Close()
            {
                using (EneterTrace.Entering())
                {
                    myStopReceivingRequestedFlag = true;

                    if (mySocket != null)
                    {
                        try
                        {
                            // This will close the connection with the server and it should
                            // also release the thread waiting for a response message.
                            mySocket.Close();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning("Failed to stop listening.", err);
                        }
                        finally
                        {
                            mySocket = null;
                        }
                    }

                    if (mySocketReceiverThread != null && mySocketReceiverThread.ThreadState != ThreadState.Unstarted)
                    {
                        if (!mySocketReceiverThread.Join(1000))
                        {
                            EneterTrace.Warning(ErrorHandler.FailedToStopThreadId + mySocketReceiverThread.ManagedThreadId);

                            try
                            {
                                mySocketReceiverThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(ErrorHandler.FailedToAbortThread, err);
                            }

                        }
                    }
                    mySocketReceiverThread = null;


                    if (myReceiveStream != null)
                    {
                        try
                        {
                            myReceiveStream.Close();
                        }
                        catch
                        {
                        }

                        myReceiveStream = null;
                    }
                }
            }

            public void Connect(string hostName, int port)
            {
                using (EneterTrace.Entering())
                {
                    if (Connected)
                    {
                        throw new SocketException((int)SocketError.IsConnected);
                    }

                    try
                    {
                        EndPoint anEndPoint = new DnsEndPoint(hostName, port);
                        CreateConnectionSocketAsyncEventArgs(anEndPoint);

                        myConnectionCompletedSignal.Reset();
                        mySocket.ConnectAsync(myConnectionSocketAsyncEventArgs);

                        if (!myConnectionCompletedSignal.WaitOne(ConnectTimeout))
                        {
                            throw new InvalidOperationException("Failed to open the connection within the configured timeout " + ConnectTimeout.ToString() + " ms.");
                        }

                        if (myConnectionSocketAsyncEventArgs.SocketError != SocketError.Success)
                        {
                            throw new InvalidOperationException("Failed to open the connection because " + myConnectionSocketAsyncEventArgs.SocketError.ToString());
                        }

                        ActivateReceiving();
                    }
                    catch
                    {
                        try
                        {
                            // Just try to close.
                            Close();
                        }
                        catch
                        {
                        }

                        throw;
                    }
                }
            }

            public bool Connected { get { return myIsListeningToSockets == true; } }

            public int ConnectTimeout
            {
                get { return myConnectTimeout; }
                set { myConnectTimeout = (value != 0) ? value : -1; }
            }

            public int SendTimeout
            {
                get { return mySendTimeout; }
                set { mySendTimeout = (value != 0) ? value : -1; }
            }

            public int ReceiveTimeout
            {
                get { return myReceiveTimeout; }
                set { myReceiveTimeout = (value != 0) ? value : -1; }
            }

            public int SendBufferSize
            {
                get { return mySocket.SendBufferSize; }
                set { mySocket.SendBufferSize = value; }
            }

            public int ReceiveBufferSize
            {
                get { return mySocket.ReceiveBufferSize; }
                set { mySocket.ReceiveBufferSize = value; }
            }

            public bool NoDelay
            {
                get { return mySocket.NoDelay; }
                set { mySocket.NoDelay = value; }
            }

            public Socket Client { get { return mySocket; } }

            private void CreateConnectionSocketAsyncEventArgs(EndPoint endPoint)
            {
                using (EneterTrace.Entering())
                {
                    myConnectionSocketAsyncEventArgs = new SocketAsyncEventArgs();
                    myConnectionSocketAsyncEventArgs.RemoteEndPoint = endPoint;
                    myConnectionSocketAsyncEventArgs.Completed += (x, y) => myConnectionCompletedSignal.Set();
                }
            }

            private void CreateSendingSocketAsyncEventArgs()
            {
                using (EneterTrace.Entering())
                {
                    mySendingSocketAsyncEventArgs = new SocketAsyncEventArgs();
                    mySendingSocketAsyncEventArgs.Completed += (x, y) => mySendCompletedSignal.Set();
                }
            }

            private void CreateReceiveSocketAsyncEventArgs()
            {
                using (EneterTrace.Entering())
                {
                    byte[] aResponseBuffer = new byte[32768];
                    myReceiveSocketAsyncEventArgs = new SocketAsyncEventArgs();
                    myReceiveSocketAsyncEventArgs.SetBuffer(aResponseBuffer, 0, aResponseBuffer.Length);
                    myReceiveSocketAsyncEventArgs.Completed += (x, y) =>
                        {
                            using (EneterTrace.Entering())
                            {
                                if (y.SocketError == SocketError.Success)
                                {
                                    myReceiveStream.Write(y.Buffer, 0, y.BytesTransferred);

                                    // If the connection was closed.
                                    if (y.BytesTransferred == 0)
                                    {
                                        y.SocketError = SocketError.SocketError;
                                        EneterTrace.Error("Failed to receive the message because the connection was closed.");
                                    }
                                }

                                // Indicate the message is received.
                                myReceivingCompletedSignal.Set();
                            }
                        };
                }
            }

            private void ActivateReceiving()
            {
                using (EneterTrace.Entering())
                {
                    myStopReceivingRequestedFlag = false;

                    // Open stream for writing receied data from the socket.
                    myReceiveStream = new DynamicStream();

                    myListeningToSocketsStartedEvent.Reset();

                    // Thread responsible for reading RAW data from the socket and writing them to
                    // the DynamicStream.
                    // It writes bytes to the stream as they come from the network.
                    mySocketReceiverThread = new Thread(DoReceiveSocket);
                    mySocketReceiverThread.Start();

                    // Wait until the thread is running.
                    myListeningToSocketsStartedEvent.WaitOne(3000);
                }
            }

            private void DoReceiveSocket()
            {
                using (EneterTrace.Entering())
                {
                    myIsListeningToSockets = true;
                    myListeningToSocketsStartedEvent.Set();

                    while (!myStopReceivingRequestedFlag)
                    {
                        try
                        {
                            myReceivingCompletedSignal.Reset();
                            mySocket.ReceiveAsync(myReceiveSocketAsyncEventArgs);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error("Failed to receive the response message.", err);
                            myReceiveSocketAsyncEventArgs.SocketError = SocketError.SocketError;
                            myReceivingCompletedSignal.Set();
                        }

                        if (!myReceivingCompletedSignal.WaitOne(ReceiveTimeout))
                        {
                            EneterTrace.Error("Failed to receive the response within the specified timeout " + ReceiveTimeout + " ms.");

                            // Close the connection.
                            Close();

                            break;
                        }

                        if (myReceiveSocketAsyncEventArgs.SocketError != SocketError.Success)
                        {
                            // Closing the dynamic stream will also stop listening to messages
                            // from the dynamic stream.
                            myReceiveStream.Close();

                            // Stop listening to sockets
                            break;
                        }
                    }

                    myIsListeningToSockets = false;
                }
            }

            private int mySendTimeout;
            private int myReceiveTimeout;

            private Socket mySocket;
            private DynamicStream myReceiveStream;

            private SocketAsyncEventArgs myConnectionSocketAsyncEventArgs = new SocketAsyncEventArgs();
            private ManualResetEvent myConnectionCompletedSignal = new ManualResetEvent(false);
            private int myConnectTimeout;

            private SocketAsyncEventArgs mySendingSocketAsyncEventArgs;
            private ManualResetEvent mySendCompletedSignal = new ManualResetEvent(false);

            private bool myIsListeningToSockets;
            private ManualResetEvent myListeningToSocketsStartedEvent = new ManualResetEvent(false);
            private Thread mySocketReceiverThread;
            private SocketAsyncEventArgs myReceiveSocketAsyncEventArgs;
            private ManualResetEvent myReceivingCompletedSignal = new ManualResetEvent(false);
            private volatile bool myStopReceivingRequestedFlag;
        }

        public TcpClient(AddressFamily addressFamily)
        {
            using (EneterTrace.Entering())
            {
                myNetworkStream = new NetworkStream(addressFamily);
            }
        }

        public TcpClient(Socket acceptedSocket)
        {
            using (EneterTrace.Entering())
            {
                myNetworkStream = new NetworkStream(acceptedSocket);
            }
        }

        public Socket Client { get { return myNetworkStream.Client; } }

        public int SendBufferSize
        {
            get { return myNetworkStream.SendBufferSize; }
            set { myNetworkStream.SendBufferSize = value; }
        }

        public int ReceiveBufferSize
        {
            get { return myNetworkStream.ReceiveBufferSize; }
            set { myNetworkStream.ReceiveBufferSize = value; }
        }

        public bool NoDelay
        {
            get { return myNetworkStream.NoDelay; }
            set { myNetworkStream.NoDelay = value; }
        }


        /// <summary>
        /// Timeout to establish the connection.
        /// </summary>
        public int ConnectTimeout
        {
            get { return myNetworkStream.ConnectTimeout; }
            set { myNetworkStream.ConnectTimeout = value; }
        }

        /// <summary>
        /// Timeout to send a message.
        /// </summary>
        public int SendTimeout
        {
            get { return myNetworkStream.SendTimeout; }
            set { myNetworkStream.SendTimeout = value; }
        }

        /// <summary>
        /// Timeout to receive a message.
        /// </summary>
        public int ReceiveTimeout
        {
            get { return myNetworkStream.ReceiveTimeout; }
            set { myNetworkStream.ReceiveTimeout = value; }
        }

        /// <summary>
        /// Opens TCP connection with the server.
        /// </summary>
        /// <remarks>
        /// Please notice, Silverlight framework requires the TCP policy server running on the server side.
        /// Before opening the connection, Silverlight tries to connect the server on the port 943 and asks for
        /// the policy xml. If the server responses and returns a policy xml which allows the connection only then
        /// Silverlight allows to open the required TCP connection. See TcpPolicyServer in Eneter framework.
        /// </remarks>
        public void Connect(string hostName, int port)
        {
            using (EneterTrace.Entering())
            {
                myNetworkStream.Connect(hostName, port);
            }
        }

        /// <summary>
        /// Closes TCP connection with the server.
        /// </summary>
        public void Close()
        {
            using (EneterTrace.Entering())
            {
                myNetworkStream.Close();
            }
        }

        /// <summary>
        /// Returns true if the connection with the server is open.
        /// </summary>
        public bool Connected { get { return myNetworkStream.Connected; } }

        /// <summary>
        /// Returns stream to read and write messages from/to the server.
        /// </summary>
        /// <remarks>
        /// Reading the stream blocks the thread until data is available or the connection is closed.
        /// </remarks>
        /// <returns></returns>
        public Stream GetStream()
        {
            using (EneterTrace.Entering())
            {
                return myNetworkStream;
            }
        }

        void IDisposable.Dispose()
        {
            try
            {
                Close();
            }
            catch
            {
            }
        }


        private NetworkStream myNetworkStream;
    }
}

#endif