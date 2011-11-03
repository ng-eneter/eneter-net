/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        public TcpDuplexOutputChannel(string ipAddressAndPort, string responseReceiverId, ISecurityFactory securityStreamFactory)
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
                    new UriBuilder(ipAddressAndPort);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                mySecurityStreamFactory = securityStreamFactory;

                ChannelId = ipAddressAndPort;

                ResponseReceiverId = (string.IsNullOrEmpty(responseReceiverId)) ? ipAddressAndPort + "_" + Guid.NewGuid().ToString() : responseReceiverId;
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
                    if (myTcpClient != null)
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
                        myStopReceivingRequestedFlag = false;

                        myTcpClient = new TcpClient();
                        myTcpClient.NoDelay = true;
                        UriBuilder aUriBuilder = new UriBuilder(ChannelId);
                        myTcpClient.Connect(IPAddress.Parse(aUriBuilder.Host), aUriBuilder.Port);

                        if (mySecurityStreamFactory != null)
                        {
                            NetworkStream aStream = myTcpClient.GetStream();
                            myClientStream = mySecurityStreamFactory.CreateSecurityStreamAndAuthenticate(aStream);
                        }
                        else
                        {
                            myClientStream = myTcpClient.GetStream();
                        }

                        myMessageProcessingThread.RegisterMessageHandler(MessageHandler);

                        myResponseReceiverThread = new Thread(DoResponseListening);
                        myResponseReceiverThread.Start();

                        // Send open connection message with receiver id.
                        MessageStreamer.WriteOpenConnectionMessage(myClientStream, ResponseReceiverId);

                        // Invoke the event notifying, the connection was opened.
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

                    if (myTcpClient != null)
                    {
                        // Try to notify that the connection is closed
                        if (myClientStream != null && !string.IsNullOrEmpty(ResponseReceiverId))
                        {
                            try
                            {
                                object[] aMessage = MessageStreamer.GetCloseConnectionMessage(ResponseReceiverId);
                                MessageStreamer.WriteMessage(myClientStream, aMessage);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                            }

                            myClientStream.Close();
                        }

                        try
                        {
                            // This will close the connection with the server and it should
                            // also release the thread waiting for a response message.
                            myTcpClient.Close();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to stop Tcp connection.", err);
                        }

                        myTcpClient = null;
                    }

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
                        return myTcpClient != null && myIsListeningToResponses;
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
                        object[] aMessage = { (byte)2, ResponseReceiverId, message };

                        // Store the message to the buffer.
                        byte[] aBufferedMessage = null;
                        using (MemoryStream aMemStream = new MemoryStream())
                        {
                            MessageStreamer.WriteMessage(aMemStream, aMessage);
                            aBufferedMessage = aMemStream.ToArray();
                        }

                        // Send the message from the buffer.
                        myClientStream.Write(aBufferedMessage, 0, aBufferedMessage.Length);
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
                        object aMessage = MessageStreamer.ReadMessage(myClientStream);

                        if (!myStopReceivingRequestedFlag && aMessage != null)
                        {
                            myMessageProcessingThread.EnqueueMessage(aMessage);
                        }

                        // If disconnected
                        if (aMessage == null || !myTcpClient.Connected)
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

                // Stop the thread processing messages
                try
                {
                    myMessageProcessingThread.UnregisterMessageHandler();
                }
                catch
                {
                    // We need just to close it, therefore we are not interested about exception.
                    // They can be ignored here.
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
                    try
                    {
                        ResponseMessageReceived(this, new DuplexChannelMessageEventArgs(ChannelId, message, ResponseReceiverId));
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
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
                WaitCallback anInvokeConnectionClosed = x =>
                    {
                        using (EneterTrace.Entering())
                        {
                            try
                            {
                                if (ConnectionClosed != null)
                                {
                                    DuplexChannelEventArgs aMsg = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId);
                                    ConnectionClosed(this, aMsg);
                                }
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                    };
                ThreadPool.QueueUserWorkItem(anInvokeConnectionClosed);
            }
        }



        private TcpClient myTcpClient;
        private object myConnectionManipulatorLock = new object();

        private ISecurityFactory mySecurityStreamFactory;
        private Stream myClientStream;

        private Thread myResponseReceiverThread;
        private volatile bool myStopReceivingRequestedFlag;
        private volatile bool myIsListeningToResponses;

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