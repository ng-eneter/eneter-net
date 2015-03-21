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
    internal class ReliableDuplexTypedMessageReceiver<_ResponseType, _RequestType> : IReliableTypedMessageReceiver<_ResponseType, _RequestType>
    {
        public event EventHandler<TypedRequestReceivedEventArgs<_RequestType>> MessageReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        public event EventHandler<ReliableMessageIdEventArgs> ResponseMessageDelivered;

        public event EventHandler<ReliableMessageIdEventArgs> ResponseMessageNotDelivered;


        public ReliableDuplexTypedMessageReceiver(TimeSpan messageAcknowledgeTimeout, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myTimeTracker = new ReliableMessageTimeTracker(messageAcknowledgeTimeout);
                myTimeTracker.TrackingTimeout += OnTrackingTimeout;

                mySerializer = serializer;

                IDuplexTypedMessagesFactory aReceiverFactory = new DuplexTypedMessagesFactory(serializer);
                myReceiver = aReceiverFactory.CreateDuplexTypedMessageReceiver<ReliableMessage, ReliableMessage>();
                myReceiver.MessageReceived += OnMessageReceived;
                myReceiver.ResponseReceiverConnected += OnResponseReceiverConnected;
                myReceiver.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
            }
        }

        public void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                myReceiver.AttachDuplexInputChannel(duplexInputChannel);
            }
        }

        public void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                myReceiver.DetachDuplexInputChannel();
            }
        }

        public bool IsDuplexInputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myReceiver.IsDuplexInputChannelAttached;
                }
            }
        }

        public IDuplexInputChannel AttachedDuplexInputChannel
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myReceiver.AttachedDuplexInputChannel;
                }
            }
        }

        public string SendResponseMessage(string responseReceiverId, _ResponseType responseMessage)
        {
            using (EneterTrace.Entering())
            {
                // Create identifier for the reliable message.
                string aMessageId = Guid.NewGuid().ToString();

                try
                {
                    // Serialize the response message.
                    object aSerializedResponseMessage = mySerializer.Serialize<_ResponseType>(responseMessage);

                    // Create reliable message.
                    ReliableMessage aReliableMessage = new ReliableMessage(aMessageId, aSerializedResponseMessage);

                    // Create tracking of the response message.
                    // (it will check whether the receiving of the message was acknowledged in the specified timeout.)
                    myTimeTracker.AddTracking(aMessageId);

                    // Send the response message.
                    myReceiver.SendResponseMessage(responseReceiverId, aReliableMessage);

                    return aMessageId;
                }
                catch (Exception err)
                {
                    // Remove the message from the tracking.
                    myTimeTracker.RemoveTracking(aMessageId);

                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                    throw;
                }
            }
        }

        private void OnMessageReceived(object sender, TypedRequestReceivedEventArgs<ReliableMessage> e)
        {
            using (EneterTrace.Entering())
            {
                // If no error during receiving the response message.
                if (e.ReceivingError == null)
                {
                    // If it is an acknowledge that a response message was delivered.
                    if (e.RequestMessage.MessageType == ReliableMessage.EMessageType.Acknowledge)
                    {
                        // The acknowledge was delivered so we can remove tracking.
                        myTimeTracker.RemoveTracking(e.RequestMessage.MessageId);

                        // Notify that response message was received.
                        NotifyMessageDeliveryStatus(ResponseMessageDelivered, e.RequestMessage.MessageId);
                    }
                    // If it is a request message.
                    else
                    {
                        // Send back the acknowledge that the request message was delivered.
                        try
                        {
                            ReliableMessage anAcknowledge = new ReliableMessage(e.RequestMessage.MessageId);
                            myReceiver.SendResponseMessage(e.ResponseReceiverId, anAcknowledge);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + "failed to send the acknowledge message.", err);
                        }

                        // Notify the request message was received.
                        if (MessageReceived != null)
                        {
                            TypedRequestReceivedEventArgs<_RequestType> aMsg = null;

                            try
                            {
                                // Deserialize the incoming request message.
                                _RequestType aRequestMessage = mySerializer.Deserialize<_RequestType>(e.RequestMessage.Message);
                                aMsg = new TypedRequestReceivedEventArgs<_RequestType>(e.ResponseReceiverId, e.SenderAddress, aRequestMessage);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + "failed to deserialize the request message.", err);
                                aMsg = new TypedRequestReceivedEventArgs<_RequestType>(e.ResponseReceiverId, e.SenderAddress, err);
                            }

                            try
                            {
                                MessageReceived(this, aMsg);
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
                    if (MessageReceived != null)
                    {
                        TypedRequestReceivedEventArgs<_RequestType> aMsg = new TypedRequestReceivedEventArgs<_RequestType>(e.ResponseReceiverId, e.SenderAddress, e.ReceivingError);

                        try
                        {
                            MessageReceived(this, aMsg);
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

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                NotifyConnectionStatus(ResponseReceiverDisconnected, e);
            }
        }

        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                NotifyConnectionStatus(ResponseReceiverConnected, e);
            }
        }

        private void OnTrackingTimeout(object sender, ReliableMessageIdEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // The acknowledgement was not received in the specified timeout so notify
                // that the response message was not delivered.
                NotifyMessageDeliveryStatus(ResponseMessageNotDelivered, e.MessageId);
            }
        }

        private void NotifyConnectionStatus(EventHandler<ResponseReceiverEventArgs> handler, ResponseReceiverEventArgs e)
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
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
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


        private ISerializer mySerializer;
        private IDuplexTypedMessageReceiver<ReliableMessage, ReliableMessage> myReceiver;
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
