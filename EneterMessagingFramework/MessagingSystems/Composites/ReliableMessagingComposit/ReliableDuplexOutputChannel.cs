/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit
{
    internal class ReliableDuplexOutputChannel : IReliableDuplexOutputChannel
    {
        public event EventHandler<MessageIdEventArgs> MessageDelivered;

        public event EventHandler<MessageIdEventArgs> MessageNotDelivered;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;


        public ReliableDuplexOutputChannel(IDuplexOutputChannel underlyingDuplexOutputChannel, ISerializer serializer, TimeSpan acknowledgeTimeout)
        {
            using (EneterTrace.Entering())
            {
                UnderlyingDuplexOutputChannel = underlyingDuplexOutputChannel;
                mySerializer = serializer;
                myAcknowledgeTimeout = acknowledgeTimeout;

                myAcknowledgeTimer = new Timer(OnCheckAcknowledgeTimeoutTick);
                myAcknowledgeTimer.Change(-1, -1);
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

                    UnderlyingDuplexOutputChannel.ConnectionOpened += OnConnectionOpened;
                    UnderlyingDuplexOutputChannel.ConnectionClosed += OnConnectionClosed;
                    UnderlyingDuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

                    try
                    {
                        UnderlyingDuplexOutputChannel.OpenConnection();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.OpenConnectionFailure, err);

                        UnderlyingDuplexOutputChannel.ConnectionOpened -= OnConnectionOpened;
                        UnderlyingDuplexOutputChannel.ConnectionClosed -= OnConnectionClosed;
                        UnderlyingDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;

                        throw;
                    }
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (UnderlyingDuplexOutputChannel != null)
                    {
                        try
                        {
                            UnderlyingDuplexOutputChannel.CloseConnection();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                        }

                        UnderlyingDuplexOutputChannel.ConnectionOpened -= OnConnectionOpened;
                        UnderlyingDuplexOutputChannel.ConnectionClosed -= OnConnectionClosed;
                        UnderlyingDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                    }
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
                        return UnderlyingDuplexOutputChannel != null && UnderlyingDuplexOutputChannel.IsConnected;
                    }
                }
            }
        }
        

        public string SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                if (!IsConnected)
                {
                    string anError = TracedObject + ErrorHandler.SendMessageNotConnectedFailure;
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                // Create the reliable message.
                string aMessageId = Guid.NewGuid().ToString();

                // Add the message among sent messages.
                // Note: The message must be put among sent message due to the possible race-condition.
                //       => if the message acknowledgement would be received before the message would
                //          be put among send messages - e.g. this happens in case of synchronous messaging.
                lock (mySentMessages)
                {
                    SentMessageItem anItem = new SentMessageItem(aMessageId);
                    mySentMessages.Add(anItem);

                    // If this is the only message among sent messages then start the timer measuring
                    // if the acknowledge message was received before the timeout.
                    if (mySentMessages.Count > 0)
                    {
                        myAcknowledgeTimer.Change(500, Timeout.Infinite);
                    }
                }

                try
                {
                    ReliableMessage aMessage = new ReliableMessage(aMessageId, message);
                    object aSerializedMessage = mySerializer.Serialize<ReliableMessage>(aMessage);

                    UnderlyingDuplexOutputChannel.SendMessage(aSerializedMessage);
                }
                catch (Exception err)
                {
                    // Remove the message from the list of sent messages.
                    lock (mySentMessages)
                    {
                        mySentMessages.RemoveWhere(x => x.MessageId == aMessageId);
                    }

                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);

                    throw;
                }

                return aMessageId;
            }
        }

        void IDuplexOutputChannel.SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                this.SendMessage(message);
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
                if (ConnectionClosed != null)
                {
                    try
                    {
                        ConnectionClosed(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
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
                    // Deserialize the incoming message.
                    ReliableMessage aReceivedMessage = mySerializer.Deserialize<ReliableMessage>(e.Message);

                    // if the message is some acknowledgement for some previously sent response message.
                    if (aReceivedMessage.MessageType == ReliableMessage.EMessageType.Acknowledge)
                    {
                        // Remove the message from the list of sent messaging waiting for the acknowledgement.
                        lock (mySentMessages)
                        {
                            mySentMessages.RemoveWhere(x => x.MessageId == aReceivedMessage.MessageId);
                        }

                        NotifyMessageDelivered(aReceivedMessage.MessageId);
                    }
                    else
                    {
                        try
                        {
                            // The received message is a regular message.
                            // Send the acknowledgement back to the sender.
                            ReliableMessage anAcknowledgeMessage = new ReliableMessage(aReceivedMessage.MessageId);
                            object aSerializedAcknowledgeMessage = mySerializer.Serialize<ReliableMessage>(anAcknowledgeMessage);
                            UnderlyingDuplexOutputChannel.SendMessage(aSerializedAcknowledgeMessage);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + "failed to send the acknowledge message.", err);
                        }

                        // Notify the message to the subscriber.
                        if (ResponseMessageReceived != null)
                        {
                            try
                            {
                                DuplexChannelMessageEventArgs aMsg = new DuplexChannelMessageEventArgs(e.ChannelId, aReceivedMessage.Message, e.ResponseReceiverId);
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

        private void OnCheckAcknowledgeTimeoutTick(object o)
        {
            using (EneterTrace.Entering())
            {
                bool aTimerContinuesFlag;

                List<string> aTimeoutedMessages = new List<string>();

                DateTime aCurrentTime = DateTime.Now;

                lock (mySentMessages)
                {
                    // Remove messages that did not get the acknowledge within the defined timeout.
                    mySentMessages.RemoveWhere(x =>
                        {
                            if (aCurrentTime - x.SendTime > myAcknowledgeTimeout)
                            {
                                // Store the removed message id.
                                aTimeoutedMessages.Add(x.MessageId);

                                // Indicate that the message can be removed
                                return true;
                            }

                            // Indicate, the message cannot be removed.
                            return false;
                        });

                    // If there are still some messages, then the timer continues.
                    // Note: The timer will be set at the end of this method - after notifications.
                    aTimerContinuesFlag = mySentMessages.Count > 0;
                }

                // Notify the removed messages.
                foreach (string aMessageId in aTimeoutedMessages)
                {
                    if (MessageNotDelivered != null)
                    {
                        try
                        {
                            MessageIdEventArgs aMsg = new MessageIdEventArgs(aMessageId);
                            MessageNotDelivered(this, aMsg);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }

                // If there are still some sent messages, then continue in checking of the timeout.
                if (aTimerContinuesFlag)
                {
                    myAcknowledgeTimer.Change(500, Timeout.Infinite);
                }
            }
        }

        private void NotifyMessageDelivered(string messageId)
        {
            using (EneterTrace.Entering())
            {
                if (MessageDelivered != null)
                {
                    try
                    {
                        MessageIdEventArgs aMsg = new MessageIdEventArgs(messageId);
                        MessageDelivered(this, aMsg);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private object myConnectionManipulatorLock = new object();
        private ISerializer mySerializer;
        
        private HashSet<SentMessageItem> mySentMessages = new HashSet<SentMessageItem>();

        private Timer myAcknowledgeTimer;
        private TimeSpan myAcknowledgeTimeout;

        private string TracedObject
        {
            get
            {
                string aChannelId = (UnderlyingDuplexOutputChannel != null) ? UnderlyingDuplexOutputChannel.ChannelId : "";
                return "Reliable duplex output channel '" + aChannelId + "' ";
            }
        }

    }
}
