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
using Eneter.Messaging.Threading;

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    internal class MonitoredDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;


        public MonitoredDuplexOutputChannel(IDuplexOutputChannel underlyingOutputChannel, ISerializer serializer,
            int pingFrequency,
            int receiveTimeout,
            IThreadDispatcher dispatcher)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingOutputChannel = underlyingOutputChannel;

                mySerializer = serializer;
                myPingFrequency = pingFrequency;
                myReceiveTimeout = receiveTimeout;
                Dispatcher = dispatcher;

                MonitorChannelMessage aPingMessage = new MonitorChannelMessage(MonitorChannelMessageType.Ping, null);
                myPreserializedPingMessage = mySerializer.Serialize<MonitorChannelMessage>(aPingMessage);

                myPingingTimer = new EneterTimer(OnPingingTimerTick);
                myReceiveTimer = new EneterTimer(OnResponseTimerTick);

                myUnderlyingOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
                myUnderlyingOutputChannel.ConnectionOpened += OnConnectionOpened;
                myUnderlyingOutputChannel.ConnectionClosed += OnConnectionClosed;
            }
        }

        public string ChannelId { get { return myUnderlyingOutputChannel.ChannelId; } }

        public string ResponseReceiverId { get { return myUnderlyingOutputChannel.ResponseReceiverId; } }

        public IThreadDispatcher Dispatcher { get; private set; }

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

                    try
                    {
                        // Start timers.
                        myPingingTimer.Change(myPingFrequency);
                        myReceiveTimer.Change(myReceiveTimeout);

                        // Open connection in the underlying channel.
                        myUnderlyingOutputChannel.OpenConnection();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToOpenConnection, err);
                        CloseConnection();
                        throw;
                    }
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                CleanAfterConnection(true, false);
            }
        }

        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myConnectionManipulatorLock))
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
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    if (!IsConnected)
                    {
                        string anError = TracedObject + ErrorHandler.FailedToSendMessageBecauseNotConnected;
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

                        // Reschedule the ping.
                        myPingingTimer.Change(myPingFrequency);
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + ErrorHandler.FailedToSendMessage;
                        EneterTrace.Error(anErrorMessage, err);

                        CleanAfterConnection(true, true);

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

                    // Note: timer setting is after deserialization.
                    //       reason: if deserialization fails the timer is not updated and the client will be disconnected.
                    using (ThreadLock.Lock(myConnectionManipulatorLock))
                    {
                        // Cancel the current response timeout and set the new one.
                        myReceiveTimer.Change(myReceiveTimeout);
                    }

                    // If it is a message.
                    if (aMessage.MessageType == MonitorChannelMessageType.Message)
                    {
                        Dispatcher.Invoke(() => Notify<DuplexChannelMessageEventArgs>(ResponseMessageReceived, new DuplexChannelMessageEventArgs(e.ChannelId, aMessage.MessageContent, e.ResponseReceiverId, e.SenderAddress), true));
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToReceiveMessage, err);
                }
            }
        }

        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Dispatcher.Invoke(() => Notify<DuplexChannelEventArgs>(ConnectionOpened, e, false));
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                CleanAfterConnection(false, true);
            }
        }
        

        // The method is called if the inactivity (not sending messages) exceeded the pinging frequency time.
        private void OnPingingTimerTick(object x)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    using (ThreadLock.Lock(myConnectionManipulatorLock))
                    {
                        // Send the ping message.
                        myUnderlyingOutputChannel.SendMessage(myPreserializedPingMessage);

                        // Schedule the next ping.
                        myPingingTimer.Change(myPingFrequency);
                    }
                }
                catch
                {
                    // The sending of the ping message failed - the connection is broken.
                    CleanAfterConnection(true, true);
                }
            }
        }

        // The method is called if there is no message from the input channel within response timeout.
        private void OnResponseTimerTick(object x)
        {
            using (EneterTrace.Entering())
            {
                CleanAfterConnection(true, true);
            }
        }

        private void CleanAfterConnection(bool sendCloseMessageFlag, bool notifyConnectionClosedFlag)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    // Stop timers.
                    myPingingTimer.Change(-1);
                    myReceiveTimer.Change(-1);

                    if (sendCloseMessageFlag)
                    {
                        myUnderlyingOutputChannel.CloseConnection();
                    }
                }

                if (notifyConnectionClosedFlag)
                {
                    Dispatcher.Invoke(() => Notify<DuplexChannelEventArgs>(ConnectionClosed, new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, ""), false));
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

        private IDuplexOutputChannel myUnderlyingOutputChannel;
        private object myConnectionManipulatorLock = new object();
        
        private EneterTimer myPingingTimer;
        private int myPingFrequency;

        private EneterTimer myReceiveTimer;
        private int myReceiveTimeout;

        private ISerializer mySerializer;
        private object myPreserializedPingMessage;

        private string TracedObject { get { return GetType().Name + " '" + ChannelId + "' "; } }
    }
}
