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
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    internal class MonitoredDuplexInputChannel : IDuplexInputChannel
    {
        private class TResponseReceiverContext
        {
            public TResponseReceiverContext(string responseReceiverId, string clientAddress)
            {
                ResponseReceiverId = responseReceiverId;
                ClientAddress = clientAddress;
                LastReceiveTime = DateTime.Now;
                LastPingSentTime = DateTime.Now;
            }

            public string ResponseReceiverId { get; private set; }
            public string ClientAddress { get; private set; }
            public DateTime LastReceiveTime { get; set; }
            public DateTime LastPingSentTime { get; set; }
        }



        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;


        public MonitoredDuplexInputChannel(IDuplexInputChannel underlyingInputChannel, ISerializer serializer,
            int pingFrequency,
            int receiveTimeout)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingInputChannel = underlyingInputChannel;
                mySerializer = serializer;

                myPingFrequency = pingFrequency;
                myReceiveTimeout = receiveTimeout;
                myCheckTimer = new Timer(OnCheckerTick, null, -1, -1);

                MonitorChannelMessage aPingMessage = new MonitorChannelMessage(MonitorChannelMessageType.Ping, null);
                myPreserializedPingMessage = mySerializer.Serialize<MonitorChannelMessage>(aPingMessage);
            }
        }


        public string ChannelId
        {
            get
            {
                return myUnderlyingInputChannel.ChannelId;
            }
        }

        public IThreadDispatcher Dispatcher { get { return myUnderlyingInputChannel.Dispatcher; } }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

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
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
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
                    using (ThreadLock.Lock(myListeningManipulatorLock))
                    {
                        return myUnderlyingInputChannel != null && myUnderlyingInputChannel.IsListening;
                    }
                }
            }
        }

        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Create the response message for the monitor duplex output chanel.
                    MonitorChannelMessage aMessage = new MonitorChannelMessage(MonitorChannelMessageType.Message, message);
                    object aSerializedMessage = mySerializer.Serialize<MonitorChannelMessage>(aMessage);

                    // Send the response message via the underlying channel.
                    myUnderlyingInputChannel.SendResponseMessage(responseReceiverId, aSerializedMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                    throw;
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    using (ThreadLock.Lock(myResponseReceiverContexts))
                    {
                        myResponseReceiverContexts.RemoveWhere(x => x.ResponseReceiverId == responseReceiverId);
                    }

                    myUnderlyingInputChannel.DisconnectResponseReceiver(responseReceiverId);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToDisconnectResponseReceiver + responseReceiverId, err);
                }
            }
        }

        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myResponseReceiverContexts))
                {
                    TResponseReceiverContext aResponseReceiver = GetResponseReceiver(e.ResponseReceiverId);
                    if (aResponseReceiver != null)
                    {
                        EneterTrace.Warning(TracedObject + "received open connection from already connected response receiver.");
                        return;
                    }
                    else
                    {
                        CreateResponseReceiver(e.ResponseReceiverId, e.SenderAddress);
                    }
                }

                if (ResponseReceiverConnected != null)
                {
                    try
                    {
                        ResponseReceiverConnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                int aNumberOfRemoved;
                using (ThreadLock.Lock(myResponseReceiverContexts))
                {
                    aNumberOfRemoved = myResponseReceiverContexts.RemoveWhere(x => x.ResponseReceiverId == e.ResponseReceiverId);
                }

                if (aNumberOfRemoved > 0)
                {
                    NotifyResponseReceiverDisconnected(e);
                }
            }
        }

        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    using (ThreadLock.Lock(myResponseReceiverContexts))
                    {
                        TResponseReceiverContext aResponseReceiver = GetResponseReceiver(e.ResponseReceiverId);
                        if (aResponseReceiver == null)
                        {
                            // Note: the response receiver was just disconnected.
                            return;
                        }

                        aResponseReceiver.LastReceiveTime = DateTime.Now;
                    }

                    // Deserialize the incoming message.
                    MonitorChannelMessage aMessage = mySerializer.Deserialize<MonitorChannelMessage>(e.Message);

                    // if the message is ping, then response.
                    if (aMessage.MessageType == MonitorChannelMessageType.Message)
                    {
                        // Notify the incoming message.
                        if (MessageReceived != null)
                        {
                            DuplexChannelMessageEventArgs aMsg = new DuplexChannelMessageEventArgs(e.ChannelId, aMessage.MessageContent, e.ResponseReceiverId, e.SenderAddress);

                            try
                            {
                                MessageReceived(this, aMsg);
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
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToReceiveMessage, err);
                }
            }
        }

        private void OnCheckerTick(object o)
        {
            using (EneterTrace.Entering())
            {
                List<TResponseReceiverContext> aPingNeededReceivers = new List<TResponseReceiverContext>();
                List<TResponseReceiverContext> aTimeoutedResponseReceivers = new List<TResponseReceiverContext>();
                bool aContinueTimerFlag = false;

                using (ThreadLock.Lock(myResponseReceiverContexts))
                {
                    DateTime aCurrentTime = DateTime.Now;

                    myResponseReceiverContexts.RemoveWhere(x =>
                        {
                            if (aCurrentTime - x.LastReceiveTime > TimeSpan.FromMilliseconds(myReceiveTimeout))
                            {
                                // Store the timeouted response receiver.
                                aTimeoutedResponseReceivers.Add(x);

                                // Indicate, that the response receiver can be removed.
                                return true;
                            }

                            if (aCurrentTime - x.LastPingSentTime >= TimeSpan.FromMilliseconds(myPingFrequency))
                            {
                                aPingNeededReceivers.Add(x);
                            }

                            // Indicate, that the response receiver cannot be removed.
                            return false;
                        });

                    aContinueTimerFlag = myResponseReceiverContexts.Count > 0;
                }

                // Send pings to all receivers which need it.
                foreach (TResponseReceiverContext aResponseReceiver in aPingNeededReceivers)
                {
                    try
                    {
                        myUnderlyingInputChannel.SendResponseMessage(aResponseReceiver.ResponseReceiverId, myPreserializedPingMessage);
                        aResponseReceiver.LastPingSentTime = DateTime.Now;
                    }
                    catch
                    {
                        // The sending of ping failed. It means the response receiver will be notified as disconnected.
                    }
                }

                // Close all removed response receivers.
                foreach (TResponseReceiverContext aResponseReceiver in aTimeoutedResponseReceivers)
                {
                    // Try to disconnect the response receiver.
                    try
                    {
                        myUnderlyingInputChannel.DisconnectResponseReceiver(aResponseReceiver.ResponseReceiverId);
                    }
                    catch
                    {
                        // The response receiver is already disconnected, therefore the attempt
                        // to send a message about the disconnection failed.
                    }

                    // Notify that the response receiver was disconected.
                    ResponseReceiverEventArgs e = new ResponseReceiverEventArgs(aResponseReceiver.ResponseReceiverId, aResponseReceiver.ClientAddress);
                    Dispatcher.Invoke(() => NotifyResponseReceiverDisconnected(e));
                }

                // If the timer chall continue.
                if (aContinueTimerFlag)
                {
                    myCheckTimer.Change(myPingFrequency, -1);
                }
            }
        }

        private TResponseReceiverContext GetResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                TResponseReceiverContext aResponseReceiverContext = myResponseReceiverContexts.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);
                return aResponseReceiverContext;
            }
        }

        private TResponseReceiverContext CreateResponseReceiver(string responseReceiverId, string clientAddress)
        {
            using (EneterTrace.Entering())
            {
                TResponseReceiverContext aResponseReceiver = new TResponseReceiverContext(responseReceiverId, clientAddress);
                myResponseReceiverContexts.Add(aResponseReceiver);

                if (myResponseReceiverContexts.Count == 1)
                {
                    myCheckTimer.Change(myPingFrequency, -1);
                }

                return aResponseReceiver;
            }
        }

        private void NotifyResponseReceiverDisconnected(ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverDisconnected != null)
                {
                    try
                    {
                        ResponseReceiverDisconnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }



        private object myListeningManipulatorLock = new object();
        private IDuplexInputChannel myUnderlyingInputChannel;
        private ISerializer mySerializer;

        private int myPingFrequency;
        private int myReceiveTimeout;
        private Timer myCheckTimer;
        private HashSet<TResponseReceiverContext> myResponseReceiverContexts = new HashSet<TResponseReceiverContext>();

        private object myPreserializedPingMessage;

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
