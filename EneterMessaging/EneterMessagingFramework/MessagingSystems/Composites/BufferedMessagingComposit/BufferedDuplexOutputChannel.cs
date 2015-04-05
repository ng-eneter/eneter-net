/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class BufferedDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelEventArgs> Offline;
        public event EventHandler<DuplexChannelEventArgs> ConnectionRecovered;

        public BufferedDuplexOutputChannel(IDuplexOutputChannel underlyingDuplexOutputChannel, TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingOutputChannel = underlyingDuplexOutputChannel;
                myMaxOfflineTime = maxOfflineTime;
            }
        }

        public string ChannelId { get { return myUnderlyingOutputChannel.ChannelId; } }


        public string ResponseReceiverId { get { return myUnderlyingOutputChannel.ResponseReceiverId; } }

        public IThreadDispatcher Dispatcher { get { return myUnderlyingOutputChannel.Dispatcher; } }


        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return myConnectionIsOpenFlag;
                    }
                }
            }
        }
        
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

                    myUnderlyingOutputChannel.ConnectionClosed += OnConnectionClosed;
                    myUnderlyingOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

                    myMessageQueueRequestedToStopFlag = false;

                    myConnectionOpeningRequestedToStopFlag = false;
                    myConnectionOpeningEndedEvent.Reset();

                    // Try open connection in a different thread.
                    myConnectionOpeningActiveFlag = true;
                    ThreadPool.QueueUserWorkItem(DoOpenConnection);

                    // Indicate the connection is open.
                    myConnectionIsOpenFlag = true;
                }

                DuplexChannelEventArgs anEvent = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, "");
                Dispatcher.Invoke(() => Notify(ConnectionOpened, anEvent, false));
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myMessageQueueRequestedToStopFlag = true;
                    if (!myMessageQueueEndedEvent.WaitOne(5000))
                    {
                        EneterTrace.Warning(TracedObject + "failed to stop the message sending thread within 5 seconds.");
                    }

                    myConnectionOpeningActiveFlag = true;
                    if (!myConnectionOpeningEndedEvent.WaitOne(5000))
                    {
                        EneterTrace.Warning(TracedObject + "failed to stop the connection openning thread within 5 seconds.");
                    }

                    myUnderlyingOutputChannel.CloseConnection();
                    myUnderlyingOutputChannel.ConnectionClosed -= OnConnectionClosed;
                    myUnderlyingOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;

                    lock (myMessageQueue)
                    {
                        myMessageQueue.Clear();
                    }

                    myConnectionIsOpenFlag = false;
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
                        string aMessage = TracedObject + ErrorHandler.FailedToSendMessageBecauseNotConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    lock (myMessageQueue)
                    {
                        if (!myMessageQueueActiveFlag)
                        {
                            myMessageQueueActiveFlag = true;

                            // Start thread responsible for sending messages.
                            ThreadPool.QueueUserWorkItem(DoMessageSending);
                        }

                        myMessageQueue.Enqueue(message);
                    }
                }
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Try to reopen the connection in a different thread.
                if (!myConnectionOpeningActiveFlag)
                {
                    myConnectionOpeningActiveFlag = true;

                    // Start openning in another thread.
                    ThreadPool.QueueUserWorkItem(DoOpenConnection);
                }
            }
        }

        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify<DuplexChannelMessageEventArgs>(ResponseMessageReceived, e, true);
            }
        }

        private void DoOpenConnection(object x)
        {
            using (EneterTrace.Entering())
            {
                DuplexChannelEventArgs anEvent = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, "");
                myUnderlyingOutputChannel.Dispatcher.Invoke(() => Notify(Offline, anEvent, false));

                bool aConnectionOpenFlag = false;
                DateTime aStartConnectionTime = DateTime.Now;

                // Loop until the connection is open, or the connection openning is requested to stop,
                // or the max offline time expired.
                while (!myConnectionOpeningRequestedToStopFlag)
                {
                    try
                    {
                        myUnderlyingOutputChannel.OpenConnection();
                        aConnectionOpenFlag = true;
                        break;
                    }
                    catch
                    {
                        // The connection failed, so try again.
                        // Or the connection was already open (by some other thread).
                    }

                    // If the max offline time is exceeded, then notify disconnection.
                    if (DateTime.Now - aStartConnectionTime > myMaxOfflineTime)
                    {
                        break;
                    }

                    // Do not wait for the next attempt, if the connection opening shall stop.
                    if (!myConnectionOpeningRequestedToStopFlag)
                    {
                        Thread.Sleep(300);
                    }
                }


                // Indicate this connection opening is not active.
                myConnectionOpeningActiveFlag = false;
                myConnectionOpeningEndedEvent.Set();

                // If openning failed and the connection was meanwhile not explicitelly closed.
                if (!myConnectionOpeningRequestedToStopFlag)
                {
                    if (!aConnectionOpenFlag)
                    {
                        myUnderlyingOutputChannel.Dispatcher.Invoke(() => Notify(ConnectionClosed, anEvent, false));
                    }
                    else
                    {
                        myUnderlyingOutputChannel.Dispatcher.Invoke(() => Notify(ConnectionRecovered, anEvent, false));
                    }
                }
            }
        }

        private void DoMessageSending(object x)
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


                        // Loop until the message is sent or until there is no request to stop sending.
                        while (!myMessageQueueRequestedToStopFlag)
                        {
                            try
                            {
                                if (myUnderlyingOutputChannel.IsConnected)
                                {
                                    myUnderlyingOutputChannel.SendMessage(aMessage);

                                    // The message was successfuly sent, therefore remove it from the queue.
                                    lock (myMessageQueue)
                                    {
                                        myMessageQueue.Dequeue();
                                    }

                                    break;
                                }
                            }
                            catch
                            {
                                // The sending of the message failed, therefore wait for a while and try again.
                            }

                            // Do not wait if there is a request to stop the sending.
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

        private TimeSpan myMaxOfflineTime;
        private IDuplexOutputChannel myUnderlyingOutputChannel;

        private object myConnectionManipulatorLock = new object();
        private bool myConnectionIsOpenFlag;
        private bool myConnectionOpeningActiveFlag;
        private bool myConnectionOpeningRequestedToStopFlag;
        private ManualResetEvent myConnectionOpeningEndedEvent = new ManualResetEvent(true);

        private bool myMessageQueueActiveFlag;
        private bool myMessageQueueRequestedToStopFlag;
        private ManualResetEvent myMessageQueueEndedEvent = new ManualResetEvent(true);
        private Queue<object> myMessageQueue = new Queue<object>();

        private string TracedObject { get { return GetType().Name + " '" + ChannelId + "' "; } }
    }
}
