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
using System.Threading;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;


namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// Implements the duplex output channel based on Http.
    /// </summary>
    internal class HttpDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        public HttpDuplexOutputChannel(string channelId, string responseReceiverId, int pullingFrequencyMiliseconds)
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


                ChannelId = channelId;
                myPollingFrequencyMiliseconds = pullingFrequencyMiliseconds;

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

                    // If it is needed clean after the previous connection.
                    if (myWebClientForPollingResponseMessages != null)
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
                        myStopHttpResponseListeningRequested = false;

                        // Register the method handling messages from the working with the queue.
                        // Note: Received responses are put to the message queue. This thread takes
                        //       messages from the queue and calls the registered method to process them.
                        myResponseMessageWorkingThread.RegisterMessageHandler(HandleResponseMessage);

                        myWebClientForPollingResponseMessages = new WebClient();

                        // Create thread responsible for the loop listening to response messages coming from
                        // the Http duplex input channel.
                        myStopPollingWaitingEvent.Reset();
                        myResponseListener = new Thread(DoPolling);
                        myResponseListener.Start();

                        // Send open connection message
                        using (WebClient aWebClient = new WebClient())
                        {
                            using (MemoryStream aMemoryStream = new MemoryStream())
                            {
                                MessageStreamer.WriteOpenConnectionMessage(aMemoryStream, ResponseReceiverId);
                                aWebClient.UploadData(myUriBuilder.Uri, aMemoryStream.ToArray());
                            }
                        }

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
                    myStopPollingWaitingEvent.Set();

                    // Try to notify the server that the connection is closed.
                    try
                    {
                        if (!string.IsNullOrEmpty(ResponseReceiverId))
                        {
                            using (WebClient aWebClient = new WebClient())
                            {
                                using (MemoryStream aMemoryStream = new MemoryStream())
                                {
                                    MessageStreamer.WriteCloseConnectionMessage(aMemoryStream, ResponseReceiverId);
                                    aWebClient.UploadData(myUriBuilder.Uri, aMemoryStream.ToArray());
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                    }

                    // Close the webclient.
                    if (myWebClientForPollingResponseMessages != null)
                    {
                        try
                        {
                            myWebClientForPollingResponseMessages.Dispose();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to dispose the web client.", err);
                        }

                        myWebClientForPollingResponseMessages = null;
                    }

                    // Wait until the polling stops.
                    if (myResponseListener != null && myResponseListener.ThreadState != ThreadState.Unstarted)
                    {
                        if (!myResponseListener.Join(5000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myResponseListener.ManagedThreadId);

                            try
                            {
                                myResponseListener.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                            }
                        }
                    }
                    myResponseListener = null;

                    // Stop the thread processing polled message.
                    try
                    {
                        myResponseMessageWorkingThread.UnregisterMessageHandler();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.UnregisterMessageHandlerThreadFailure, err);
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
                        return myWebClientForPollingResponseMessages != null && myIsListeningToResponses;
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
                        using (WebClient aWebClient = new WebClient())
                        {
                            using (MemoryStream aMemoryStream = new MemoryStream())
                            {
                                MessageStreamer.WriteRequestMessage(aMemoryStream, ResponseReceiverId, message);
                                byte[] aData = aMemoryStream.ToArray();
                                aWebClient.UploadData(myUriBuilder.Uri, aData);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Implements the loop receiving response messages.
        /// The receiving the response messages is implemented as pulling on a regular basis according to the
        /// frequency specified in the constructor.
        /// </summary>
        private void DoPolling()
        {
            using (EneterTrace.Entering())
            {
                myIsListeningToResponses = true;

                try
                {
                    while (!myStopHttpResponseListeningRequested)
                    {
                        byte[] aWrapperResponseMessage = null;

                        // Crete a message used for pulling messages from the duplex input channel.
                        using (MemoryStream aMemoryStream = new MemoryStream())
                        {
                            MessageStreamer.WriteMessage(aMemoryStream, ResponseReceiverId);

                            // Get the response.
                            aWrapperResponseMessage = myWebClientForPollingResponseMessages.UploadData(myUriBuilder.Uri, aMemoryStream.ToArray());
                        }

                        // If some response is there (and response listening is not stopped)
                        // deserialize the message.
                        if (!myStopHttpResponseListeningRequested && aWrapperResponseMessage != null && aWrapperResponseMessage.Length > 0)
                        {
                            // Convert the response to the stream
                            using (MemoryStream aStreamedResponse = new MemoryStream(aWrapperResponseMessage))
                            {
                                // The response can contain more messages.
                                // Therefore let's read all messages.
                                object aResponseMessage = null;
                                while (aStreamedResponse.Position < aStreamedResponse.Length &&
                                       (aResponseMessage = MessageStreamer.ReadMessage(aStreamedResponse)) != null)
                                {
                                    // Put the message to the message queue from where it will be processed
                                    // by the working thread.
                                    myResponseMessageWorkingThread.EnqueueMessage(aResponseMessage);
                                }
                            }
                        }

                        myStopPollingWaitingEvent.WaitOne(myPollingFrequencyMiliseconds);
                    }
                }
                catch (WebException err)
                {
                    string anErrorStatus = err.Status.ToString();
                    EneterTrace.Error(TracedObject + "detected an error during listening to response messages. Error status: '" + anErrorStatus + "'. ", err);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.DoListeningFailure, err);
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

                // Notify the listening to messages stoped.
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
                if (MessageStreamer.IsCloseConnectionMessage(message))
                {
                    // Close connection with the duplex input channel.
                    // Note: The Close() must be called from the different thread because
                    //       it will try to stop this thread (thread processing messages).
                    WaitCallback aConnectionClosing = x => CloseConnection();
                    ThreadPool.QueueUserWorkItem(aConnectionClosing);
                }
                else if (ResponseMessageReceived != null)
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



        private WebClient myWebClientForPollingResponseMessages;

        private object myConnectionManipulatorLock = new object();

        private Thread myResponseListener;
        private volatile bool myStopHttpResponseListeningRequested;
        private volatile bool myIsListeningToResponses;

        private WorkingThread<object> myResponseMessageWorkingThread;

        private UriBuilder myUriBuilder;

        private int myPollingFrequencyMiliseconds;
        private ManualResetEvent myStopPollingWaitingEvent = new ManualResetEvent(false);



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

