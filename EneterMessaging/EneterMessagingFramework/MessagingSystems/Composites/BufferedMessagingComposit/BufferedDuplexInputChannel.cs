/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class BufferedDuplexInputChannel : IDuplexInputChannel
    {
        private class TBufferedResponseReceiver
        {
            public TBufferedResponseReceiver(string responseReceiverId, string clientAddress, IDuplexInputChannel duplexInputChannel)
            {
                ResponseReceiverId = responseReceiverId;
                ClientAddress = clientAddress;
                myDuplexInputChannel = duplexInputChannel;
                IsResponseReceiverConnected = false;
            }

            public bool IsResponseReceiverConnected
            {
                get
                {
                    return myIsResponseReceiverConnected;
                }

                set
                {
                    myIsResponseReceiverConnected = value;

                    if (!myIsResponseReceiverConnected)
                    {
                        DisconnectionStartedAt = DateTime.Now;
                    }
                }
            }

            public void SendResponseMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    if (IsResponseReceiverConnected)
                    {
                        try
                        {
                            myDuplexInputChannel.SendResponseMessage(ResponseReceiverId, message);
                        }
                        catch
                        {
                            // Sending failed because of disconnection. So enqueue the message and wait if the connection is recovered.
                            EnqueueMessage(message);
                        }
                    }
                    else
                    {
                        EnqueueMessage(message);
                    }
                }
            }

            public void CloseResponseReceiver()
            {
                using (EneterTrace.Entering())
                {
                    // Indicate to the running thread, that the sending shall stop.
                    myMessageQueueRequestedToStopFlag = true;

                    // Wait until the thread is stopped.
                    if (!myMessageQueueEndedEvent.WaitOne(5000))
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.FailedToStopThreadId);
                    }

                    lock (myMessageQueue)
                    {
                        // Remove all messages.
                        myMessageQueue.Clear();
                    }

                    myDuplexInputChannel.DisconnectResponseReceiver(ResponseReceiverId);
                }
            }

            public DateTime DisconnectionStartedAt { get; private set; }

            public string ResponseReceiverId { get; private set; }
            public string ClientAddress { get; set; }

            private void EnqueueMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    lock (myMessageQueue)
                    {
                        // If the thread sending messages from the queue is not running, then invoke one.
                        if (!myMessageQueueActiveFlag)
                        {
                            myMessageQueueActiveFlag = true;
                            ThreadPool.QueueUserWorkItem(DoResponseMessageSending);
                        }

                        myMessageQueue.Enqueue(message);
                    }
                }
            }

            private void DoResponseMessageSending(object x)
            {
                using (EneterTrace.Entering())
                {
                    myMessageQueueEndedEvent.Reset();

                    try
                    {
                        // Loop taking messages from the queue, until the queue is empty or there is a request to stop sending.
                        while (!myMessageQueueRequestedToStopFlag)
                        {
                            object aMessage;

                            lock (myMessageQueue)
                            {
                                if (myMessageQueue.Count == 0)
                                {
                                    return;
                                }

                                aMessage = myMessageQueue.Peek();
                            }

                            while (!myMessageQueueRequestedToStopFlag)
                            {
                                // Try to send the message.
                                try
                                {
                                    // Send the message using the underlying output channel.
                                    myDuplexInputChannel.SendResponseMessage(ResponseReceiverId, aMessage);

                                    // The message was successfuly sent, therefore it can be removed from the queue.
                                    lock (myMessageQueue)
                                    {
                                        myMessageQueue.Dequeue();
                                    }

                                    break;
                                }
                                catch
                                {
                                    // The receiver is not available. Therefore try again if not timeout.
                                }


                                // If sending thread is not asked to stop.
                                if (!myMessageQueueRequestedToStopFlag)
                                {
                                    Thread.Sleep(300);
                                }
                            }
                        }
                    }
                    finally
                    {
                        myMessageQueueActiveFlag = false;
                        myMessageQueueEndedEvent.Set();
                    }
                }
            }


            private IDuplexInputChannel myDuplexInputChannel;

            private bool myMessageQueueActiveFlag;
            private bool myMessageQueueRequestedToStopFlag;
            private ManualResetEvent myMessageQueueEndedEvent = new ManualResetEvent(true);
            private Queue<object> myMessageQueue = new Queue<object>();

            private volatile bool myIsResponseReceiverConnected;
            private object myResponseReceiverManipulatorLock = new object();

            private string TracedObject
            {
                get
                {
                    return GetType().Name + " '" + ResponseReceiverId + "' ";
                }
            }
        }


        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;


        public BufferedDuplexInputChannel(IDuplexInputChannel underlyingDuplexInputChannel, TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingInputChannel = underlyingDuplexInputChannel;
                myMaxOfflineTime = maxOfflineTime;

                myMaxOfflineChecker = new Timer(OnMaxOfflineTimeCheckTick, null, -1, -1);
            }
        }


        public string ChannelId { get { return myUnderlyingInputChannel.ChannelId; } }

        public IThreadDispatcher Dispatcher { get { return myUnderlyingInputChannel.Dispatcher; } }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    myUnderlyingInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
                    myUnderlyingInputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                    myUnderlyingInputChannel.MessageReceived += OnMessageReceived;

                    try
                    {
                        myUnderlyingInputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        myUnderlyingInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                        myUnderlyingInputChannel.ResponseReceiverDisconnected -= OnResponseReceiverDisconnected;
                        myUnderlyingInputChannel.MessageReceived -= OnMessageReceived;

                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);
                        throw;
                    }

                    myMaxOfflineCheckerRequestedToStop = false;
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    // Indicate, that the timer responsible for checking if response receivers are timeouted (i.e. exceeded the max offline time)
                    // shall stop.
                    myMaxOfflineCheckerRequestedToStop = true;

                    lock (myResponseReceivers)
                    {
                        // Stop buffering for all connected response receivers.
                        foreach (TBufferedResponseReceiver aResponseReceiverContext in myResponseReceivers)
                        {
                            aResponseReceiverContext.CloseResponseReceiver();
                        }

                        myResponseReceivers.Clear();
                    }

                    try
                    {
                        myUnderlyingInputChannel.StopListening();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.IncorrectlyStoppedListening, err);
                    }

                    myUnderlyingInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                    myUnderlyingInputChannel.ResponseReceiverDisconnected -= OnResponseReceiverDisconnected;
                    myUnderlyingInputChannel.MessageReceived -= OnMessageReceived;
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
                        return myUnderlyingInputChannel.IsListening;
                    }
                }
            }
        }

        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                TBufferedResponseReceiver aResponseReceiverContext;

                lock (myResponseReceivers)
                {
                    aResponseReceiverContext = myResponseReceivers.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);

                    // If the response receiver was not found, then it means, that the response receiver is not connected.
                    // In order to support independent startup order, we can suppose, the response receiver can connect later
                    // and the response messages will be then delivered.
                    // If not, then response messages will be deleted automatically after the timeout (maxOfflineTime).
                    if (aResponseReceiverContext == null)
                    {
                        // Create the response receiver context - it allows to enqueue response messages before connection of
                        // the response receiver.
                        // Note: The client address is not known yet.
                        aResponseReceiverContext = new TBufferedResponseReceiver(responseReceiverId, "", myUnderlyingInputChannel);
                        myResponseReceivers.Add(aResponseReceiverContext);

                        // If it is the first response receiver, then start the timer checking which response receivers
                        // are disconnected due to the timeout (i.e. max offline time)
                        if (myResponseReceivers.Count == 1)
                        {
                            myMaxOfflineChecker.Change(300, -1);
                        }
                    }
                }

                // Enqueue the message.
                aResponseReceiverContext.SendResponseMessage(message);
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                TBufferedResponseReceiver aResponseReceiverContext;

                lock (myResponseReceivers)
                {
                    aResponseReceiverContext = myResponseReceivers.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);

                    if (aResponseReceiverContext != null)
                    {
                        myResponseReceivers.Remove(aResponseReceiverContext);
                    }
                }

                if (aResponseReceiverContext != null)
                {
                    // Stop the buffer queue.
                    aResponseReceiverContext.CloseResponseReceiver();
                }
            }
        }

        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Update that the response receiver is connected.
                // If the response receiver does not exist, then create it.
                UpdateResponseReceiverContext(e.ResponseReceiverId, e.SenderAddress, true, true);

                Notify<ResponseReceiverEventArgs>(ResponseReceiverConnected, e, false);
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Update that the response receiver is disconnected.
                UpdateResponseReceiverContext(e.ResponseReceiverId, e.SenderAddress, false, false);
            }
        }

        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Note: this method is called from the underlying channel. Therefore it is called in the correct thread.
                Notify<DuplexChannelMessageEventArgs>(MessageReceived, e, true);
            }
        }


        private void UpdateResponseReceiverContext(string responseReceiverId, string clientAddress,
            bool isConnected,
            bool createNewIfDoesNotExistFlag)
        {
            using (EneterTrace.Entering())
            {
                TBufferedResponseReceiver aResponseReceiverContext;

                lock (myResponseReceivers)
                {
                    aResponseReceiverContext = myResponseReceivers.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);

                    // If the response receiver was not found, then it means, that the response receiver is not connected.
                    // In order to support independent startup order, we can suppose, the response receiver can connect later
                    // and the response messages will be then delivered.
                    // If not, then response messages will be deleted automatically after the timeout (maxOfflineTime).
                    if (aResponseReceiverContext == null && createNewIfDoesNotExistFlag)
                    {
                        // Create the response receiver context - it allows to enqueue response messages before connection of
                        // the response receiver.
                        aResponseReceiverContext = new TBufferedResponseReceiver(responseReceiverId, clientAddress, myUnderlyingInputChannel);
                        myResponseReceivers.Add(aResponseReceiverContext);

                        // If it is the first response receiver, then start the timer checking which response receivers
                        // are disconnected due to the timeout (i.e. max offline time)
                        if (myResponseReceivers.Count == 1)
                        {
                            myMaxOfflineChecker.Change(300, -1);
                        }

                        return;
                    }
                }

                // Update the connection status.
                aResponseReceiverContext.IsResponseReceiverConnected = isConnected;

                // In case the client was connected after the response message was sent the client address is not set.
                // Therefore try to set it now too.
                aResponseReceiverContext.ClientAddress = clientAddress;
            }
        }

        private void OnMaxOfflineTimeCheckTick(object o)
        {
            using (EneterTrace.Entering())
            {
                // Do nothing if there is a request to stop.
                if (myMaxOfflineCheckerRequestedToStop)
                {
                    return;
                }

                List<TBufferedResponseReceiver> aTimeoutedResponseReceivers = new List<TBufferedResponseReceiver>();

                DateTime aCurrentCheckTime = DateTime.Now;
                bool aTimerShallContinueFlag;

                lock (myResponseReceivers)
                {
                    // Remove all not connected response receivers which exceeded the max offline timeout.
                    myResponseReceivers.RemoveWhere(x =>
                        {
                            // If disconnected and max offline time is exceeded. 
                            if (!x.IsResponseReceiverConnected &&
                                aCurrentCheckTime - x.DisconnectionStartedAt > myMaxOfflineTime)
                            {
                                aTimeoutedResponseReceivers.Add(x);

                                // Indicate, the response receiver can be removed.
                                return true;
                            }

                            // Response receiver will not be removed.
                            return false;
                        });

                    aTimerShallContinueFlag = myResponseReceivers.Count > 0;
                }

                // Notify disconnected response receivers.
                foreach (TBufferedResponseReceiver aResponseReceiverContext in aTimeoutedResponseReceivers)
                {
                    // Stop disconnecting if the we are requested to stop.
                    if (myMaxOfflineCheckerRequestedToStop)
                    {
                        return;
                    }

                    aResponseReceiverContext.CloseResponseReceiver();

                    // Invoke the event in the correct thread.
                    Dispatcher.Invoke(() => Notify<ResponseReceiverEventArgs>(ResponseReceiverDisconnected, new ResponseReceiverEventArgs(aResponseReceiverContext.ResponseReceiverId, aResponseReceiverContext.ClientAddress), false));
                }

                // If the timer checking the timeout for response receivers shall continue
                if (!myMaxOfflineCheckerRequestedToStop && aTimerShallContinueFlag)
                {
                    myMaxOfflineChecker.Change(300, -1);
                }
            }
        }

        private void Notify<T>(EventHandler<T> handler, T eventArgs, bool isNobodySubscribedWarning)
            where T : EventArgs
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, eventArgs);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else if (isNobodySubscribedWarning)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }


        private object myListeningManipulatorLock = new object();
        
        private TimeSpan myMaxOfflineTime;
        private Timer myMaxOfflineChecker;
        private bool myMaxOfflineCheckerRequestedToStop;
        private IDuplexInputChannel myUnderlyingInputChannel;

        private HashSet<TBufferedResponseReceiver> myResponseReceivers = new HashSet<TBufferedResponseReceiver>();


        private string TracedObject { get { return GetType().Name + " '" + ChannelId + "' "; } }
    }
}
