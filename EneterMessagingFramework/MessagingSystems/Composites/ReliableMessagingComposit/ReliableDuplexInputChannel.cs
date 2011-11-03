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
    internal class ReliableDuplexInputChannel : IReliableDuplexInputChannel
    {
        public event EventHandler<MessageIdEventArgs> ResponseMessageDelivered;

        public event EventHandler<MessageIdEventArgs> ResponseMessageNotDelivered;

        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public ReliableDuplexInputChannel(IDuplexInputChannel underlyingDuplexInputChannel, ISerializer serializer, TimeSpan acknowledgeTimeout)
        {
            using (EneterTrace.Entering())
            {
                UnderlyingDuplexInputChannel = underlyingDuplexInputChannel;
                mySerializer = serializer;
                myAcknowledgeTimeout = acknowledgeTimeout;

                myAcknowledgeTimer = new Timer(OnCheckAcknowledgeTimeoutTick);
                myAcknowledgeTimer.Change(-1, -1);
            }
        }

        public string ChannelId { get { return UnderlyingDuplexInputChannel.ChannelId; } }
        

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    UnderlyingDuplexInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
                    UnderlyingDuplexInputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                    UnderlyingDuplexInputChannel.MessageReceived += OnMessageReceived;

                    try
                    {
                        UnderlyingDuplexInputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);

                        UnderlyingDuplexInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                        UnderlyingDuplexInputChannel.ResponseReceiverDisconnected -= OnResponseReceiverDisconnected;
                        UnderlyingDuplexInputChannel.MessageReceived -= OnMessageReceived;

                        throw;
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
                    // Try to stop listening.
                    try
                    {
                        UnderlyingDuplexInputChannel.StopListening();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                    }

                    UnderlyingDuplexInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                    UnderlyingDuplexInputChannel.ResponseReceiverDisconnected -= OnResponseReceiverDisconnected;
                    UnderlyingDuplexInputChannel.MessageReceived -= OnMessageReceived;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return UnderlyingDuplexInputChannel.IsListening;
                }
            }
        }
        

        public string SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
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

                    UnderlyingDuplexInputChannel.SendResponseMessage(responseReceiverId, aSerializedMessage);
                }
                catch (Exception err)
                {
                    // Remove the message from the list of sent messages.
                    lock (mySentMessages)
                    {
                        mySentMessages.RemoveWhere(x => x.MessageId == aMessageId);
                    }

                    // Remove the message from sent messages.
                    EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);
                    throw;
                }


                return aMessageId;
            }
        }

        void IDuplexInputChannel.SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                this.SendResponseMessage(responseReceiverId, message);
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    UnderlyingDuplexInputChannel.DisconnectResponseReceiver(responseReceiverId);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    throw;
                }
            }
        }

        public IDuplexInputChannel UnderlyingDuplexInputChannel { get; private set; }


        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
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

        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
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
                        // Remove the message from the list of sent messages waiting for the acknowledgement.
                        lock (mySentMessages)
                        {
                            mySentMessages.RemoveWhere(x => x.MessageId == aReceivedMessage.MessageId);
                        }

                        if (ResponseMessageDelivered != null)
                        {
                            try
                            {
                                MessageIdEventArgs aMsg = new MessageIdEventArgs(aReceivedMessage.MessageId);
                                ResponseMessageDelivered(this, aMsg);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            // The received message is a regular message.
                            // Send the acknowledgement back to the sender.
                            ReliableMessage anAcknowledgeMessage = new ReliableMessage(aReceivedMessage.MessageId);
                            object aSerializedAcknowledgeMessage = mySerializer.Serialize<ReliableMessage>(anAcknowledgeMessage);
                            UnderlyingDuplexInputChannel.SendResponseMessage(e.ResponseReceiverId, aSerializedAcknowledgeMessage);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + "failed to send the acknowledge message.", err);
                        }

                        // Notify the message to the subscriber.
                        if (MessageReceived != null)
                        {
                            try
                            {
                                DuplexChannelMessageEventArgs aMsg = new DuplexChannelMessageEventArgs(e.ChannelId, aReceivedMessage.Message, e.ResponseReceiverId);
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
                            using (EneterTrace.Entering())
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
                            }
                        });

                    // If there are still some messages, then the timer continues.
                    // Note: The timer will be set at the end of this method - after notifications.
                    aTimerContinuesFlag = mySentMessages.Count > 0;
                }

                // Notify the removed messages.
                foreach (string aMessageId in aTimeoutedMessages)
                {
                    if (ResponseMessageNotDelivered != null)
                    {
                        try
                        {
                            MessageIdEventArgs aMsg = new MessageIdEventArgs(aMessageId);
                            ResponseMessageNotDelivered(this, aMsg);
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



        private object myListeningManipulatorLock = new object();
        private ISerializer mySerializer;
        
        private HashSet<SentMessageItem> mySentMessages = new HashSet<SentMessageItem>();

        private Timer myAcknowledgeTimer;
        private TimeSpan myAcknowledgeTimeout;

        private string TracedObject
        {
            get
            {
                string aChannelId = (UnderlyingDuplexInputChannel != null) ? UnderlyingDuplexInputChannel.ChannelId : "";
                return "Reliable duplex input channel '" + aChannelId + "' ";
            }
        }
    }
}
