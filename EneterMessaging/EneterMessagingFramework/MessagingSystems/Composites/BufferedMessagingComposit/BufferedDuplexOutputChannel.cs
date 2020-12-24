

using System;
using System.Collections.Generic;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;
using Eneter.Messaging.Threading;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class BufferedDuplexOutputChannel : IBufferedDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelEventArgs> ConnectionOnline;
        public event EventHandler<DuplexChannelEventArgs> ConnectionOffline;

        public BufferedDuplexOutputChannel(IDuplexOutputChannel underlyingDuplexOutputChannel, TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                myOutputChannel = underlyingDuplexOutputChannel;
                myMaxOfflineTime = maxOfflineTime;
            }
        }

        public string ChannelId { get { return myOutputChannel.ChannelId; } }


        public string ResponseReceiverId { get { return myOutputChannel.ResponseReceiverId; } }

        public IThreadDispatcher Dispatcher { get { return myOutputChannel.Dispatcher; } }


        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myConnectionManipulatorLock))
                    {
                        return myConnectionIsOpenFlag;
                    }
                }
            }
        }

        public bool IsOnline
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myOutputChannel.IsConnected;
                }
            }
        }

        public void OpenConnection()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    if (IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    myOutputChannel.ConnectionOpened += OnConnectionOpened;
                    myOutputChannel.ConnectionClosed += OnConnectionClosed;
                    myOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

                    myConnectionOpeningRequestedToStopFlag = false;
                    myConnectionOpeningEndedEvent.Reset();

                    // Try open connection in a different thread.
                    myConnectionOpeningActiveFlag = true;
                    EneterThreadPool.QueueUserWorkItem(DoOpenConnection);

                    // Indicate the ConnectionOpened event shall be raised when the connection is really open.
                    myIsConnectionOpenEventPendingFlag = true;

                    // Indicate the connection is open.
                    myConnectionIsOpenFlag = true;
                }

                DuplexChannelEventArgs anEvent = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, "");
                Dispatcher.Invoke(() => Notify(ConnectionOffline, anEvent, false));
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    myConnectionOpeningRequestedToStopFlag = true;
                    if (!myConnectionOpeningEndedEvent.WaitOne(5000))
                    {
                        EneterTrace.Warning(TracedObject + "failed to stop the connection openning thread within 5 seconds.");
                    }

                    myOutputChannel.CloseConnection();
                    myOutputChannel.ConnectionOpened -= OnConnectionOpened;
                    myOutputChannel.ConnectionClosed -= OnConnectionClosed;
                    myOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;

                    myMessageQueue.Clear();

                    myConnectionIsOpenFlag = false;
                }
            }
        }


        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    if (!IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.FailedToSendMessageBecauseNotConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    myMessageQueue.Enqueue(message);
                    SendMessagesFromQueue();
                }
            }
        }

        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionOnline, e, false);

                if (myIsConnectionOpenEventPendingFlag)
                {
                    Notify(ConnectionOpened, e, false);
                }

                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    SendMessagesFromQueue();
                }
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    // Try to reopen the connection in a different thread.
                    if (!myConnectionOpeningActiveFlag)
                    {
                        myConnectionOpeningActiveFlag = true;

                        // Start openning in another thread.
                        EneterThreadPool.QueueUserWorkItem(DoOpenConnection);
                    }
                }

                Notify(ConnectionOffline, e, false);
            }
        }

        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify<DuplexChannelMessageEventArgs>(ResponseMessageReceived, e, true);
            }
        }

        private void SendMessagesFromQueue()
        {
            using (EneterTrace.Entering())
            {
                if (IsConnected)
                {
                    while (myMessageQueue.Count > 0)
                    {
                        object aMessage = myMessageQueue.Peek();

                        try
                        {
                            myOutputChannel.SendMessage(aMessage);

                            // Message was successfully sent therefore it can be removed from the queue.
                            myMessageQueue.Dequeue();
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void DoOpenConnection()
        {
            using (EneterTrace.Entering())
            {
                bool aConnectionOpenFlag = false;
                DateTime aStartConnectionTime = DateTime.Now;

                // Loop until the connection is open, or the connection opening is requested to stop,
                // or the max offline time expired.
                while (!myConnectionOpeningRequestedToStopFlag)
                {
                    try
                    {
                        myOutputChannel.OpenConnection();
                        aConnectionOpenFlag = true;
                        break;
                    }
                    catch
                    {
                        // The connection failed, so try again.
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

                // If opening failed and the connection was meanwhile not explicitly closed.
                if (!myConnectionOpeningRequestedToStopFlag)
                {
                    DuplexChannelEventArgs anEvent = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, "");

                    if (!aConnectionOpenFlag)
                    {
                        Dispatcher.Invoke(() => Notify(ConnectionClosed, anEvent, false));
                    }
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
        private IDuplexOutputChannel myOutputChannel;

        private object myConnectionManipulatorLock = new object();
        private bool myIsConnectionOpenEventPendingFlag;
        private bool myConnectionIsOpenFlag;
        private bool myConnectionOpeningActiveFlag;
        private bool myConnectionOpeningRequestedToStopFlag;
        private ManualResetEvent myConnectionOpeningEndedEvent = new ManualResetEvent(true);

        private Queue<object> myMessageQueue = new Queue<object>();

        private string TracedObject { get { return GetType().Name + " '" + ChannelId + "' "; } }
    }
}
