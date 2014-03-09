/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if SILVERLIGHT && !WINDOWS_PHONE_70

using System;
using System.IO;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpOutputConnector : IOutputConnector
    {
        public TcpOutputConnector(string ipAddressAndPort,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout)
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

                myConnectionTimeout = connectTimeout;
                mySendTimeout = sendTimeout;
                myReceiveTimeout = receiveTimeout;
            }
        }

        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
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
                        // 4502 and 4532 ???
                        myTcpClient = new TcpClient(myUriBuilder.Host, myUriBuilder.Port);

                        myTcpClient.ConnectTimeout = myConnectionTimeout;
                        myTcpClient.SendTimeout = mySendTimeout;
                        myTcpClient.ReceiveTimeout = myReceiveTimeout;

                        myTcpClient.Connect();

                        myStopReceivingRequestedFlag = false;
                        myResponseMessageHandler = responseMessageHandler;

                        // Thread responsible for reading meaningfull message from the DynamicStream.
                        // It recognizes messages from RAW bytes in the stream. If the message is
                        // not complete it waits for missing data.
                        myResponseReceiverThread = new Thread(DoResponseListening);
                        myResponseReceiverThread.Start();

                        // Try to wait until the thread listening to responses started.
                        myListeningToResponsesStartedEvent.WaitOne(1000);
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
                        myTcpClient.Close();
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

                    myResponseMessageHandler = null;
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

        public bool IsStreamWritter { get { return false; } }
        

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    byte[] aMessage = (byte[])message;
                    myTcpClient.Send(aMessage);
                }
            }
        }

        public void SendMessage(Action<System.IO.Stream> toStreamWritter)
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
                    Stream anInputStream = myTcpClient.GetInputStream();
                    MessageContext aContext = new MessageContext(anInputStream, "", null);

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
                    ThreadPool.QueueUserWorkItem(x => CloseConnection());
                }
            }
        }



        private object myConnectionManipulatorLock = new object();
        private TcpClient myTcpClient;
        private UriBuilder myUriBuilder;
        private int myConnectionTimeout;
        private int mySendTimeout;
        private int myReceiveTimeout;
        private volatile bool myStopReceivingRequestedFlag;
        private volatile bool myIsListeningToResponses;
        private Thread myResponseReceiverThread;
        private ManualResetEvent myListeningToResponsesStartedEvent = new ManualResetEvent(false);
        private Func<MessageContext, bool> myResponseMessageHandler;



        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif