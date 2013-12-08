/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Broker
{
    internal class DuplexBroker : AttachableDuplexInputChannelBase, IDuplexBroker
    {
        private class TSubscriptionItem
        {
            public TSubscriptionItem(string messageTypeId, string receiverId)
            {
                MessageTypeId = messageTypeId;
                ReceiverId = receiverId;
            }

            // MessageTypeId or regular expression used to recognize matching message type id.
            public string MessageTypeId { get; private set; }
            public string ReceiverId { get; private set; }
        }

        public event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        public DuplexBroker(bool isPublisherNotified, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myIsPublisherSelfnotified = isPublisherNotified;
                mySerializer = serializer;
            }
        }

        public void SendMessage(string eventId, object serializedMessage)
        {
            using (EneterTrace.Entering())
            {
                BrokerMessage aNotifyMessage = new BrokerMessage(eventId, serializedMessage);
                object aSerializedNotifyMessage = mySerializer.Serialize<BrokerMessage>(aNotifyMessage);
                Publish(myLocalReceiverId, aNotifyMessage, aSerializedNotifyMessage);
            }
        }

        public void Subscribe(string eventId)
        {
            using (EneterTrace.Entering())
            {
                string[] aEventsToSubscribe = { eventId };
                Subscribe(myLocalReceiverId, aEventsToSubscribe, myMessageSubscribtions);
            }
        }

        public void Subscribe(string[] eventIds)
        {
            using (EneterTrace.Entering())
            {
                Subscribe(myLocalReceiverId, eventIds, myMessageSubscribtions);
            }
        }

        public void Unsubscribe(string eventId)
        {
            using (EneterTrace.Entering())
            {
                string[] aEventsToUnsubscribe = { eventId };
                Unsubscribe(myLocalReceiverId, aEventsToUnsubscribe, myMessageSubscribtions);
            }
        }

        public void Unsubscribe(string[] eventIds)
        {
            using (EneterTrace.Entering())
            {
                Unsubscribe(myLocalReceiverId, eventIds, myMessageSubscribtions);
            }
        }

        public void SubscribeRegExp(string regularExpression)
        {
            using (EneterTrace.Entering())
            {
                string[] aSubscribingExpressions = { regularExpression };
                Subscribe(myLocalReceiverId, aSubscribingExpressions, myRegExpSubscribtions);
            }
        }

        public void SubscribeRegExp(string[] regularExpressions)
        {
            using (EneterTrace.Entering())
            {
                Subscribe(myLocalReceiverId, regularExpressions, myRegExpSubscribtions);
            }
        }


        public void UnsubscribeRegExp(string regularExpression)
        {
            using (EneterTrace.Entering())
            {
                string[] aUnsubscribingExpressions = { regularExpression };
                Unsubscribe(myLocalReceiverId, aUnsubscribingExpressions, myRegExpSubscribtions);
            }
        }

        public void UnsubscribeRegExp(string[] regularExpressions)
        {
            using (EneterTrace.Entering())
            {
                Unsubscribe(myLocalReceiverId, regularExpressions, myRegExpSubscribtions);
            }
        }

        public void Unsubscribe()
        {
            using (EneterTrace.Entering())
            {
                lock (mySubscribtionManipulatorLock)
                {
                    Unsubscribe(myLocalReceiverId, null, myMessageSubscribtions);
                    Unsubscribe(myLocalReceiverId, null, myRegExpSubscribtions);
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
                    aBrokerMessage = mySerializer.Deserialize<BrokerMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize the message.", err);
                    return;
                }


                if (aBrokerMessage.Request == EBrokerRequest.Publish)
                {
                    Publish(e.ResponseReceiverId, aBrokerMessage, e.Message);
                }
                else if (aBrokerMessage.Request == EBrokerRequest.Subscribe)
                {
                    Subscribe(e.ResponseReceiverId, aBrokerMessage.MessageTypes, myMessageSubscribtions);
                }
                else if (aBrokerMessage.Request == EBrokerRequest.SubscribeRegExp)
                {
                    Subscribe(e.ResponseReceiverId, aBrokerMessage.MessageTypes, myRegExpSubscribtions);
                }
                else if (aBrokerMessage.Request == EBrokerRequest.Unsubscribe)
                {
                    Unsubscribe(e.ResponseReceiverId, aBrokerMessage.MessageTypes, myMessageSubscribtions);
                }
                else if (aBrokerMessage.Request == EBrokerRequest.UnsubscribeRegExp)
                {
                    Unsubscribe(e.ResponseReceiverId, aBrokerMessage.MessageTypes, myRegExpSubscribtions);
                }
                else if (aBrokerMessage.Request == EBrokerRequest.UnsubscribeAll)
                {
                    lock (mySubscribtionManipulatorLock)
                    {
                        Unsubscribe(e.ResponseReceiverId, null, myMessageSubscribtions);
                        Unsubscribe(e.ResponseReceiverId, null, myRegExpSubscribtions);
                    }
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
                // Remove all subscriptions for the disconnected subscriber.
                lock (mySubscribtionManipulatorLock)
                {
                    Unsubscribe(e.ResponseReceiverId, null, myMessageSubscribtions);
                    Unsubscribe(e.ResponseReceiverId, null, myRegExpSubscribtions);
                }
            }
        }

        private void Publish(string publisherResponseReceiverId, BrokerMessage message, object originalSerializedMessage)
        {
            using (EneterTrace.Entering())
            {
                lock (mySubscribtionManipulatorLock)
                {
                    List<TSubscriptionItem> anIncorrectRegExpCollector = new List<TSubscriptionItem>();

                    IEnumerable<TSubscriptionItem> aMessageSubscribers = myMessageSubscribtions.Where(x => 
                        (myIsPublisherSelfnotified || x.ReceiverId != publisherResponseReceiverId) && x.MessageTypeId == message.MessageTypes[0]);
                    IEnumerable<TSubscriptionItem> aRegExpSubscribers = myRegExpSubscribtions.Where(x =>
                        {
                            if (myIsPublisherSelfnotified || x.ReceiverId != publisherResponseReceiverId)
                            {
                                try
                                {
                                    return Regex.IsMatch(message.MessageTypes[0], x.MessageTypeId);
                                }
                                catch (Exception err)
                                {
                                    // The regular expression provided by a client can be incorrect and can cause an exception.
                                    // Other clients should not be affected by this. Therefore, we catch the exception and remove the invalid expression.
                                    EneterTrace.Error(TracedObject + "detected an incorrect regular expression: " + x.MessageTypeId, err);

                                    // Store the subscribtion with the wrong expression.
                                    anIncorrectRegExpCollector.Add(x);

                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        });

                    // Remove subscriptions with regular expresions causing exceptions.
                    anIncorrectRegExpCollector.ForEach(x => myRegExpSubscribtions.Remove(x));

                    IEnumerable<TSubscriptionItem> aSubscribers = aMessageSubscribers.Concat(aRegExpSubscribers);
                    if (aSubscribers != null)
                    {
                        foreach (TSubscriptionItem aSubscriber in aSubscribers)
                        {
                            if (aSubscriber.ReceiverId == myLocalReceiverId)
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
                                }
                            }
                            else
                            {
                                Send(aSubscriber.ReceiverId, originalSerializedMessage);
                            }
                        }
                    }
                }
            }
        }

        private void Send(string responseReceiverId, object serializedMessage)
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
                    lock (myDuplexInputChannelManipulatorLock)
                    {
                        Unsubscribe(responseReceiverId, null, myMessageSubscribtions);
                        Unsubscribe(responseReceiverId, null, myRegExpSubscribtions);
                    }
                }
            }
        }

        private void Subscribe(string responseReceiverId, string[] messageTypes, HashSet<TSubscriptionItem> subscribtions)
        {
            using (EneterTrace.Entering())
            {
                lock (mySubscribtionManipulatorLock)
                {
                    // Subscribe only messages that are not subscribed yet.
                    List<string> aMessagesToSubscribe = new List<string>(messageTypes);
                    foreach (TSubscriptionItem aSubscription in subscribtions)
                    {
                        if (aSubscription.ReceiverId == responseReceiverId)
                        {
                            aMessagesToSubscribe.Remove(aSubscription.MessageTypeId);
                        }
                    }

                    // Subscribe
                    foreach (string aMessageType in aMessagesToSubscribe)
                    {
                        subscribtions.Add(new TSubscriptionItem(aMessageType, responseReceiverId));
                    }
                }
            }
        }

        private void Unsubscribe(string responseReceiverId, string[] messageTypes, HashSet<TSubscriptionItem> subscribtions)
        {
            using (EneterTrace.Entering())
            {
                lock (mySubscribtionManipulatorLock)
                {
                    // If unsubscribe from all messages
                    if (messageTypes == null || messageTypes.Length == 0)
                    {
                        subscribtions.RemoveWhere(x => x.ReceiverId == responseReceiverId);
                    }
                    // If unsubscribe from specified messages
                    else
                    {
                        subscribtions.RemoveWhere(x => x.ReceiverId == responseReceiverId && messageTypes.Any(y => x.MessageTypeId == y));
                    }
                }
            }
        }

        private object mySubscribtionManipulatorLock = new object();
        private HashSet<TSubscriptionItem> myMessageSubscribtions = new HashSet<TSubscriptionItem>();
        private HashSet<TSubscriptionItem> myRegExpSubscribtions = new HashSet<TSubscriptionItem>();
        private bool myIsPublisherSelfnotified;
        private ISerializer mySerializer;

        private readonly string myLocalReceiverId = "Eneter.Broker.LocalReceiver";
        
        protected override string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }

    }
}
