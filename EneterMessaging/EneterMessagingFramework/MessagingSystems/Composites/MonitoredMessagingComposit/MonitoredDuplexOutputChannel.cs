/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    internal class MonitoredDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;


        public MonitoredDuplexOutputChannel(IDuplexOutputChannel underlyingOutputChannel, ISerializer serializer, TimeSpan pingFrequency, TimeSpan pingResponseTimeout)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingOutputChannel = underlyingOutputChannel;

                mySerializer = serializer;
                myPingFrequency = pingFrequency;
                myPingResponseTimeout = pingResponseTimeout;
            }
        }

        public string ChannelId { get { return myUnderlyingOutputChannel.ChannelId; } }

        public string ResponseReceiverId { get { return myUnderlyingOutputChannel.ResponseReceiverId; } }

        public IThreadDispatcher Dispatcher { get { return myUnderlyingOutputChannel.Dispatcher; } }

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

                    myUnderlyingOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
                    myUnderlyingOutputChannel.ConnectionOpened += OnConnectionOpened;

                    try
                    {
                        // Open connection in the underlying channel.
                        myUnderlyingOutputChannel.OpenConnection();
                    }
                    catch (Exception err)
                    {
                        myUnderlyingOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                        myUnderlyingOutputChannel.ConnectionOpened -= OnConnectionOpened;

                        EneterTrace.Error(TracedObject + ErrorHandler.OpenConnectionFailure, err);

                        throw;
                    }

                    // Indicate, the pinging shall run.
                    myPingingRequestedToStopFlag = false;
                    myPingFrequencyWaiting.Reset();
                    myResponseReceivedEvent.Reset();

                    myPingingThread = new Thread(DoPinging);
                    myPingingThread.Start();
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    // Indicate, the pinging thread shall stop.
                    // Note: when pinging thread stops, it also closes the underlying duplex output channel.
                    myPingingRequestedToStopFlag = true;

                    // Interrupt waiting for the frequency time.
                    myPingFrequencyWaiting.Set();

                    // Interrupt waiting for the response of the ping.
                    myResponseReceivedEvent.Set();

                    // Wait until the pinging thread is stopped.
#if COMPACT_FRAMEWORK
                    if (myPingingThread != null)
#else
                    if (myPingingThread != null && myPingingThread.ThreadState != ThreadState.Unstarted)
#endif
                    {
                        if (!myPingingThread.Join(3000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myPingingThread.ManagedThreadId);

                            try
                            {
                                myPingingThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                            }
                        }
                    }
                    myPingingThread = null;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return myUnderlyingOutputChannel.IsConnected;
                    }
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
                        string anError = TracedObject + ErrorHandler.SendMessageNotConnectedFailure;
                        EneterTrace.Error(anError);
                        throw new InvalidOperationException(anError);
                    }

                    try
                    {
                        // Get the message recognized by the monitor duplex input channel.
                        MonitorChannelMessage aMessage = new MonitorChannelMessage(MonitorChannelMessageType.Message, message);
                        object aSerializedMessage = mySerializer.Serialize<MonitorChannelMessage>(aMessage);

                        // Send the message by using the underlying messaging system.
                        myUnderlyingOutputChannel.SendMessage(aSerializedMessage);
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + ErrorHandler.SendMessageFailure;
                        EneterTrace.Error(anErrorMessage, err);
                        throw;
                    }
                }
            }
        }

        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Deserialize the message.
                    MonitorChannelMessage aMessage = mySerializer.Deserialize<MonitorChannelMessage>(e.Message);

                    // If it is the response for the ping.
                    if (aMessage.MessageType == MonitorChannelMessageType.Ping)
                    {
                        // Release the pinging thread waiting for the response.
                        myResponseReceivedEvent.Set();
                    }
                    else
                    {
                        Notify<DuplexChannelMessageEventArgs>(ResponseMessageReceived, new DuplexChannelMessageEventArgs(e.ChannelId, aMessage.MessageContent, e.ResponseReceiverId, e.SenderAddress), true);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.ReceiveMessageFailure, err);
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

        private void DoPinging()
        {
            using (EneterTrace.Entering())
            {
                // While the flag indicates, the pinging shall run.
                while (!myPingingRequestedToStopFlag)
                {
                    try
                    {
                        // Wait, before the next ping.
                        myPingFrequencyWaiting.WaitOne((int)myPingFrequency.TotalMilliseconds);

                        if (!myPingingRequestedToStopFlag)
                        {
                            // Send the ping message.
                            MonitorChannelMessage aPingMessage = new MonitorChannelMessage(MonitorChannelMessageType.Ping, null);
                            object aSerializedPingMessage = mySerializer.Serialize<MonitorChannelMessage>(aPingMessage);
                            myUnderlyingOutputChannel.SendMessage(aSerializedPingMessage);
                        }
                    }
                    catch
                    {
                        // The sending of the ping message failed.
                        // Therefore the connection is broken and the pinging will be stopped.
                        break;
                    }

                    // If the response does not come in defined time, then it consider that as broken connection.
                    if (!myResponseReceivedEvent.WaitOne((int)myPingResponseTimeout.TotalMilliseconds))
                    {
                        break;
                    }
                }

                // Close the underlying channel.
                if (myUnderlyingOutputChannel != null)
                {
                    try
                    {
                        // Close connection in the underlying channel.
                        myUnderlyingOutputChannel.CloseConnection();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                    }

                    myUnderlyingOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                    myUnderlyingOutputChannel.ConnectionOpened -= OnConnectionOpened;
                }

                // Notify, the connection is closed.
                ThreadPool.QueueUserWorkItem(x =>
                    Dispatcher.Invoke(() => Notify<DuplexChannelEventArgs>(ConnectionClosed, new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, ""), false))
                    );
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

        private IDuplexOutputChannel myUnderlyingOutputChannel;
        private object myConnectionManipulatorLock = new object();
        private TimeSpan myPingFrequency;
        private AutoResetEvent myPingFrequencyWaiting = new AutoResetEvent(false);
        private TimeSpan myPingResponseTimeout;
        private volatile bool myPingingRequestedToStopFlag;
        private AutoResetEvent myResponseReceivedEvent = new AutoResetEvent(false);
        private Thread myPingingThread;

        private ISerializer mySerializer;


        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
