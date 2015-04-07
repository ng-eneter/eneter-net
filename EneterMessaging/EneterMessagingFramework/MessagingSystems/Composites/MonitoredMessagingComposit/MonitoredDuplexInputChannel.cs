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
                LastUpdateTime = DateTime.Now;
            }

            public string ResponseReceiverId { get; private set; }
            public string ClientAddress { get; private set; }
            public DateTime LastUpdateTime { get; set; }
        }



        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;


        public MonitoredDuplexInputChannel(IDuplexInputChannel underlyingInputChannel, ISerializer serializer, TimeSpan pingTimeout)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingInputChannel = underlyingInputChannel;
                mySerializer = serializer;
                myPingTimeout = pingTimeout;

                myPingTimeoutChecker = new Timer(OnPingTimeoutCheckerTick, null, -1, -1);
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
                lock (myListeningManipulatorLock)
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    myUnderlyingInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
                    myUnderlyingInputChannel.MessageReceived += OnMessageReceived;

                    try
                    {
                        myUnderlyingInputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        myUnderlyingInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                        myUnderlyingInputChannel.MessageReceived -= OnMessageReceived;

                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
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
                    lock (myResponseReceiverContexts)
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
                lock (myResponseReceiverContexts)
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

        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    lock (myResponseReceiverContexts)
                    {
                        TResponseReceiverContext aResponseReceiver = GetResponseReceiver(e.ResponseReceiverId);
                        if (aResponseReceiver == null)
                        {
                            EneterTrace.Warning(TracedObject + "received message from not connected response receiver.");
                            return;
                        }

                        aResponseReceiver.LastUpdateTime = DateTime.Now;
                    }

                    // Deserialize the incoming message.
                    MonitorChannelMessage aMessage = mySerializer.Deserialize<MonitorChannelMessage>(e.Message);

                    // if the message is ping, then response.
                    if (aMessage.MessageType == MonitorChannelMessageType.Ping)
                    {
                        try
                        {
                            myUnderlyingInputChannel.SendResponseMessage(e.ResponseReceiverId, e.Message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to response the ping message.", err);
                        }
                    }
                    else
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

        private void OnPingTimeoutCheckerTick(object o)
        {
            using (EneterTrace.Entering())
            {
                List<TResponseReceiverContext> aTimeoutedResponseReceivers = new List<TResponseReceiverContext>();

                bool aContinueTimerFlag = false;

                lock (myResponseReceiverContexts)
                {
                    DateTime aCurrentTime = DateTime.Now;

                    myResponseReceiverContexts.RemoveWhere(x =>
                        {
                            if (aCurrentTime - x.LastUpdateTime > myPingTimeout)
                            {
                                // Store the timeouted response receiver.
                                aTimeoutedResponseReceivers.Add(x);

                                // Indicate, that the response receiver can be removed.
                                return true;
                            }

                            // Indicate, that the response receiver cannot be removed.
                            return false;
                        });

                    aContinueTimerFlag = myResponseReceiverContexts.Count > 0;
                }

                // Close connection for all timeouted response receivers.
                foreach (TResponseReceiverContext aResponseReceiver in aTimeoutedResponseReceivers)
                {
                    // Try to disconnect the response receiver.
                    try
                    {
                        DisconnectResponseReceiver(aResponseReceiver.ResponseReceiverId);
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
                    myPingTimeoutChecker.Change(500, -1);
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
                    myPingTimeoutChecker.Change(300, -1);
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

        private TimeSpan myPingTimeout;
        private Timer myPingTimeoutChecker;
        private HashSet<TResponseReceiverContext> myResponseReceiverContexts = new HashSet<TResponseReceiverContext>();

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
