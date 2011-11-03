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

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    internal class MonitoredDuplexOutputChannel : IDuplexOutputChannel, ICompositeDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;



        public MonitoredDuplexOutputChannel(IDuplexOutputChannel underlyingOutputChannel, ISerializer serializer, TimeSpan pingFrequency, TimeSpan pingResponseTimeout)
        {
            using (EneterTrace.Entering())
            {
                UnderlyingDuplexOutputChannel = underlyingOutputChannel;

                mySerializer = serializer;
                myPingFrequency = pingFrequency;
                myPingResponseTimeout = pingResponseTimeout;
            }
        }

        public string ChannelId { get { return UnderlyingDuplexOutputChannel.ChannelId; } }

        public string ResponseReceiverId { get { return UnderlyingDuplexOutputChannel.ResponseReceiverId; } }

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

                    UnderlyingDuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
                    UnderlyingDuplexOutputChannel.ConnectionOpened += OnConnectionOpened;
                    UnderlyingDuplexOutputChannel.ConnectionClosed += OnConnectionClosed;

                    try
                    {
                        // Open connection in the underlying channel.
                        UnderlyingDuplexOutputChannel.OpenConnection();
                    }
                    catch (Exception err)
                    {
                        UnderlyingDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                        UnderlyingDuplexOutputChannel.ConnectionOpened -= OnConnectionOpened;
                        UnderlyingDuplexOutputChannel.ConnectionClosed -= OnConnectionClosed;

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
                    myPingingRequestedToStopFlag = true;

                    // Interrupt waiting for the frequency time.
                    myPingFrequencyWaiting.Set();

                    // Interrupt waiting for the response of the ping.
                    myResponseReceivedEvent.Set();

                    // Wait until the pinging thread stopped.
                    if (myPingingThread != null && myPingingThread.ThreadState != ThreadState.Unstarted)
                    {
                        //myPingingThread.Join();

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
                        return UnderlyingDuplexOutputChannel.IsConnected;
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

                    // Get the message recognized by the monitor duplex input channel.
                    MonitorChannelMessage aMessage = new MonitorChannelMessage(MonitorChannelMessageType.Message, message);
                    object aSerializedMessage = mySerializer.Serialize<MonitorChannelMessage>(aMessage);

                    // Send the message by using the underlying messaging system.
                    UnderlyingDuplexOutputChannel.SendMessage(aSerializedMessage);
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
                        // Notify the event to the subscriber.
                        if (ResponseMessageReceived != null)
                        {
                            DuplexChannelMessageEventArgs aMsg = new DuplexChannelMessageEventArgs(e.ChannelId, aMessage.MessageContent, e.ResponseReceiverId);

                            try
                            {
                                ResponseMessageReceived(this, aMsg);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
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
                if (ConnectionOpened != null)
                {
                    try
                    {
                        ConnectionOpened(this, e);
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
                CloseConnection();
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
                        myPingFrequencyWaiting.WaitOne(myPingFrequency);

                        if (!myPingingRequestedToStopFlag)
                        {
                            // Send the ping message.
                            MonitorChannelMessage aPingMessage = new MonitorChannelMessage(MonitorChannelMessageType.Ping, null);
                            object aSerializedPingMessage = mySerializer.Serialize<MonitorChannelMessage>(aPingMessage);
                            UnderlyingDuplexOutputChannel.SendMessage(aSerializedPingMessage);
                        }
                    }
                    catch
                    {
                        // The sending of the ping message failed.
                        // Therefore the connection is broken and the pinging will be stopped.
                        break;
                    }

                    // If the response does not come in defined time, then it consider that as broken connection.
                    if (!myResponseReceivedEvent.WaitOne(myPingResponseTimeout))
                    {
                        break;
                    }
                }

                // Close the underlying channel.
                if (UnderlyingDuplexOutputChannel != null)
                {
                    try
                    {
                        // Close connection in the underlying channel.
                        UnderlyingDuplexOutputChannel.CloseConnection();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                    }

                    UnderlyingDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                    UnderlyingDuplexOutputChannel.ConnectionClosed -= OnConnectionClosed;
                }

                // Notify, the connection is closed.
                NotifyConnectionClosed();
            }
        }

        private void NotifyConnectionClosed()
        {
            using (EneterTrace.Entering())
            {
                WaitCallback aConnectionClosedInvoker = x =>
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
                    };

                // Invoke the event in a different thread.
                ThreadPool.QueueUserWorkItem(aConnectionClosedInvoker);
            }
        }


        private object myConnectionManipulatorLock = new object();
        private TimeSpan myPingFrequency;
        private AutoResetEvent myPingFrequencyWaiting = new AutoResetEvent(false);
        private TimeSpan myPingResponseTimeout;
        private bool myPingingRequestedToStopFlag;
        private AutoResetEvent myResponseReceivedEvent = new AutoResetEvent(false);
        private Thread myPingingThread;

        private ISerializer mySerializer;


        private string TracedObject
        {
            get
            {
                string aChannelId = (UnderlyingDuplexOutputChannel != null) ? UnderlyingDuplexOutputChannel.ChannelId : "";
                return "The monitor duplex output channel '" + aChannelId + "' ";
            }
        }
    }
}
