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
using Eneter.Messaging.Utils.Collections;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class BufferedDuplexInputChannel : IBufferedDuplexInputChannel
    {
        private class TBufferedResponseReceiver
        {
            public TBufferedResponseReceiver(string responseReceiverId, IDuplexInputChannel duplexInputChannel)
            {
                ResponseReceiverId = responseReceiverId;

                // Note: at the time of instantiation the client address does not have to be known.
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
                    myIsOnline = value;

                    if (!myIsOnline)
                    {
                        OfflineStartedAt = DateTime.Now;
                    }
                }
            }

            public bool PendingResponseReceiverConnectedEvent { get; set; }

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

                                // Message was successfully sent therefore it can be removed from the queue.
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

            private IDuplexInputChannel myDuplexInputChannel;
            private Queue<object> myMessageQueue = new Queue<object>();
            private bool myIsOnline;
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

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverOnline;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverOffline;


        public BufferedDuplexInputChannel(IDuplexInputChannel underlyingDuplexInputChannel, TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                myInputChannel = underlyingDuplexInputChannel;
                myMaxOfflineTime = maxOfflineTime;

                myMaxOfflineChecker = new Timer(OnMaxOfflineTimeCheckTick, null, -1, -1);
            }
        }


        public string ChannelId { get { return myInputChannel.ChannelId; } }

        public IThreadDispatcher Dispatcher { get { return myInputChannel.Dispatcher; } }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    myInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
                    myInputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                    myInputChannel.MessageReceived += OnMessageReceived;

                    try
                    {
                        myInputChannel.StartListening();
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
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    // Indicate, that the timer responsible for checking if response receivers are timeouted (i.e. exceeded the max offline time)
                    // shall stop.
                    myMaxOfflineCheckerRequestedToStop = true;

                    try
                    {
                        myInputChannel.StopListening();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.IncorrectlyStoppedListening, err);
                    }

                    using (ThreadLock.Lock(myResponseReceivers))
                    {
                        myBroadcasts.Clear();
                        myResponseReceivers.Clear();
                    }

                    myInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                    myInputChannel.ResponseReceiverDisconnected -= OnResponseReceiverDisconnected;
                    myInputChannel.MessageReceived -= OnMessageReceived;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myListeningManipulatorLock))
                    {
                        return myInputChannel.IsListening;
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
                    using (ThreadLock.Lock(myResponseReceivers))
                    {
                        TBroadcast aBroadcastMessage = new TBroadcast(message);
                        myBroadcasts.Add(aBroadcastMessage);

                        foreach (TBufferedResponseReceiver aResponseReceiver in myResponseReceivers)
                        {
                            // Note: it does not throw exception.
                            aResponseReceiver.SendResponseMessage(message);
                        }
                    }
                }
                else
                {
                    bool aNotifyOffline = false;
                    using (ThreadLock.Lock(myResponseReceivers))
                    {
                        TBufferedResponseReceiver aResponseReciever = GetResponseReceiver(responseReceiverId);
                        if (aResponseReciever == null)
                        {
                            aResponseReciever = CreateResponseReceiver(responseReceiverId, "", true);
                            aNotifyOffline = true;
                        }

                        aResponseReciever.SendResponseMessage(message);

                        if (aNotifyOffline)
                        {
                            ResponseReceiverEventArgs anEvent = new ResponseReceiverEventArgs(responseReceiverId, "");
                            Dispatcher.Invoke(() => Notify(ResponseReceiverOffline, anEvent, false));
                        }
                    }
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myResponseReceivers))
                {
                    myResponseReceivers.RemoveWhere(x => x.ResponseReceiverId == responseReceiverId);
                }

                myInputChannel.DisconnectResponseReceiver(responseReceiverId);
            }
        }

        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                bool aPendingResponseReceicerConnectedEvent = false;
                bool aNewResponseReceiverFlag = false;
                TBufferedResponseReceiver aResponseReciever;
                using (ThreadLock.Lock(myResponseReceivers))
                {
                    aResponseReciever = GetResponseReceiver(e.ResponseReceiverId);
                    if (aResponseReciever == null)
                    {
                        aResponseReciever = CreateResponseReceiver(e.ResponseReceiverId, e.SenderAddress, false);
                        aNewResponseReceiverFlag = true;
                    }
                    
                    aResponseReciever.IsOnline = true;

                    if (aResponseReciever.PendingResponseReceiverConnectedEvent)
                    {
                        aResponseReciever.ClientAddress = e.SenderAddress;
                        aPendingResponseReceicerConnectedEvent = aResponseReciever.PendingResponseReceiverConnectedEvent;
                        aResponseReciever.PendingResponseReceiverConnectedEvent = false;
                    }

                    if (aNewResponseReceiverFlag)
                    {
                        // This is a fresh new response receiver. Therefore broadcast messages were not sent to it yet.
                        foreach (TBroadcast aBroadcastMessage in myBroadcasts)
                        {
                            aResponseReciever.SendResponseMessage(aBroadcastMessage.Message);
                        }
                    }

                    // Send all buffered messages.
                    aResponseReciever.SendMessagesFromQueue();
                }


                Notify(ResponseReceiverOnline, e, false);
                if (aNewResponseReceiverFlag || aPendingResponseReceicerConnectedEvent)
                {
                    Notify(ResponseReceiverConnected, e, false);
                }
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                bool aNotify = false;
                using (ThreadLock.Lock(myResponseReceivers))
                {
                    TBufferedResponseReceiver aResponseReciever = GetResponseReceiver(e.ResponseReceiverId);
                    if (aResponseReciever != null)
                    {
                        aResponseReciever.IsOnline = false;
                        aNotify = true;
                    }
                }

                if (aNotify)
                {
                    Notify(ResponseReceiverOffline, e, false);
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
                TBufferedResponseReceiver aResponseReceiver = new TBufferedResponseReceiver(responseReceiverId, myInputChannel);
                
                // Note: if it is created as offline then when the client connects raise ResponseReceiverConnected event.
                aResponseReceiver.PendingResponseReceiverConnectedEvent = notifyWhenConnectedFlag;

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

                using (ThreadLock.Lock(myResponseReceivers))
                {
                    // Remove all expired broadcasts.
                    myBroadcasts.RemoveAll(x => aCurrentCheckTime - x.SentAt > myMaxOfflineTime);

                    // Remove all not connected response receivers which exceeded the max offline timeout.
                    myResponseReceivers.RemoveWhere(x =>
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
                    Dispatcher.Invoke(() => Notify(ResponseReceiverDisconnected, new ResponseReceiverEventArgs(aResponseReceiver.ResponseReceiverId, aResponseReceiver.ClientAddress), false));
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
        private IDuplexInputChannel myInputChannel;

        private List<TBufferedResponseReceiver> myResponseReceivers = new List<TBufferedResponseReceiver>();
        private List<TBroadcast> myBroadcasts = new List<TBroadcast>();


        private string TracedObject { get { return GetType().Name + " '" + ChannelId + "' "; } }
    }
}
