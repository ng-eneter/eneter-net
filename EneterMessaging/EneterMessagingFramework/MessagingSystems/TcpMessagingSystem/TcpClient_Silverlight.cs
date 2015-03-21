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
    public class TcpClient : IDisposable
    {
        /// <summary>
        /// Constructs TcpSilverlightClient.
        /// </summary>
        /// <param name="host">host name or ip address</param>
        /// <param name="port">port number</param>
        public TcpClient(string host, int port)
            : this(new DnsEndPoint(host, port))
        {
        }

        /// <summary>
        /// Constructs TcpSilverlightClient.
        /// </summary>
        /// <param name="endPoint">server address</param>
        public TcpClient(EndPoint endPoint)
        {
            using (EneterTrace.Entering())
            {
                ConnectTimeout = 30000;
                SendTimeout = 30000;
                ReceiveTimeout = -1;

                myConnectionSocketAsyncEventArgs = new SocketAsyncEventArgs();
                myConnectionSocketAsyncEventArgs.RemoteEndPoint = endPoint;
                myConnectionSocketAsyncEventArgs.Completed += (x, y) => myConnectionCompletedSignal.Set();

                mySendingSocketAsyncEventArgs = new SocketAsyncEventArgs();
                mySendingSocketAsyncEventArgs.RemoteEndPoint = endPoint;
                mySendingSocketAsyncEventArgs.Completed += (xx, yy) => mySendCompletedSignal.Set();

                byte[] aResponseBuffer = new byte[32768];
                myReceiveSocketAsyncEventArgs = new SocketAsyncEventArgs();
                myReceiveSocketAsyncEventArgs.RemoteEndPoint = endPoint;
                myReceiveSocketAsyncEventArgs.SetBuffer(aResponseBuffer, 0, aResponseBuffer.Length);
                myReceiveSocketAsyncEventArgs.Completed += (xx, yy) =>
                    {
                        using (EneterTrace.Entering())
                        {
                            if (yy.SocketError == SocketError.Success)
                            {
                                myDynamicStream.Write(yy.Buffer, 0, yy.BytesTransferred);

                                // If the connection was closed.
                                if (yy.BytesTransferred == 0)
                                {
                                    yy.SocketError = SocketError.SocketError;
                                    EneterTrace.Error(TracedObject + "failed to receive the message because the connection was closed.");
                                }
                            }

                            // Indicate the message is received.
                            myReceivingCompletedSignal.Set();
                        }
                    };
            }
        }

        /// <summary>
        /// Timeout to establish the connection.
        /// </summary>
        public int ConnectTimeout
        {
            get
            {
                return myConnectTimeout;
            }

            set
            {
                myConnectTimeout = (value != 0) ? value : -1;
            }
        }

        /// <summary>
        /// Timeout to send a message.
        /// </summary>
        public int SendTimeout
        {
            get
            {
                return mySendTimeout;
            }

            set
            {
                mySendTimeout = (value != 0) ? value : -1;
            }
        }

        /// <summary>
        /// Timeout to receive a message.
        /// </summary>
        public int ReceiveTimeout
        {
            get
            {
                return myReceiveTimeout;
            }

            set
            {
                myReceiveTimeout = (value != 0) ? value : -1;
            }
        }

        /// <summary>
        /// Connects one-way. To send messages without receiving responses.
        /// </summary>
        /// <remarks>
        /// If you do not need to receive response messages from the server then establising oneway
        /// connection increases the performance because it does not start threads looping for response messages.
        /// </remarks>
        public void ConnectOneWay()
        {
            using (EneterTrace.Entering())
            {
                Connect(true);
            }
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
        public void Connect()
        {
            using (EneterTrace.Entering())
            {
                Connect(false);
            }
        }

        /// <summary>
        /// Closes TCP connection with the server.
        /// </summary>
        public void Close()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
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
                            EneterTrace.Warning(TracedObject + "failed to stop listening.", err);
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
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToStopThreadId + mySocketReceiverThread.ManagedThreadId);

                            try
                            {
                                mySocketReceiverThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.FailedToAbortThread, err);
                            }

                        }
                    }
                    mySocketReceiverThread = null;


                    if (myDynamicStream != null)
                    {
                        try
                        {
                            myDynamicStream.Close();
                        }
                        catch
                        {
                        }

                        myDynamicStream = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the connection with the server is open.
        /// </summary>
        public bool Connected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return mySocket != null && myIsOneWayConnection == true ||
                               mySocket != null && myIsOneWayConnection == false && myIsListeningToSockets == true;
                    }
                }
            }
        }

        /// <summary>
        /// Sends message to the server.
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (!Connected)
                    {
                        string aMessage = TracedObject + ErrorHandler.FailedToSendMessageBecauseNotConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        mySendCompletedSignal.Reset();
                        mySendingSocketAsyncEventArgs.SetBuffer(data, 0, data.Length);
                        mySocket.SendAsync(mySendingSocketAsyncEventArgs);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendMessage, err);
                        throw;
                    }

                    if (!mySendCompletedSignal.WaitOne(SendTimeout))
                    {
                        string aMessage = TracedObject + "failed to send the message within a specified timeout.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    if (mySendingSocketAsyncEventArgs.SocketError != SocketError.Success)
                    {
                        string aMessage = TracedObject + "failed to send the message because " + mySendingSocketAsyncEventArgs.SocketError.ToString();
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Returns stream to read messages from the server.
        /// </summary>
        /// <remarks>
        /// Reading the stream blocks the thread until data is available or the connection is closed.
        /// </remarks>
        /// <returns></returns>
        public Stream GetInputStream()
        {
            using (EneterTrace.Entering())
            {
                if (myIsOneWayConnection)
                {
                    throw new InvalidOperationException(TracedObject + "has one-way connection. Messages can be sent but not read.");
                }

                return myDynamicStream;
            }
        }

        private void Connect(bool isOneWayConnection)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    try
                    {
                        try
                        {
                            myConnectionCompletedSignal.Reset();
                            mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            mySocket.ConnectAsync(myConnectionSocketAsyncEventArgs);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.FailedToOpenConnection, err);
                            throw;
                        }

                        if (!myConnectionCompletedSignal.WaitOne(ConnectTimeout))
                        {
                            string aMessage = TracedObject + TracedObject + "failed to open the connection within the configured timeout " + ConnectTimeout.ToString() + " ms.";
                            EneterTrace.Error(aMessage);
                            throw new InvalidOperationException(aMessage);
                        }

                        if (myConnectionSocketAsyncEventArgs.SocketError != SocketError.Success)
                        {
                            string aMessage = TracedObject + "failed to open the connection because " + myConnectionSocketAsyncEventArgs.SocketError.ToString();
                            EneterTrace.Error(aMessage);
                            throw new InvalidOperationException(aMessage);
                        }

                        try
                        {
                            myIsOneWayConnection = isOneWayConnection;
                            myStopReceivingRequestedFlag = false;

                            // Open the dynamic stream for writing data from the socket and reading messages.
                            myDynamicStream = new DynamicStream();

                            // If we want to receive response messages too.
                            if (!isOneWayConnection)
                            {
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
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.FailedToOpenConnection, err);
                            throw;
                        }
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
                        EneterTrace.Error(TracedObject + "failed to receive the response message.", err);
                        myReceiveSocketAsyncEventArgs.SocketError = SocketError.SocketError;
                        myReceivingCompletedSignal.Set();
                    }

                    if (!myReceivingCompletedSignal.WaitOne(ReceiveTimeout))
                    {
                        EneterTrace.Error(TracedObject + "failed to receive the response within the specified timeout " + ReceiveTimeout + " ms.");

                        // Close the connection.
                        Close();

                        break;
                    }

                    if (myReceiveSocketAsyncEventArgs.SocketError != SocketError.Success)
                    {
                        // Closing the dynamic stream will also stop listening to messages
                        // from the dynamic stream.
                        myDynamicStream.Close();

                        // Stop listening to sockets
                        break;
                    }
                }

                myIsListeningToSockets = false;
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

        private Socket mySocket;
        private object myConnectionManipulatorLock = new object();
        private bool myIsOneWayConnection;
        private bool myIsListeningToSockets;
        private ManualResetEvent myListeningToSocketsStartedEvent = new ManualResetEvent(false);

        private Thread mySocketReceiverThread;

        private SocketAsyncEventArgs myConnectionSocketAsyncEventArgs = new SocketAsyncEventArgs();
        private ManualResetEvent myConnectionCompletedSignal = new ManualResetEvent(false);

        private SocketAsyncEventArgs mySendingSocketAsyncEventArgs;
        private ManualResetEvent mySendCompletedSignal = new ManualResetEvent(false);

        private SocketAsyncEventArgs myReceiveSocketAsyncEventArgs;
        private ManualResetEvent myReceivingCompletedSignal = new ManualResetEvent(false);

        private int myConnectTimeout;
        private int mySendTimeout;
        private int myReceiveTimeout;

        private DynamicStream myDynamicStream;
        private volatile bool myStopReceivingRequestedFlag;

        private string TracedObject { get {return GetType().Name + " "; } }
    }
}

#endif