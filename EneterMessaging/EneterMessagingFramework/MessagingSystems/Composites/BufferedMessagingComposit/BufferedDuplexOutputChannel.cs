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
                        return myIsOpenConnectionCalledFlag;
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

                    myUnderlyingOutputChannel.ConnectionOpened += OnConnectionOpened;
                    myUnderlyingOutputChannel.ConnectionClosed += OnConnectionClosed;
                    myUnderlyingOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

                    myIsSendingThreadRequestedToStop = false;

                    myIsConnectionOpeningRequestedToStop = false;
                    myConnectionOpeningThreadIsStoppedEvent.Reset();

                    // Try open connection in a different thread.
                    myIsConnectionOpeningActive = true;
                    ThreadPool.QueueUserWorkItem(x => DoOpenConnection());

                    // Indicate the connection is open.
                    myIsOpenConnectionCalledFlag = true;
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myIsSendingThreadRequestedToStop = true;
                    if (!mySendingThreadIsStoppedEvent.WaitOne(5000))
                    {
                        EneterTrace.Warning(TracedObject + "failed to stop the message sending thread within 5 seconds.");
                    }

                    myIsConnectionOpeningRequestedToStop = true;
                    if (!myConnectionOpeningThreadIsStoppedEvent.WaitOne(5000))
                    {
                        EneterTrace.Warning(TracedObject + "failed to stop the connection openning thread within 5 seconds.");
                    }

                    myUnderlyingOutputChannel.CloseConnection();
                    myUnderlyingOutputChannel.ConnectionOpened -= OnConnectionOpened;
                    myUnderlyingOutputChannel.ConnectionClosed -= OnConnectionClosed;
                    myUnderlyingOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;

                    // Emty the queue with messages.
                    lock (myMessagesToSend)
                    {
                        myMessagesToSend.Clear();
                    }


                    myIsOpenConnectionCalledFlag = false;
                }
            }
        }


        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                if (!IsConnected)
                {
                    string aMessage = TracedObject + ErrorHandler.SendMessageNotConnectedFailure;
                    EneterTrace.Error(aMessage);
                    throw new InvalidOperationException(aMessage);
                }

                lock (myMessagesToSend)
                {
                    myMessagesToSend.Add(message);

                    if (!mySendingThreadActiveFlag)
                    {
                        mySendingThreadActiveFlag = true;
                        mySendingThreadIsStoppedEvent.Reset();

                        // Start thread responsible for sending messages.
                        ThreadPool.QueueUserWorkItem(x => DoMessageSending());
                    }
                }
            }
        }

        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify<DuplexChannelEventArgs>(ConnectionOpened, e, false);
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // If the opening of the connection is not desired, then just return.
                if (myIsConnectionOpeningRequestedToStop)
                {
                    Notify<DuplexChannelEventArgs>(ConnectionClosed, e, false);
                    return;
                }

                // Try to reopen the connection in a different thread.
                if (!myIsConnectionOpeningActive)
                {
                    myIsConnectionOpeningActive = true;

                    // Start openning in another thread.
                    ThreadPool.QueueUserWorkItem(x => DoOpenConnection());
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

        private void DoOpenConnection()
        {
            using (EneterTrace.Entering())
            {
                DateTime aStartConnectionTime = DateTime.Now;

                // Loop until the connection is open, or the connection openning is requested to stop,
                // or the max offline time expired.
                while (!myIsConnectionOpeningRequestedToStop)
                {
                    try
                    {
                        myUnderlyingOutputChannel.OpenConnection();
                    }
                    catch
                    {
                        // The connection failed, so try again.
                        // Or the connection was already open (by some other thread).
                    }

                    if (myUnderlyingOutputChannel.IsConnected)
                    {
                        break;
                    }

                    // If the max offline time is exceeded, then notify disconnection.
                    if (DateTime.Now - aStartConnectionTime > myMaxOfflineTime)
                    {
                        break;
                    }

                    // Do not wait for the next attempt, if the connection opening shall stop.
                    if (!myIsConnectionOpeningRequestedToStop)
                    {
                        Thread.Sleep(300);
                    }
                }


                // Indicate this connection opening is not active.
                // The CloseConnection() is going to be called.
                // There is the WaitOne(), waiting until the
                myIsConnectionOpeningActive = false;
                myConnectionOpeningThreadIsStoppedEvent.Set();
            }
        }

        private void DoMessageSending()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Loop taking messages from the queue, until the queue is empty or there is a request to stop sending.
                    while (!myIsSendingThreadRequestedToStop)
                    {
                        object aMessage;

                        lock (myMessagesToSend)
                        {
                            // If there is a message in the queue, read it.
                            if (myMessagesToSend.Count > 0)
                            {
                                aMessage = myMessagesToSend[0];
                            }
                            else
                            {
                                // There are no messages in the queue, therefore the thread can end.
                                return;
                            }
                        }


                        // Loop until the message is sent or until there is no request to stop sending.
                        while (!myIsSendingThreadRequestedToStop)
                        {
                            try
                            {
                                if (myUnderlyingOutputChannel.IsConnected)
                                {
                                    myUnderlyingOutputChannel.SendMessage(aMessage);

                                    // The message was successfuly sent, therefore remove it from the queue.
                                    lock (myMessagesToSend)
                                    {
                                        myMessagesToSend.RemoveAt(0);
                                    }

                                    break;
                                }
                            }
                            catch
                            {
                                // The sending of the message failed, therefore wait for a while and try again.
                            }

                            // Do not wait if there is a request to stop the sending.
                            if (!myIsSendingThreadRequestedToStop)
                            {
                                Thread.Sleep(300);
                            }
                        }
                    }
                }
                finally
                {
                    mySendingThreadActiveFlag = false;
                    mySendingThreadIsStoppedEvent.Set();
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

        private bool myIsOpenConnectionCalledFlag;
        
        private bool mySendingThreadActiveFlag;
        private bool myIsSendingThreadRequestedToStop;
        private ManualResetEvent mySendingThreadIsStoppedEvent = new ManualResetEvent(true);

        private bool myIsConnectionOpeningActive;
        private bool myIsConnectionOpeningRequestedToStop;
        private ManualResetEvent myConnectionOpeningThreadIsStoppedEvent = new ManualResetEvent(true);

        private object myConnectionManipulatorLock = new object();
        private List<object> myMessagesToSend = new List<object>();


        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
