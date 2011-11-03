/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


#if !SILVERLIGHT

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using System.Collections.Generic;
using System.IO;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal abstract class TcpInputChannelBase
    {
        public TcpInputChannelBase(string ipAddressAndPort, ISecurityFactory serverSecurityFactory)
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
                    myUriBuilder = new UriBuilder(ipAddressAndPort);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                mySecurityStreamFactory = serverSecurityFactory;

                ChannelId = ipAddressAndPort;
                myMessageProcessingThread = new WorkingThread<object>(ipAddressAndPort);
            }
        }

        public string ChannelId { get; private set; }

        public void StartListening()
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

                    try
                    {
                        myStopTcpListeningRequested = false;

                        // Start the working thread for removing messages from the queue
                        myMessageProcessingThread.RegisterMessageHandler(MessageHandler);

                        myTcpListener = new TcpListener(IPAddress.Parse(myUriBuilder.Host), myUriBuilder.Port);
                        myTcpListener.Server.LingerState = new LingerOption(true, 0);
                        myTcpListener.Start();

                        // Listen in another thread
                        myTcpListeningThread = new Thread(DoTcpListening);
                        myTcpListeningThread.Start();
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
                    myStopTcpListeningRequested = true;

                    try
                    {
                        DisconnectClients();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to close Tcp connections with clients.", err);
                    }

                    if (myTcpListener != null)
                    {
                        try
                        {
                            myTcpListener.Stop();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                        }
                        myTcpListener = null;
                    }

                    if (myTcpListeningThread != null && myTcpListeningThread.ThreadState != ThreadState.Unstarted)
                    {
                        if (!myTcpListeningThread.Join(1000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myTcpListeningThread.ManagedThreadId.ToString());

                            try
                            {
                                myTcpListeningThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                            }
                        }
                    }
                    myTcpListeningThread = null;

                    // Stop thread processing the queue with messages.
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

        public bool IsListening
        { 
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myListeningManipulatorLock)
                    {
                        return myTcpListener != null;
                    }
                }
            }
        }

        /// <summary>
        /// Loop for the main listening thread.
        /// </summary>
        private void DoTcpListening()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Listening loop.
                    while (!myStopTcpListeningRequested)
                    {
                        // When the connection is established then handle it in another thread.
                        IAsyncResult anAsyncResult = myTcpListener.BeginAcceptTcpClient(HandleConnection, myTcpListener);

                        if (!myStopTcpListeningRequested)
                        {
                            // Wait for the connection.
                            anAsyncResult.AsyncWaitHandle.WaitOne();
                            anAsyncResult.AsyncWaitHandle.Close();
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.DoListeningFailure, err);
                }
            }
        }


        protected abstract void DisconnectClients();

        /// <summary>
        /// The method is called in a separate thread when the connection is established.
        /// </summary>
        /// <param name="asyncResult"></param>
        protected abstract void HandleConnection(IAsyncResult asyncResult);

        protected abstract void MessageHandler(object message);

        private TcpListener myTcpListener;
        private UriBuilder myUriBuilder;
        
        protected ISecurityFactory mySecurityStreamFactory;

        private Thread myTcpListeningThread;

        protected WorkingThread<object> myMessageProcessingThread;

        protected volatile bool myStopTcpListeningRequested;

        protected object myListeningManipulatorLock = new object();


        protected abstract string TracedObject { get; }


    }
}

#endif