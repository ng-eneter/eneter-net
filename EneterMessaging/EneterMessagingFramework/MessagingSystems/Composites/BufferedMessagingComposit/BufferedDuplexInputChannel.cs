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
            public TBufferedResponseReceiver(string responseReceiverId, IDuplexInputChannel duplexInputChannel)
            {
                ResponseReceiverId = responseReceiverId;

                // Note: at the time of instantiation the client address does not have to be knonwn.
                //       E.g. if sending response to a not yet connected response receiver.
                //       Therefore it will be set explicitly.
                ClientAddress = "";
                myDuplexInputChannel = duplexInputChannel;
                IsOnline = false;
            }

            public bool IsOnline
            {
                get
                {
                    return myIsOnline;
                }

                set
                {
                    if (value != myIsOnline)
                    {
                        myIsOnline = value;

                        if (!myIsOnline)
                        {
                            OfflineStartedAt = DateTime.Now;
                        }
                    }
                }
            }

            public bool ResponseReceiverConnectedEventPending { get; set; }

            public void SendResponseMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    myMessageQueue.Enqueue(message);
                    SendMessagesFromQueue();
                }
            }

            public void SendMessagesFromQueue()
            {
                using (EneterTrace.Entering())
                {
                    if (IsOnline)
                    {
                        while (myMessageQueue.Count > 0)
                        {
                            object aMessage = myMessageQueue.Peek();

                            try
                            {
                                myDuplexInputChannel.SendResponseMessage(ResponseReceiverId, aMessage);

                                // Message was successfuly sent therefore it can be removed from the queue.
                                myMessageQueue.Dequeue();
                            }
                            catch
                            {
                                // Sending failed because of disconnection.
                                IsOnline = false;
                                break;
                            }
                        }
                    }
                }
            }

            public DateTime OfflineStartedAt { get; private set; }

            public string ResponseReceiverId { get; private set; }
            public string ClientAddress { get; set; }

            public object ManipulatorLock { get { return myManipulatorLock; } }

            private object myManipulatorLock = new object();
            private IDuplexInputChannel myDuplexInputChannel;
            private Queue<object> myMessageQueue = new Queue<object>();
            private volatile bool myIsOnline;
        }

        private class TBroadcast
        {
            public TBroadcast(object message)
            {
                Message = message;
                SentAt = DateTime.Now;
            }
            public DateTime SentAt { get; private set; }
            public object Message { get; private set; }
        }

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverRecovered;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverInterrupted;


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
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);
                        StopListening();
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
                // If it is a broadcast response message.
                if (responseReceiverId == "*")
                {
                    lock (myResponseReceivers)
                    {
                        foreach (TBufferedResponseReceiver aResponseReceiver in myResponseReceivers)
                        {
                            lock (aResponseReceiver.ManipulatorLock)
                            {
                                aResponseReceiver.SendResponseMessage(message);
                            }
                        }
                    }
                }
                else
                {
                    TBufferedResponseReceiver aResponseReciever;
                    lock (myResponseReceivers)
                    {
                        aResponseReciever = GetResponseReceiver(responseReceiverId);
                        if (aResponseReciever == null)
                        {
                            aResponseReciever = CreateResponseReceiver(responseReceiverId, "", true);
                        }
                    }

                    lock (aResponseReciever.ManipulatorLock)
                    {
                        aResponseReciever.SendResponseMessage(message);
                    }

                    ResponseReceiverEventArgs anEvent = new ResponseReceiverEventArgs(responseReceiverId, "");
                    Dispatcher.Invoke(() => Notify(ResponseReceiverInterrupted, anEvent, false));
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseReceivers)
                {
                    myResponseReceivers.RemoveWhere(x => x.ResponseReceiverId == responseReceiverId);
                }

                myUnderlyingInputChannel.DisconnectResponseReceiver(responseReceiverId);
            }
        }

        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                bool aNewResponseReceiver = false;
                TBufferedResponseReceiver aResponseReciever;
                lock (myResponseReceivers)
                {
                    aResponseReciever = GetResponseReceiver(e.ResponseReceiverId);
                    if (aResponseReciever == null)
                    {
                        aResponseReciever = CreateResponseReceiver(e.ResponseReceiverId, e.SenderAddress, false);
                        aNewResponseReceiver = true;
                    }
                }

                bool aResponseReceicerConnectedEventFlag = false;
                lock (aResponseReciever.ManipulatorLock)
                {
                    aResponseReciever.IsOnline = true;

                    if (aResponseReciever.ResponseReceiverConnectedEventPending)
                    {
                        aResponseReciever.ClientAddress = e.SenderAddress;
                        aResponseReceicerConnectedEventFlag = aResponseReciever.ResponseReceiverConnectedEventPending;
                        aResponseReciever.ResponseReceiverConnectedEventPending = false;
                    }

                    if (aNewResponseReceiver)
                    {
                        // This is a fresh new response receiver. Therefore broadcast messages were not sent to it yet.
                        lock (myBroadcasts)
                        {
                            foreach (TBroadcast aBroadcastMessage in myBroadcasts)
                            {
                                aResponseReciever.SendResponseMessage(aBroadcastMessage);
                            }
                        }
                    }
                    else
                    {
                        // This is a reconnected response receiver. Therefore all meessages including broadcasts are already in the queue.
                        aResponseReciever.SendMessagesFromQueue();
                    }
                }

                if (aNewResponseReceiver || aResponseReceicerConnectedEventFlag)
                {
                    Dispatcher.Invoke(() => Notify(ResponseReceiverConnected, e, false));
                }
                else
                {
                    Dispatcher.Invoke(() => Notify(ResponseReceiverRecovered, e, false));
                }
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                TBufferedResponseReceiver aResponseReciever;
                lock (myResponseReceivers)
                {
                    aResponseReciever = GetResponseReceiver(e.ResponseReceiverId);
                }

                if (aResponseReciever != null)
                {
                    lock (aResponseReciever.ManipulatorLock)
                    {
                        aResponseReciever.IsOnline = false;
                    }

                    Dispatcher.Invoke(() => Notify(ResponseReceiverInterrupted, e, false));
                }
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

        private TBufferedResponseReceiver GetResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                TBufferedResponseReceiver aResponseReceiver = myResponseReceivers.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);
                return aResponseReceiver;
            }
        }

        private TBufferedResponseReceiver CreateResponseReceiver(string responseReceiverId, string clientAddress, bool notifyWhenConnectedFlag)
        {
            using (EneterTrace.Entering())
            {
                TBufferedResponseReceiver aResponseReceiver = new TBufferedResponseReceiver(responseReceiverId, myUnderlyingInputChannel);
                
                // Note: if it is created as offline then when the client connects raise ResponseReceiverConnected event.
                aResponseReceiver.ResponseReceiverConnectedEventPending = notifyWhenConnectedFlag;

                myResponseReceivers.Add(aResponseReceiver);

                // If it is the first response receiver, then start the timer checking which response receivers
                // are disconnected due to the timeout (i.e. max offline time)
                if (myResponseReceivers.Count == 1)
                {
                    myMaxOfflineChecker.Change(300, -1);
                }

                return aResponseReceiver;
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

                lock (myBroadcasts)
                {
                    // Remove all expired broadcasts.
                    myBroadcasts.RemoveAll(x => aCurrentCheckTime - x.SentAt > myMaxOfflineTime);
                }

                lock (myResponseReceivers)
                {
                    // Remove all not connected response receivers which exceeded the max offline timeout.
                    myResponseReceivers.RemoveWhere(x =>
                        {
                            lock (x.ManipulatorLock)
                            {
                                // If disconnected and max offline time is exceeded. 
                                if (!x.IsOnline &&
                                    aCurrentCheckTime - x.OfflineStartedAt > myMaxOfflineTime)
                                {
                                    aTimeoutedResponseReceivers.Add(x);

                                    // Indicate, the response receiver can be removed.
                                    return true;
                                }

                                // Response receiver will not be removed.
                                return false;
                            }
                        });

                    aTimerShallContinueFlag = myResponseReceivers.Count > 0;
                }

                // Notify disconnected response receivers.
                foreach (TBufferedResponseReceiver aResponseReceiver in aTimeoutedResponseReceivers)
                {
                    // Stop disconnecting if the we are requested to stop.
                    if (myMaxOfflineCheckerRequestedToStop)
                    {
                        return;
                    }

                    // Invoke the event in the correct thread.
                    Dispatcher.Invoke(() => Notify<ResponseReceiverEventArgs>(ResponseReceiverDisconnected, new ResponseReceiverEventArgs(aResponseReceiver.ResponseReceiverId, aResponseReceiver.ClientAddress), false));
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
        private List<TBroadcast> myBroadcasts = new List<TBroadcast>();


        private string TracedObject { get { return GetType().Name + " '" + ChannelId + "' "; } }
    }
}
