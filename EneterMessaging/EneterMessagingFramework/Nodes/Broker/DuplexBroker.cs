/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eneter.Messaging.Nodes.Broker
{
    internal class DuplexBroker : AttachableDuplexInputChannelBase, IDuplexBroker
    {
        private class TSubscription
        {
            public TSubscription(string messageTypeId, string receiverId)
            {
                MessageTypeId = messageTypeId;
                ReceiverId = receiverId;
            }

            // MessageTypeId or regular expression used to recognize matching message type id.
            public string MessageTypeId { get; private set; }
            public string ReceiverId { get; private set; }
        }

        public event EventHandler<PublishInfoEventArgs> MessagePublished;
        public event EventHandler<SubscribeInfoEventArgs> ClientSubscribed;
        public event EventHandler<SubscribeInfoEventArgs> ClientUnsubscribed;
        public event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        public DuplexBroker(bool isPublisherNotified, ISerializer serializer,
            AuthorizeBrokerRequestCallback validateBrokerRequestCallback)
        {
            using (EneterTrace.Entering())
            {
                if (serializer == null)
                {
                    string anError = "Input parameter serializer is null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                myIsPublisherSelfnotified = isPublisherNotified;
                mySerializer = serializer;
                myValidateBrokerRequestCallback = validateBrokerRequestCallback;
            }
        }

        public void SendMessage(string eventId, object serializedMessage)
        {
            using (EneterTrace.Entering())
            {
                BrokerMessage aNotifyMessage = new BrokerMessage(eventId, serializedMessage);

                // If one serializer is used for the whole communication then pre-serialize the message to increase the performance.
                // If there is SerializerProvider callback then the serialization must be performed before sending individualy
                // for each client.
                object aSerializedNotifyMessage = null;
                if (mySerializer.IsSameForAllResponseReceivers())
                {
                    aSerializedNotifyMessage = mySerializer.Serialize<BrokerMessage>(aNotifyMessage);
                }

                Publish(myLocalReceiverId, aNotifyMessage, aSerializedNotifyMessage);
            }
        }

        public void Subscribe(string eventId)
        {
            using (EneterTrace.Entering())
            {
                string[] aEventsToSubscribe = { eventId };
                Subscribe(myLocalReceiverId, aEventsToSubscribe);
            }
        }

        public void Subscribe(string[] eventIds)
        {
            using (EneterTrace.Entering())
            {
                Subscribe(myLocalReceiverId, eventIds);
            }
        }

        public void Unsubscribe(string eventId)
        {
            using (EneterTrace.Entering())
            {
                string[] aEventsToUnsubscribe = { eventId };
                IEnumerable<string> anUnsubscribedMessages = Unsubscribe(myLocalReceiverId, aEventsToUnsubscribe);
                RaiseClientUnsubscribed(myLocalReceiverId, anUnsubscribedMessages);
            }
        }

        public void Unsubscribe(string[] eventIds)
        {
            using (EneterTrace.Entering())
            {
                IEnumerable<string> anUnsubscribedMessages = Unsubscribe(myLocalReceiverId, eventIds);
                RaiseClientUnsubscribed(myLocalReceiverId, anUnsubscribedMessages);
            }
        }

        public void Unsubscribe()
        {
            using (EneterTrace.Entering())
            {
                IEnumerable<string> anUnsubscribedMessages = Unsubscribe(myLocalReceiverId, null);
                RaiseClientUnsubscribed(myLocalReceiverId, anUnsubscribedMessages);
            }
        }

        public IEnumerable<string> GetSubscribedMessages(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(mySubscribtions))
                {
                    string[] aResult = mySubscribtions.Where(x => x.ReceiverId == responseReceiverId).Select(y => y.MessageTypeId).ToArray();
                    return aResult;
                }
            }
        }

        public IEnumerable<string> GetSubscribedResponseReceivers(string messageTypeId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(mySubscribtions))
                {
                    string[] aResult = mySubscribtions.Where(x => x.MessageTypeId == messageTypeId).Select(y => y.ReceiverId).ToArray();
                    return aResult;
                }
            }
        }

        protected override void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Try to deserialize the message.
                BrokerMessage aBrokerMessage;
                try
                {
                    aBrokerMessage = mySerializer.ForResponseReceiver(e.ResponseReceiverId).Deserialize<BrokerMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize the message.", err);
                    return;
                }

                if (myValidateBrokerRequestCallback != null)
                {
                    bool isValidated = false;
                    try
                    {
                        isValidated = myValidateBrokerRequestCallback(e.ResponseReceiverId, aBrokerMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                    }

                    if (!isValidated)
                    {
                        IEnumerable<string> anUnsubscribedMessages = Unsubscribe(e.ResponseReceiverId, null);
                        RaiseClientUnsubscribed(e.ResponseReceiverId, anUnsubscribedMessages);

                        try
                        {
                            AttachedDuplexInputChannel.DisconnectResponseReceiver(e.ResponseReceiverId);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to disconnect response receiver.", err);
                        }
                        return;
                    }
                }

                if (aBrokerMessage.Request == EBrokerRequest.Publish)
                {
                    if (mySerializer.IsSameForAllResponseReceivers())
                    {
                        // If only one serializer is used for communication with all clients then
                        // increase the performance by reusing already serialized message.
                        Publish(e.ResponseReceiverId, aBrokerMessage, e.Message);
                    }
                    else
                    {
                        // If there is a serializer per client then the message must be serialized
                        // individually for each subscribed client.
                        Publish(e.ResponseReceiverId, aBrokerMessage, null);
                    }
                }
                else if (aBrokerMessage.Request == EBrokerRequest.Subscribe)
                {
                    Subscribe(e.ResponseReceiverId, aBrokerMessage.MessageTypes);
                }
                else if (aBrokerMessage.Request == EBrokerRequest.Unsubscribe)
                {
                    IEnumerable<string> anUnsubscribedMessages = Unsubscribe(e.ResponseReceiverId, aBrokerMessage.MessageTypes);
                    RaiseClientUnsubscribed(e.ResponseReceiverId, anUnsubscribedMessages);
                }
                else if (aBrokerMessage.Request == EBrokerRequest.UnsubscribeAll)
                {
                    IEnumerable<string> anUnsubscribedMessages = Unsubscribe(e.ResponseReceiverId, null);
                    RaiseClientUnsubscribed(e.ResponseReceiverId, anUnsubscribedMessages);
                }
            }
        }

        protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            // n.a.
        }

        protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                IEnumerable<string> anUnsubscribedMessages = Unsubscribe(e.ResponseReceiverId, null);
                RaiseClientUnsubscribed(e.ResponseReceiverId, anUnsubscribedMessages);
            }
        }

        private void Publish(string publisherResponseReceiverId, BrokerMessage message, object originalSerializedMessage)
        {
            using (EneterTrace.Entering())
            {
                List<TSubscription> anIdetifiedSubscriptions = new List<TSubscription>();

                using (ThreadLock.Lock(mySubscribtions))
                {
                    foreach (TSubscription aMessageSubscription in mySubscribtions)
                    {
                        if ((myIsPublisherSelfnotified || aMessageSubscription.ReceiverId != publisherResponseReceiverId) &&
                            aMessageSubscription.MessageTypeId == message.MessageTypes[0])
                        {
                            anIdetifiedSubscriptions.Add(aMessageSubscription);
                        }
                    }
                }

                Dictionary<string, IEnumerable<string>> aFailedSubscribers = new Dictionary<string, IEnumerable<string>>();
                int aNumberOfSentSubscribers = 0;
                foreach (TSubscription aSubscription in anIdetifiedSubscriptions)
                {
                    if (aSubscription.ReceiverId == myLocalReceiverId)
                    {
                        if (BrokerMessageReceived != null)
                        {
                            try
                            {
                                BrokerMessageReceivedEventArgs anEvent = new BrokerMessageReceivedEventArgs(message.MessageTypes[0], message.Message);
                                BrokerMessageReceived(this, anEvent);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }

                            ++aNumberOfSentSubscribers;
                        }
                    }
                    else
                    {
                        object aSerializedMessage = originalSerializedMessage;
                        if (aSerializedMessage == null)
                        {
                            try
                            {
                                ISerializer aSerializer = mySerializer.ForResponseReceiver(aSubscription.ReceiverId);
                                aSerializedMessage = aSerializer.Serialize<BrokerMessage>(message);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Error(TracedObject + "failed to serialize BrokerMessage using GetSerializeCallback.", err);
                            }
                        }

                        if (aSerializedMessage != null)
                        {
                            IEnumerable<string> anUnsubscribedMessagesDueToFailure = Send(aSubscription.ReceiverId, aSerializedMessage);
                            if (anUnsubscribedMessagesDueToFailure.Any())
                            {
                                aFailedSubscribers[aSubscription.ReceiverId] = anUnsubscribedMessagesDueToFailure;
                            }
                            else
                            {
                                ++aNumberOfSentSubscribers;
                            }
                        }
                    }
                }

                if (MessagePublished != null)
                {
                    PublishInfoEventArgs anEvent = new PublishInfoEventArgs(publisherResponseReceiverId, message.MessageTypes[0], message.Message, aNumberOfSentSubscribers);
                    try
                    {
                        MessagePublished(this, anEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }

                // If sending to some subscribers failed then they were unsubscribed.
                foreach(KeyValuePair<string, IEnumerable<string>> aFailedSubscriber in aFailedSubscribers)
                {
                    RaiseClientUnsubscribed(aFailedSubscriber.Key, aFailedSubscriber.Value);
                }
            }
        }

        private IEnumerable<string> Send(string responseReceiverId, object serializedMessage)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedDuplexInputChannel == null)
                {
                    string anErrorMessage = TracedObject + "failed to send the message because the it is not attached to duplex input channel.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                try
                {
                    AttachedDuplexInputChannel.SendResponseMessage(responseReceiverId, serializedMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send the message. The client will be disconnected and unsubscribed from all messages.", err);

                    try
                    {
                        // Try to disconnect the client.
                        AttachedDuplexInputChannel.DisconnectResponseReceiver(responseReceiverId);
                    }
                    catch
                    {
                    }

                    // Unsubscribe the failed client.
                    return Unsubscribe(responseReceiverId, null);
                }

                return new string[0];
            }
        }

        private void Subscribe(string responseReceiverId, string[] messageTypes)
        {
            using (EneterTrace.Entering())
            {
                List<string> aMessagesToSubscribe = new List<string>(messageTypes);

                using (ThreadLock.Lock(mySubscribtions))
                {
                    // Subscribe only messages that are not subscribed yet.
                    foreach (TSubscription aSubscription in mySubscribtions)
                    {
                        if (aSubscription.ReceiverId == responseReceiverId &&
                            aMessagesToSubscribe.Contains(aSubscription.MessageTypeId))
                        {
                            aMessagesToSubscribe.Remove(aSubscription.MessageTypeId);
                        }
                    }

                    // Subscribe
                    foreach (string aMessageType in aMessagesToSubscribe)
                    {
                        mySubscribtions.Add(new TSubscription(aMessageType, responseReceiverId));
                    }
                }

                if (ClientSubscribed != null && aMessagesToSubscribe.Count > 0)
                {
                    SubscribeInfoEventArgs anEvent = new SubscribeInfoEventArgs(responseReceiverId, aMessagesToSubscribe);
                    try
                    {
                        ClientSubscribed(this, anEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private IEnumerable<string> Unsubscribe(string responseReceiverId, string[] messageTypes)
        {
            using (EneterTrace.Entering())
            {
                List<string> anUnsubscribedMessages = new List<string>();
                using (ThreadLock.Lock(mySubscribtions))
                {
                    // If unsubscribe from all messages
                    if (messageTypes == null || messageTypes.Length == 0)
                    {
                        mySubscribtions.RemoveWhere(x =>
                            {
                                if (x.ReceiverId == responseReceiverId)
                                {
                                    anUnsubscribedMessages.Add(x.MessageTypeId);
                                    return true;
                                }

                                return false;
                            });
                    }
                    // If unsubscribe from specified messages
                    else
                    {
                        mySubscribtions.RemoveWhere(x =>
                            {
                                if (x.ReceiverId == responseReceiverId && messageTypes.Any(y => x.MessageTypeId == y))
                                {
                                    anUnsubscribedMessages.Add(x.MessageTypeId);
                                    return true;
                                }

                                return false;
                            });
                    }
                }

                return anUnsubscribedMessages;
            }
        }

        private void RaiseClientUnsubscribed(string responseReceiverId, IEnumerable<string> messageTypeIds)
        {
            using (EneterTrace.Entering())
            {
                if (ClientUnsubscribed != null && messageTypeIds != null && messageTypeIds.Any())
                {
                    SubscribeInfoEventArgs anEvent = new SubscribeInfoEventArgs(responseReceiverId, messageTypeIds);
                    try
                    {
                        ClientUnsubscribed(this, anEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private HashSet<TSubscription> mySubscribtions = new HashSet<TSubscription>();
        private bool myIsPublisherSelfnotified;
        private ISerializer mySerializer;
        private AuthorizeBrokerRequestCallback myValidateBrokerRequestCallback;

        private readonly string myLocalReceiverId = "Eneter.Broker.LocalReceiver";

        protected override string TracedObject { get { return GetType().Name + " "; } }

    }
}
