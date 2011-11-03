/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT
#if !WINDOWS_PHONE

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpDuplexOutputChannel_Silverlight : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;


        public TcpDuplexOutputChannel_Silverlight(string ipAddressAndPort, string responseReceiverId, int sendMessageTimeout, bool isResponseReceivedInSilverlightThread)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(ipAddressAndPort))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                try
                {
                    // just check if the address is valid
                    myUriBuilder = new UriBuilder(ipAddressAndPort);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                ChannelId = ipAddressAndPort;
                mySendMessageTimeout = sendMessageTimeout;

                myIsResponseReceivedInSilverlightThread = isResponseReceivedInSilverlightThread;

                ResponseReceiverId = (string.IsNullOrEmpty(responseReceiverId)) ? ipAddressAndPort + "_" + Guid.NewGuid().ToString() : responseReceiverId;

                // 4502 and 4532 ???
                //DnsEndPoint anEndPoint = new DnsEndPoint(Application.Current.Host.Source.DnsSafeHost, 4530);
                DnsEndPoint anEndPoint = new DnsEndPoint(myUriBuilder.Host, myUriBuilder.Port);

                myConnectionSocketAsyncEventArgs = new SocketAsyncEventArgs();
                myConnectionSocketAsyncEventArgs.UserToken = mySocket;
                myConnectionSocketAsyncEventArgs.RemoteEndPoint = anEndPoint;
                myConnectionSocketAsyncEventArgs.Completed += (x, y) => myConnectionCompletedSignal.Set();

                mySendingSocketAsyncEventArgs = new SocketAsyncEventArgs();
                mySendingSocketAsyncEventArgs.RemoteEndPoint = anEndPoint;
                mySendingSocketAsyncEventArgs.Completed += (xx, yy) => mySendCompletedSignal.Set();

                byte[] aResponseBuffer = new byte[32768];
                myReceiveSocketAsyncEventArgs = new SocketAsyncEventArgs();
                myReceiveSocketAsyncEventArgs.RemoteEndPoint = anEndPoint;
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


        public string ChannelId { get; private set; }

        public string ResponseReceiverId { get; private set; }

        public void OpenConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }


                    // If it is needed clear after previous connection
                    if (mySocket != null)
                    {
                        try
                        {
                            CloseConnection();
                        }
                        catch
                        {
                            // We tried to clean after the previous connection. The exception can be ignored.
                        }
                    }

                    try
                    {
                        mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        ConnectSocket();

                        myStopReceivingRequestedFlag = false;

                        myMessageProcessingThread.RegisterMessageHandler(MessageHandler);

                        // Open the dynamic stream for writing data from the socket and reading messages.
                        myDynamicStream = new DynamicStream();

                        // Thread responsible for reading meaningfull message from the DynamicStream.
                        // It recognizes messages from RAW bytes in the stream. If the message is
                        // not complete it waits for missing data.
                        myResponseReceiverThread = new Thread(DoResponseListening);
                        myResponseReceiverThread.Start();

                        // Thread responsible for reading RAW data from the socket and writing them to
                        // the DynamicStream.
                        // It writes bytes to the stream as they come from the network.
                        mySocketReceiverThread = new Thread(ReceiveSocket);
                        mySocketReceiverThread.Start();

                        // Send open connection message with receiver id.
                        byte[] aBufferedMessage = null;
                        using (MemoryStream aMemoryStream = new MemoryStream())
                        {
                            MessageStreamer.WriteOpenConnectionMessage(aMemoryStream, ResponseReceiverId);
                            aBufferedMessage = aMemoryStream.ToArray();
                        }
                        SendSocket(aBufferedMessage);

                        // Notify, the connection was opened.
                        NotifyConnectionOpened();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.OpenConnectionFailure, err);

                        try
                        {
                            CloseConnection();
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

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myStopReceivingRequestedFlag = true;

                    if (mySocket != null)
                    {
                        // Try to notify that the connection is closed
                        try
                        {
                            if (!string.IsNullOrEmpty(ResponseReceiverId))
                            {
                                MemoryStream aMemoryStream = new MemoryStream();
                                MessageStreamer.WriteCloseConnectionMessage(aMemoryStream, ResponseReceiverId);
                                byte[] aBufferedMessage = aMemoryStream.ToArray();

                                SendSocket(aBufferedMessage);
                            }
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                        }

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
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + mySocketReceiverThread.ManagedThreadId);

                            try
                            {
                                mySocketReceiverThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                            }

                        }
                    }
                    mySocketReceiverThread = null;


                    if (myResponseReceiverThread != null && myResponseReceiverThread.ThreadState != ThreadState.Unstarted)
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

                    try
                    {
                        myMessageProcessingThread.UnregisterMessageHandler();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.UnregisterMessageHandlerThreadFailure, err);
                    }

                }
            }
        }

        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return mySocket != null && myIsListeningToResponses;
                    }
                }
            }
        }


        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (!IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.SendMessageNotConnectedFailure;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Store the message to the buffer.
                        byte[] aBufferedMessage = null;
                        using (MemoryStream aMemStream = new MemoryStream())
                        {
                            MessageStreamer.WriteRequestMessage(aMemStream, ResponseReceiverId, message);
                            aBufferedMessage = aMemStream.ToArray();
                        }

                        SendSocket(aBufferedMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                        throw;
                    }
                }
            }
        }


        private void DoResponseListening()
        {
            using (EneterTrace.Entering())
            {
                myIsListeningToResponses = true;

                try
                {
                    while (!myStopReceivingRequestedFlag)
                    {
                        object aMessage = MessageStreamer.ReadMessage(myDynamicStream);

                        if (!myStopReceivingRequestedFlag && aMessage != null)
                        {
                            myMessageProcessingThread.EnqueueMessage(aMessage);
                        }

                        // If disconnected
                        if (aMessage == null || !mySocket.Connected)
                        {
                            EneterTrace.Warning(TracedObject + "detected the duplex input channel is not available. The connection will be closed.");
                            break;
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.DoListeningFailure, err);
                }

                myIsListeningToResponses = false;

                // Notify the listening to messages stoped.
                NotifyConnectionClosed();
            }
        }

        private void MessageHandler(object message)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseMessageReceived != null)
                {
                    // Notify the message in the main Silverlight thread.
                    if (myIsResponseReceivedInSilverlightThread)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                using (EneterTrace.Entering())
                                {
                                    try
                                    {
                                        ResponseMessageReceived(this, new DuplexChannelMessageEventArgs(ChannelId, message, ResponseReceiverId));
                                    }
                                    catch (Exception err)
                                    {
                                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                    }
                                }
                            });
                    }
                    else
                    {
                        // Incoming response messages are notified in the receiving thread.
                        // This is not the Silverlight main thread.
                        try
                        {
                            ResponseMessageReceived(this, new DuplexChannelMessageEventArgs(ChannelId, message, ResponseReceiverId));
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }


        private void ConnectSocket()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    mySocket.ConnectAsync(myConnectionSocketAsyncEventArgs);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.OpenConnectionFailure, err);
                    throw;
                }

                if (!myConnectionCompletedSignal.WaitOne(mySendMessageTimeout))
                {
                    string aMessage = TracedObject + TracedObject + "failed to open the connection within the configured timeout " + mySendMessageTimeout.ToString() + " ms.";
                    EneterTrace.Error(aMessage);
                    throw new InvalidOperationException(aMessage);
                }

                if (myConnectionSocketAsyncEventArgs.SocketError != SocketError.Success)
                {
                    string aMessage = TracedObject + "failed to open the connection because " + myConnectionSocketAsyncEventArgs.SocketError.ToString();
                    EneterTrace.Error(aMessage);
                    throw new InvalidOperationException(aMessage);
                }
            }
        }

        private void SendSocket(byte[] message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    mySendingSocketAsyncEventArgs.SetBuffer(message, 0, message.Length);
                    mySocket.SendAsync(mySendingSocketAsyncEventArgs);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }

                if (!mySendCompletedSignal.WaitOne(mySendMessageTimeout))
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

        private void ReceiveSocket()
        {
            using (EneterTrace.Entering())
            {
                while (!myStopReceivingRequestedFlag)
                {
                    try
                    {
                        mySocket.ReceiveAsync(myReceiveSocketAsyncEventArgs);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed to receive the response message.", err);
                        myReceiveSocketAsyncEventArgs.SocketError = SocketError.SocketError;
                        myReceivingCompletedSignal.Set();
                    }

                    myReceivingCompletedSignal.WaitOne();

                    if (myReceiveSocketAsyncEventArgs.SocketError != SocketError.Success)
                    {
                        // Closing the dynamic stream will also stop listening to messages
                        // from the dynamic stream.
                        myDynamicStream.Close();

                        // Stop listening to sockets
                        break;
                    }
                }
            }
        }

        private void NotifyConnectionOpened()
        {
            using (EneterTrace.Entering())
            {
                WaitCallback aConnectionOpenedInvoker = x =>
                    {
                        using (EneterTrace.Entering())
                        {
                            try
                            {
                                if (ConnectionOpened != null)
                                {
                                    DuplexChannelEventArgs aMsg = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId);
                                    ConnectionOpened(this, aMsg);
                                }
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                    };

                // Invoke the event in a different thread.
                ThreadPool.QueueUserWorkItem(aConnectionOpenedInvoker);
            }
        }

        private void NotifyConnectionClosed()
        {
            using (EneterTrace.Entering())
            {
                // Execute the callback in a different thread.
                // The problem is, the event handler can call back to the duplex output channel - e.g. trying to open
                // connection - and since this closing is not finished and this thread would be blocked, .... => problems.
                WaitCallback aNotifyier = x =>
                    {
                        using (EneterTrace.Entering())
                        {
                            try
                            {
                                if (ConnectionClosed != null)
                                {
                                    ConnectionClosed(this, new DuplexChannelEventArgs(ChannelId, ResponseReceiverId));
                                }
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                    };

                ThreadPool.QueueUserWorkItem(aNotifyier);
            }
        }


        private int mySendMessageTimeout;

        private Socket mySocket;

        private SocketAsyncEventArgs myConnectionSocketAsyncEventArgs = new SocketAsyncEventArgs();
        AutoResetEvent myConnectionCompletedSignal = new AutoResetEvent(false);

        private SocketAsyncEventArgs mySendingSocketAsyncEventArgs;
        private AutoResetEvent mySendCompletedSignal = new AutoResetEvent(false);

        private SocketAsyncEventArgs myReceiveSocketAsyncEventArgs;
        private AutoResetEvent myReceivingCompletedSignal = new AutoResetEvent(false);

        private object myConnectionManipulatorLock = new object();

        private Thread myResponseReceiverThread;
        private Thread mySocketReceiverThread;
        private volatile bool myStopReceivingRequestedFlag;
        private volatile bool myIsListeningToResponses;
        private DynamicStream myDynamicStream;

        private UriBuilder myUriBuilder;

        private bool myIsResponseReceivedInSilverlightThread;

        private WorkingThread<object> myMessageProcessingThread = new WorkingThread<object>();

        private string TracedObject
        {
            get
            {
                string aChannelId = (ChannelId != null) ? ChannelId : "";
                return "The Tcp duplex output channel '" + aChannelId + "' ";
            }
        }
    }
}

#endif
#endif