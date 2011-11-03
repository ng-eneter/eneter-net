/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Timers;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;




namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// Implements the duplex input channel based on Http.
    /// </summary>
    internal class HttpDuplexInputChannel : HttpInputChannelBase, IDuplexInputChannel
    {
        private class TResponseReceiver
        {
            public enum EConnctionState
            {
                Open,
                Close
            }

            public TResponseReceiver(string responseReceiverId, DateTime creationTime)
            {
                ResponseReceiverId = responseReceiverId;
                LastPollingActivityTime = creationTime;
                ConnectionState = EConnctionState.Open;
                Messages = new Queue<object>();
            }

            public string ResponseReceiverId { get; private set; }
            public EConnctionState ConnectionState { get; set; }
            public DateTime LastPollingActivityTime { get; set; }
            public Queue<object> Messages { get; private set; }
        }

        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;
        
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Constructs the duplex input channel.
        /// </summary>
        /// <param name="channelId">Uri of the duplex input channel.</param>
        /// <param name="responseReceiverInactivityTimeout">maximum time the Http response receiver does not poll for response messages</param>
        public HttpDuplexInputChannel(string channelId, int responseReceiverInactivityTimeout)
            : base(channelId)
        {
            using (EneterTrace.Entering())
            {
                // Initialize the timer to regularly check the timeout for connections with duplex output channels.
                // If the duplex output channel did not pull within the timeout then the connection
                // is closed and removed from the list.
                // Note: The timer is set here but not executed.
                myResponseReceiverInactivityTimer = new Timer(responseReceiverInactivityTimeout);
                myResponseReceiverInactivityTimer.AutoReset = false;
                myResponseReceiverInactivityTimer.Elapsed += OnConnectionCheckTimer;

                myResponseReceiverInactivityTimeout = responseReceiverInactivityTimeout;
            }
        }

        /// <summary>
        /// Sends the response message to the specified response receiver.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="message"></param>
        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseMessages)
                {
                    TResponseReceiver aResponsesForParticularReceiver = myResponseMessages.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);
                    if (aResponsesForParticularReceiver != null && aResponsesForParticularReceiver.ConnectionState == TResponseReceiver.EConnctionState.Open)
                    {
                        aResponsesForParticularReceiver.Messages.Enqueue(message);
                    }
                    else
                    {
                        string aMessage = TracedObject + ErrorHandler.SendResponseNotConnectedFailure;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Disconnects the response receiver.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseMessages)
                {
                    TResponseReceiver aResponsesForParticularReceiver = myResponseMessages.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);
                    if (aResponsesForParticularReceiver != null)
                    {
                        // Set the status, that the connection is closed.
                        aResponsesForParticularReceiver.ConnectionState = TResponseReceiver.EConnctionState.Close;

                        // Send the response message to the duplex output channel, that the duplx output channel was disconnected.
                        object aCloseConnectionMessage = MessageStreamer.GetCloseConnectionMessage(responseReceiverId);
                        aResponsesForParticularReceiver.Messages.Enqueue(aCloseConnectionMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Is called when the Http connection is established. It is called from DoHttpListening().
        /// </summary>
        /// <param name="asyncResult"></param>
        protected override void HandleConnection(IAsyncResult asyncResult)
        {
            using (EneterTrace.Entering())
            {
                HttpListener anHttpListener = (HttpListener)asyncResult.AsyncState;

                try
                {
                    HttpListenerContext aListenerContext = anHttpListener.EndGetContext(asyncResult);

                    try
                    {
                        // If there is not a request to stop the listening.
                        if (!myStopHttpListeningRequested)
                        {
                            Stream anInputStream = aListenerContext.Request.InputStream;

                            // First read the message to the buffer.
                            object aMessage = null;
                            using (MemoryStream aMemStream = new MemoryStream())
                            {
                                int aSize = 0;
                                byte[] aBuffer = new byte[32768];
                                while ((aSize = anInputStream.Read(aBuffer, 0, aBuffer.Length)) != 0)
                                {
                                    aMemStream.Write(aBuffer, 0, aSize);
                                }

                                // Read the message from the buffer.
                                aMemStream.Position = 0;
                                aMessage = MessageStreamer.ReadMessage(aMemStream);
                            }

                            if (aMessage != null)
                            {
                                // If the request is to send a message.
                                if (aMessage is object[])
                                {
                                    // If the message is to notify the incoming message.
                                    if (MessageStreamer.IsRequestMessage(aMessage))
                                    {
                                        object[] aMessageStruct = (object[])aMessage;
                                        string aResponseReceiverId = aMessageStruct[1] as string;

                                        lock (myResponseMessages)
                                        {
                                            // If the sending duplex output channel is connected then process the message.
                                            // Otherwise return with an error.
                                            TResponseReceiver aResponseReceiver = myResponseMessages.FirstOrDefault(x => x.ResponseReceiverId == aResponseReceiverId);
                                            if (aResponseReceiver != null && aResponseReceiver.ConnectionState == TResponseReceiver.EConnctionState.Open)
                                            {
                                                myMessageProcessingThread.EnqueueMessage(aMessage);
                                            }
                                            else
                                            {
                                                // Response back the error.
                                                aListenerContext.Response.StatusCode = 404;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Put the message to the queue from where the working thread removes it to notify
                                        // subscribers of the input channel.
                                        // Note: therfore subscribers of the input channel are notified allways in one thread.
                                        myMessageProcessingThread.EnqueueMessage(aMessage);
                                    }
                                }
                                // If the request is the polling for response messages.
                                else if (aMessage is string)
                                {
                                    try
                                    {
                                        // Get response receiver id
                                        string aResponseReceiverId = (string)aMessage;

                                        lock (myResponseMessages)
                                        {
                                            // Get messages collected for the response receiver.
                                            TResponseReceiver aResponsesForParticularReceiver = myResponseMessages.FirstOrDefault(x => x.ResponseReceiverId == aResponseReceiverId);
                                            if (aResponsesForParticularReceiver != null)
                                            {
                                                // Update the polling time.
                                                aResponsesForParticularReceiver.LastPollingActivityTime = DateTime.Now;

                                                // If there are stored messages for the receiver
                                                if (aResponsesForParticularReceiver.Messages.Count > 0)
                                                {
                                                    using (MemoryStream aStreamedResponses = new MemoryStream())
                                                    {
                                                        // Dequeue responses to be sent to the response receiver.
                                                        // Note: Try not to exceed 1MB - better do more small transfers
                                                        while (aResponsesForParticularReceiver.Messages.Count > 0 && aStreamedResponses.Length < 1048576)
                                                        {
                                                            object aResponse = aResponsesForParticularReceiver.Messages.Dequeue();
                                                            MessageStreamer.WriteMessage(aStreamedResponses, aResponse);
                                                        }

                                                        // Put the memory stream to the output stream.
                                                        Stream anOutputStream = aListenerContext.Response.OutputStream;
                                                        aStreamedResponses.WriteTo(anOutputStream);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);
                                    }
                                }
                            }
                            else
                            {
                                EneterTrace.Warning(TracedObject + "received a null message.");
                            }
                        }
                    }
                    finally
                    {
                        aListenerContext.Response.Close();
                        aListenerContext.Request.InputStream.Close();
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.ProcessingHttpConnectionFailure, err);
                }
            }
        }

        /// <summary>
        /// Processes messages from the queue.
        /// </summary>
        /// <param name="message"></param>
        protected override void MessageHandler(object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object[] aMessage = (object[])message;

                    // Open connection request.
                    if (MessageStreamer.IsOpenConnectionMessage(message))
                    {
                        OpenConnectionIfNeeded((string)aMessage[1]);
                        NotifyResponseReceiverConnected((string)aMessage[1]);
                    }
                    // Close connection request.
                    else if (MessageStreamer.IsCloseConnectionMessage(message))
                    {
                        int aNumberOfRemovedResponseReceivers;

                        lock (myResponseMessages)
                        {
                            // Delete the response receiver and its context.
                            aNumberOfRemovedResponseReceivers = myResponseMessages.RemoveWhere(x => x.ResponseReceiverId == (string)aMessage[1]);
                        }

                        if (aNumberOfRemovedResponseReceivers > 0)
                        {
                            NotifyResponseReceiverDisconnected((string)aMessage[1]);
                        }
                    }
                    // Notify the incoming message request.
                    else if (MessageStreamer.IsRequestMessage(message))
                    {
                        NotifyMessageReceived(ChannelId, aMessage[2], (string)aMessage[1]);
                    }
                    else
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.ReceiveMessageIncorrectFormatFailure);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.ReceiveMessageFailure, err);
                }
            }
        }

        private void OpenConnectionIfNeeded(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseMessages)
                {
                    TResponseReceiver aResponseReceiver = myResponseMessages.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);

                    // If the response receiver is not registered yet.
                    // E.g. because 'open connection' message came somehow after this message.
                    // "Open" connection for the response receiver. -> store response receiver id.
                    if (aResponseReceiver == null)
                    {
                        aResponseReceiver = new TResponseReceiver(responseReceiverId, DateTime.Now);
                        myResponseMessages.Add(aResponseReceiver);
                        myResponseReceiverInactivityTimer.Enabled = true;
                    }

                    aResponseReceiver.ConnectionState = TResponseReceiver.EConnctionState.Open;
                }
            }
        }


        private void NotifyResponseReceiverConnected(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverConnected != null)
                {
                    ResponseReceiverEventArgs aResponseReceiverEvent = new ResponseReceiverEventArgs(responseReceiverId);

                    try
                    {
                        ResponseReceiverConnected(this, aResponseReceiverEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void NotifyResponseReceiverDisconnected(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverDisconnected != null)
                {
                    ResponseReceiverEventArgs aResponseReceiverEvent = new ResponseReceiverEventArgs(responseReceiverId);

                    try
                    {
                        ResponseReceiverDisconnected(this, aResponseReceiverEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void NotifyMessageReceived(string channelId, object message, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    try
                    {
                        MessageReceived(this, new DuplexChannelMessageEventArgs(channelId, message, responseReceiverId));
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


        private void OnConnectionCheckTimer(object sender, ElapsedEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseMessages)
                {
                    DateTime aTime = DateTime.Now;

                    // Check the connection for each connected duplex output channel.
                    foreach (TResponseReceiver aResponseReceiver in myResponseMessages)
                    {
                        // If the last polling activity time exceeded the maximum allowed time, then close connection.
                        if (aTime - aResponseReceiver.LastPollingActivityTime > TimeSpan.FromMilliseconds(myResponseReceiverInactivityTimeout))
                        {
                            // Mark the connection as closed.
                            aResponseReceiver.ConnectionState = TResponseReceiver.EConnctionState.Close;

                            // Put the message to the queue message queue to notify that the duplex output channel is disconnected.
                            object[] aCloseConnectionMessage = MessageStreamer.GetCloseConnectionMessage(aResponseReceiver.ResponseReceiverId);
                            myMessageProcessingThread.EnqueueMessage(aCloseConnectionMessage);
                        }
                    }

                    if (myResponseMessages.Count > 0)
                    {
                        myResponseReceiverInactivityTimer.Enabled = true;
                    }
                }
            }
        }


        private HashSet<TResponseReceiver> myResponseMessages = new HashSet<TResponseReceiver>();

        private int myResponseReceiverInactivityTimeout;

        private Timer myResponseReceiverInactivityTimer;

        protected override string TracedObject
        {
            get 
            {
                return "Http duplex input channel '" + ChannelId + "' "; 
            }
        }
    }
}

#endif