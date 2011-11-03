﻿/*
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

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class BufferedDuplexOutputChannel : IDuplexOutputChannel, ICompositeDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public BufferedDuplexOutputChannel(IDuplexOutputChannel underlyingDuplexOutputChannel, TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                UnderlyingDuplexOutputChannel = underlyingDuplexOutputChannel;
                myMaxOfflineTime = maxOfflineTime;
            }
        }

        public string ChannelId { get { return UnderlyingDuplexOutputChannel.ChannelId; } }


        public string ResponseReceiverId { get { return UnderlyingDuplexOutputChannel.ResponseReceiverId; } }


        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return myIsConnectedFlag;
                    }
                }
            }
        }
        

        public IDuplexOutputChannel UnderlyingDuplexOutputChannel { get; private set; }
        

        

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

                    UnderlyingDuplexOutputChannel.ConnectionClosed += OnConnectionClosed;
                    UnderlyingDuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

                    myIsSendingThreadRequestedToStop = false;

                    myIsConnectionOpeningRequestedToStop = false;
                    myConnectionOpeningThreadIsStoppedEvent.Reset();

                    // Try open connection in a different thread.
                    myIsConnectionOpeningActive = true;
                    WaitCallback aDoOpenConnection = x => DoOpenConnection();
                    ThreadPool.QueueUserWorkItem(aDoOpenConnection);

                    // Indicate the connection is open.
                    myIsConnectedFlag = true;
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
                    mySendingThreadIsStoppedEvent.WaitOne(5000);

                    myIsConnectionOpeningRequestedToStop = true;
                    myConnectionOpeningThreadIsStoppedEvent.WaitOne(5000);

                    UnderlyingDuplexOutputChannel.CloseConnection();
                    UnderlyingDuplexOutputChannel.ConnectionClosed -= OnConnectionClosed;

                    // Emty the queue with messages.
                    lock (myMessagesToSend)
                    {
                        myMessagesToSend.Clear();
                    }


                    myIsConnectedFlag = false;
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
                        WaitCallback aSender = x => DoMessageSending();
                        ThreadPool.QueueUserWorkItem(aSender);
                    }
                }
            }
        }

        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseMessageReceived != null)
                {
                    try
                    {
                        ResponseMessageReceived(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // If the opening of the connection is not desired, then just return.
                if (myIsConnectionOpeningRequestedToStop)
                {
                    NotifyCloseConnection();
                    return;
                }

                // Try to reopen the connection in a different thread.
                if (!myIsConnectionOpeningActive)
                {
                    myIsConnectionOpeningActive = true;

                    // Start openning in another thread.
                    WaitCallback aDoOpenConnection = x => DoOpenConnection();
                    ThreadPool.QueueUserWorkItem(aDoOpenConnection);
                }
            }
        }

        private void DoOpenConnection()
        {
            using (EneterTrace.Entering())
            {
                DateTime aStartConnectionTime = DateTime.Now;

                bool aConnectionNotEstablished = false;

                // Loop until the connection is open, or the connection openning is requested to stop,
                // or the max offline time expired.
                while (!myIsConnectionOpeningRequestedToStop)
                {
                    try
                    {
                        UnderlyingDuplexOutputChannel.OpenConnection();
                    }
                    catch
                    {
                        // The connection failed, so try again.
                        // Or the connection was already open (by some other thread).
                    }

                    if (UnderlyingDuplexOutputChannel.IsConnected)
                    {
                        break;
                    }

                    // If the max offline time is exceeded, then notify disconnection.
                    if (DateTime.Now - aStartConnectionTime > myMaxOfflineTime)
                    {
                        aConnectionNotEstablished = true;
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

                if (!myIsConnectionOpeningRequestedToStop)
                {
                    if (aConnectionNotEstablished)
                    {
                        CloseConnection();
                        NotifyCloseConnection();
                    }
                    else
                    {
                        NotifyConnectionOpened();
                    }
                }
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
                                if (UnderlyingDuplexOutputChannel.IsConnected)
                                {
                                    UnderlyingDuplexOutputChannel.SendMessage(aMessage);

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

        private void NotifyConnectionOpened()
        {
            using (EneterTrace.Entering())
            {
                WaitCallback aConnectionOpenedInvoker = x =>
                {
                    using (EneterTrace.Entering())
                    {
                        if (ConnectionOpened != null)
                        {
                            try
                            {
                                DuplexChannelEventArgs aMsg = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId);
                                ConnectionOpened(this, aMsg);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }

                        }
                    }
                };

                // Invoke the event in a different thread.
                ThreadPool.QueueUserWorkItem(aConnectionOpenedInvoker);
            }
        }

        private void NotifyCloseConnection()
        {
            using (EneterTrace.Entering())
            {
                if (ConnectionClosed != null)
                {
                    try
                    {
                        DuplexChannelEventArgs aMsg = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId);
                        ConnectionClosed(this, aMsg);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private TimeSpan myMaxOfflineTime;
        
        private bool myIsConnectedFlag;
        
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
                string aChannelId = (UnderlyingDuplexOutputChannel != null) ? UnderlyingDuplexOutputChannel.ChannelId : "";
                return "Queued duplex output channel '" + aChannelId + "' ";
            }
        }
    }
}
