/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class ReliableDuplexTypedMessageSender<_ResponseType, _RequestType> : IReliableTypedMessageSender<_ResponseType, _RequestType>
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;
        public event EventHandler<TypedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;
        public event EventHandler<ReliableMessageIdEventArgs> MessageDelivered;
        public event EventHandler<ReliableMessageIdEventArgs> MessageNotDelivered;

        public ReliableDuplexTypedMessageSender(TimeSpan messageAcknowledgeTimeout, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myTimeTracker = new ReliableMessageTimeTracker(messageAcknowledgeTimeout);
                myTimeTracker.TrackingTimeout += OnTrackingTimeout;

                mySerializer = serializer;

                IDuplexTypedMessagesFactory aSenderFactory = new DuplexTypedMessagesFactory(serializer);
                mySender = aSenderFactory.CreateDuplexTypedMessageSender<ReliableMessage, ReliableMessage>();
                mySender.ConnectionOpened += OnConnectionOpened;
                mySender.ConnectionClosed += OnConnectionClosed;
                mySender.ResponseReceived += OnResponseReceived;
            }
        }


        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                mySender.AttachDuplexOutputChannel(duplexOutputChannel);
            }
        }

        public void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                mySender.DetachDuplexOutputChannel();
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return mySender.IsDuplexOutputChannelAttached;
                }
            }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return mySender.AttachedDuplexOutputChannel;
                }
            }
        }

        public string SendRequestMessage(_RequestType message)
        {
            using (EneterTrace.Entering())
            {
                // Create identifier for the reliable message.
                string aMessageId = Guid.NewGuid().ToString();

                try
                {
                    // Create tracking of the message.
                    // (it will check whether the receiving of the message was acknowledged in the specified timeout.)
                    myTimeTracker.AddTracking(aMessageId);

                    // Create the reliable message.
                    object aSerializedRequest = mySerializer.Serialize<_RequestType>(message);
                    ReliableMessage aReliableMessage = new ReliableMessage(aMessageId, aSerializedRequest);

                    // Send reliable message.
                    mySender.SendRequestMessage(aReliableMessage);

                    // Return id identifying the message.
                    return aMessageId;
                }
                catch (Exception err)
                {
                    // Remove the tracing.
                    myTimeTracker.RemoveTracking(aMessageId);

                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }


        private void OnResponseReceived(object sender, TypedResponseReceivedEventArgs<ReliableMessage> e)
        {
            using (EneterTrace.Entering())
            {
                // If no error during receiving the message.
                if (e.ReceivingError == null)
                {
                    // if it is an acknowledge that a request message was received.
                    if (e.ResponseMessage.MessageType == ReliableMessage.EMessageType.Acknowledge)
                    {
                        // The acknowledge was delivered so we can remove tracking.
                        myTimeTracker.RemoveTracking(e.ResponseMessage.MessageId);

                        // Notify the request message was received.
                        NotifyMessageDeliveryStatus(MessageDelivered, e.ResponseMessage.MessageId);
                    }
                    // If it is a response message.
                    else
                    {
                        // Send back the acknowledge that the response message was delivered.
                        try
                        {
                            ReliableMessage anAcknowledge = new ReliableMessage(e.ResponseMessage.MessageId);
                            mySender.SendRequestMessage(anAcknowledge);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + "failed to send the acknowledge message.", err);
                        }

                        // Notify the response message was received.
                        if (ResponseReceived != null)
                        {
                            TypedResponseReceivedEventArgs<_ResponseType> aMsg = null;

                            try
                            {
                                _ResponseType aResponseMessage = mySerializer.Deserialize<_ResponseType>(e.ResponseMessage.Message);
                                aMsg = new TypedResponseReceivedEventArgs<_ResponseType>(aResponseMessage);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + "failed to deserialize the response message.", err);
                                aMsg = new TypedResponseReceivedEventArgs<_ResponseType>(err);
                            }

                            try
                            {
                                ResponseReceived(this, aMsg);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                        else
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                        }
                    }
                }
                else
                {
                    // Notify that an error occured during receiving the message.
                    if (ResponseReceived != null)
                    {
                        try
                        {
                            TypedResponseReceivedEventArgs<_ResponseType> aMsg = new TypedResponseReceivedEventArgs<_ResponseType>(e.ReceivingError);
                            ResponseReceived(this, aMsg);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                    else
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                    }
                }
            }
        }

        private void OnTrackingTimeout(object sender, ReliableMessageIdEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // The acknowledgement was not received in the specified timeout so notify
                // that the request message was not delivered.
                NotifyMessageDeliveryStatus(MessageNotDelivered, e.MessageId);
            }
        }

        private void NotifyMessageDeliveryStatus(EventHandler<ReliableMessageIdEventArgs> handler, string messageId)
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        ReliableMessageIdEventArgs aMsg = new ReliableMessageIdEventArgs(messageId);
                        handler(this, aMsg);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionOpened, e);
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionClosed, e);
            }
        }

        private void Notify(EventHandler<DuplexChannelEventArgs> handler, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private ISerializer mySerializer;
        private IDuplexTypedMessageSender<ReliableMessage, ReliableMessage> mySender;
        private ReliableMessageTimeTracker myTimeTracker;

        protected string TracedObject
        {
            get
            {
                return GetType().Name + "<" + typeof(_ResponseType).Name + ", " + typeof(_RequestType).Name + "> ";
            }
        }
    }
}
