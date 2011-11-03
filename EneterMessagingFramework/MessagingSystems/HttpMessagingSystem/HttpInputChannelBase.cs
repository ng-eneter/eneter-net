/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


#if !SILVERLIGHT

using System;
using System.Net;
using System.Threading;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// Base class for the http based input channel. It implements basic
    /// common functionality for HttpInputChannel and HttpDuplexInputChannel.
    /// </summary>
    internal abstract class HttpInputChannelBase
    {
        public HttpInputChannelBase(string channelId)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                try
                {
                    // just check if the channel id is a valid Uri
                    new UriBuilder(channelId);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(ChannelId + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                ChannelId = channelId;

                myMessageProcessingThread = new WorkingThread<object>(ChannelId);
            }
        }

        public string ChannelId { get; private set; }

        /// <summary>
        /// Starts Http listening.
        /// </summary>
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
                        // Set the listening flag so that the listening loop will work.
                        myStopHttpListeningRequested = false;

                        // Start the working thread for removing messages from the queue
                        myMessageProcessingThread.RegisterMessageHandler(MessageHandler);

                        // Start listening for for incoming Http requests
                        myHttpListener = new HttpListener();
                        UriBuilder aUriBuilder = new UriBuilder(ChannelId);
                        myHttpListener.Prefixes.Add(aUriBuilder.Uri.AbsoluteUri);
                        myHttpListener.Start();

                        // Do listening loop in another thread
                        myHttpListeningThread = new Thread(DoHttpListening);
                        myHttpListeningThread.Start();
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

        /// <summary>
        /// Stops the Http listening.
        /// </summary>
        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    myStopHttpListeningRequested = true;

                    if (myHttpListener != null)
                    {
                        try
                        {
                            myHttpListener.Stop();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                        }
                    }

                    if (myHttpListeningThread != null && myHttpListeningThread.ThreadState != ThreadState.Unstarted)
                    {
                        if (!myHttpListeningThread.Join(1000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myHttpListeningThread.ManagedThreadId.ToString());

                            try
                            {
                                myHttpListeningThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                            }
                        }
                    }
                    myHttpListeningThread = null;

                    // Listening thread is not active so we can close the listener.
                    if (myHttpListener != null)
                    {
                        try
                        {
                            myHttpListener.Close();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to close the Http listener.", err);
                        }

                        myHttpListener = null;
                    }

                    try
                    {
                        // Stop thread processing the queue with messages.
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
                        return myHttpListener != null;
                    }
                }
            }
        }

        /// <summary>
        /// Implements the loop responsible for the listening.
        /// </summary>
        private void DoHttpListening()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Listening loop
                    while (!myStopHttpListeningRequested)
                    {
                        // When the connection is established then handle it in another thread.
                        // Note: HandleConnection is the abstract method implemented in the derived class.
                        IAsyncResult anAsyncResult = myHttpListener.BeginGetContext(HandleConnection, myHttpListener);

                        if (!myStopHttpListeningRequested)
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

        /// <summary>
        /// Processes the Http requests.
        /// It gets the message and puts it to the queue. The message is then removed from the queue by
        /// the working thread that notifies the subscriber.
        /// </summary>
        /// <param name="asyncResult"></param>
        protected abstract void HandleConnection(IAsyncResult asyncResult);


        /// <summary>
        /// The method is called from the working thread when a message shall be processed.
        /// Messages comming from from diffrent receiving threads are put to the queue where the working
        /// thread removes them one by one and notify the subscribers on the input channel.
        /// Therefore the channel notifies always in one thread.
        /// </summary>
        protected abstract void MessageHandler(object message);

        private HttpListener myHttpListener;

        private Thread myHttpListeningThread;

        protected WorkingThread<object> myMessageProcessingThread;

        protected volatile bool myStopHttpListeningRequested;

        protected object myListeningManipulatorLock = new object();


        protected abstract string TracedObject { get; }
    }
}

#endif