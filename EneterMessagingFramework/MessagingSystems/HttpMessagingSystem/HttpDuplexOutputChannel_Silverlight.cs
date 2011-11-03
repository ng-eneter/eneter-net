/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


#if SILVERLIGHT

using System;
using System.IO;
using System.Net;
using System.Net.Browser;
using System.Threading;
using System.Windows;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;


namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{

    internal class HttpDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        /// <summary>
        /// Constructs Http duplex output channel.
        /// The Http duplex output channel can send messages and receive responses from Http duplex input channel.
        /// </summary>
        /// <param name="channelId">Uri address of Http duplex input channel.</param>
        /// <param name="responseReceiverId">response receiver identifier</param>
        /// <param name="pollingFrequencyMiliseconds">How often the pulling for response messages will occur.</param>
        /// <param name="isResponseReceivedInSilverlightThread">indicates if the response messages are notified in the silverlight thread</param>
        public HttpDuplexOutputChannel(string channelId, string responseReceiverId, int pollingFrequencyMiliseconds, bool isResponseReceivedInSilverlightThread)
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
                    myUriBuilder = new UriBuilder(channelId);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                // Specify, that the http/https requests connected to this output duplex channel will be handled by the client and not by the browser.
                // Note: If the requests are handled by the Browser, then the browser would "blink" the text message in its statusbar, that
                //       is connecting, retrieving data ... regularly for every poll request.
                try
                {
                    if (WebRequest.RegisterPrefix(myUriBuilder.Uri.AbsoluteUri, WebRequestCreator.ClientHttp) == false)
                    {
                        EneterTrace.Warning(TracedObject + "failed to register, that the web request " + myUriBuilder.Uri.OriginalString + " shall be handled by the client and not by the browser.");
                    }
                }
                catch (Exception err)
                {
                    // In case of the error, the requests will be handled by the Browser with the comsmetic side-efect - the user will
                    // see the communication status messages in the status bar of the browser.
                    EneterTrace.Warning(TracedObject + "failed to register, that the web request " + myUriBuilder.Uri.OriginalString + " shall be handled by the client and not by the browser.", err);
                }


                ChannelId = channelId;

                myPollingFrequency = pollingFrequencyMiliseconds;

                myResponseReceivedInSilverlightThreadFlag = isResponseReceivedInSilverlightThread;

                // Creates the working thread with the message queue for processing incoming response messages.
                myResponseMessageWorkingThread = new WorkingThread<object>(ChannelId);

                ResponseReceiverId = (string.IsNullOrEmpty(responseReceiverId)) ? channelId + "_" + Guid.NewGuid().ToString() : responseReceiverId;
            }
        }

        /// <summary>
        /// Returns the channel id. - the channel id is Uri.
        /// </summary>
        public string ChannelId { get; private set; }

        public string ResponseReceiverId { get; private set; }

        /// <summary>
        /// Starts pulling for response messages.
        /// </summary>
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

                    try
                    {
                        myStopHttpResponseListeningRequested = false;

                        // Register the method handling messages from the working with the queue.
                        // Note: Received responses are put to the message queue. This thread takes
                        //       messages from the queue and calls the registered method to process them.
                        myResponseMessageWorkingThread.RegisterMessageHandler(HandleResponseMessage);

                        // Start regular polling for messages.
                        myPollingWaitingEndedEvent.Reset();
                        myPollingThread = new Thread(DoPolling);
                        myPollingThread.Start();

                        // Send open connection message
                        object[] anOpenConnectionRequest = MessageStreamer.GetOpenConnectionMessage(ResponseReceiverId);
                        HttpRequestInvoker.InvokeNotInSilverlightThread(myUriBuilder.Uri, anOpenConnectionRequest);

                        // Notify, the connection is open.
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

        /// <summary>
        /// Stops pulling for response messages.
        /// </summary>
        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    // Indicate that the processing of response messages should stop.
                    // Note: Thread listening to response messages checks this flag and stops the looping.
                    myStopHttpResponseListeningRequested = true;
                    myPollingWaitingEndedEvent.Set();

                    // Try to notify the server that the connection is closed.
                    // Send open connection message
                    try
                    {
                        if (!string.IsNullOrEmpty(ResponseReceiverId))
                        {
                            object[] aCloseConnectionRequest = MessageStreamer.GetCloseConnectionMessage(ResponseReceiverId);
                            HttpRequestInvoker.InvokeNotInSilverlightThread(myUriBuilder.Uri, aCloseConnectionRequest);
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                    }

                    // Wait until the polling stops.
                    if (myPollingThread != null && myPollingThread.ThreadState != ThreadState.Unstarted)
                    {
                        if (!myPollingThread.Join(5000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myPollingThread.ManagedThreadId);

                            try
                            {
                                myPollingThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                            }
                        }

                        myPollingThread = null;
                    }

                    // Stop the thread processing polled message.
                    try
                    {
                        myResponseMessageWorkingThread.UnregisterMessageHandler();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to unregister the handler of response messages.", err.Message);
                    }
                }
            }
        }
    

        /// <summary>
        /// Returns true if the connection is open.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return myIsListeningToResponses;
                    }
                }
            }
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="message"></param>
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
                        object[] aMessageRequest = { (byte)2, ResponseReceiverId, message };
                        HttpRequestInvoker.InvokeNotInSilverlightThread(myUriBuilder.Uri, aMessageRequest);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                        throw;
                    }
                }
            }
        }

        private void DoPolling()
        {
            using (EneterTrace.Entering())
            {
                myIsListeningToResponses = true;

                try
                {
                    // While the polling shall loop.
                    while (!myStopHttpResponseListeningRequested)
                    {
                        // Send the polling request.
                        // Note: the polling request is just plain response receiver id.
                        // Note: Important is, that the following call does not run in the Silverlight thread. => it would frozen
                        //       due to a bug in Silverlight.
                        WebResponse aWebResponse = HttpRequestInvoker.InvokeWebRequest(myUriBuilder.Uri, ResponseReceiverId);

                        if (!myStopHttpResponseListeningRequested)
                        {
                            // This stream contains the list of polled messages.
                            Stream aResponseStream = aWebResponse.GetResponseStream();

                            // Buffer the stream
                            using (MemoryStream aMemStream = new MemoryStream())
                            {
                                int aSize = 0;
                                byte[] aBuffer = new byte[32768];
                                while ((aSize = aResponseStream.Read(aBuffer, 0, aBuffer.Length)) != 0)
                                {
                                    aMemStream.Write(aBuffer, 0, aSize);
                                }

                                // Read messages from the buffered stream
                                // Note: the duplex input channel can send more response messages at once.
                                aMemStream.Position = 0;
                                object aResponseMessage = null;
                                while (aMemStream.Position < aMemStream.Length &&
                                        (aResponseMessage = MessageStreamer.ReadMessage(aMemStream)) != null)
                                {
                                    // Put the message to the message queue from where it will be processed
                                    // by the working thread.
                                    myResponseMessageWorkingThread.EnqueueMessage(aResponseMessage);
                                }
                            }
                        }

                        // Wait the frequency pause.
                        // Note: AutoresetEvent allows to interrupt waiting.
                        myPollingWaitingEndedEvent.WaitOne(myPollingFrequency);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to poll the response messages from the duplex input channel. The listening is stoped.", err);
                    myIsListeningToResponses = false;
                }

                // Stop the thread processing polled message.
                try
                {
                    myResponseMessageWorkingThread.UnregisterMessageHandler();
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.UnregisterMessageHandlerThreadFailure, err);
                }

                myIsListeningToResponses = false;

                // Notify the listening to messages (polling) stoped.
                NotifyConnectionClosed();
            }
        }

        /// <summary>
        /// Handles response messages from the queue.
        /// </summary>
        /// <param name="message"></param>
        private void HandleResponseMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseMessageReceived != null)
                {
                    Action anInvokeResponseMessageReceived = () =>
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
                        };

                    if (myResponseReceivedInSilverlightThreadFlag)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(anInvokeResponseMessageReceived);
                    }
                    else
                    {
                        anInvokeResponseMessageReceived();
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


        private object myConnectionManipulatorLock = new object();

        private volatile bool myStopHttpResponseListeningRequested;
        private volatile bool myIsListeningToResponses;

        private WorkingThread<object> myResponseMessageWorkingThread;

        private UriBuilder myUriBuilder;

        private Thread myPollingThread;
        private int myPollingFrequency;
        private ManualResetEvent myPollingWaitingEndedEvent = new ManualResetEvent(false);

        private bool myResponseReceivedInSilverlightThreadFlag;


        private string TracedObject
        {
            get
            {
                string aChannelId = (ChannelId != null) ? ChannelId : "";
                return "Http duplex output channel '" + aChannelId + "' ";
            }
        }
    }

}

#endif
