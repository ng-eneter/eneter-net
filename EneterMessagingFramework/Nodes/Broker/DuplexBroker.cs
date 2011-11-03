/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.Infrastructure.ConnectionProvider;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Nodes.ChannelWrapper;
using System.Text.RegularExpressions;

namespace Eneter.Messaging.Nodes.Broker
{
    internal class DuplexBroker : IDuplexBroker
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

        public DuplexBroker(IConnectionProvider connectionProvider,
                      IChannelWrapperFactory channelWrapperFactory,
                      IDuplexTypedMessagesFactory typedRequestResponseFactory)
        {
            using (EneterTrace.Entering())
            {
                myDuplexChannelUnwrapper = channelWrapperFactory.CreateDuplexChannelUnwrapper(connectionProvider.MessagingSystem);

                myBrokerRequestReceiver = typedRequestResponseFactory.CreateDuplexTypedMessageReceiver<BrokerNotifyMessage, BrokerRequestMessage>();
                myBrokerRequestReceiver.MessageReceived += OnBrokerRequestReceived;
                myBrokerRequestReceiver.ResponseReceiverDisconnected += OnSubscriberDisconnected;
                connectionProvider.Attach(myBrokerRequestReceiver, "BrokerRequestChannel");

                myBrokerMessagesReceiver = typedRequestResponseFactory.CreateDuplexTypedMessageReceiver<bool, BrokerNotifyMessage>();
                myBrokerMessagesReceiver.MessageReceived += OnBrokerMessageReceived;
                connectionProvider.Attach(myBrokerMessagesReceiver, "BrokerMessageChannel");
            }
        }

        public void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myDuplexChannelUnwrapper.AttachDuplexInputChannel(duplexInputChannel);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to attach duplex input channel '" + duplexInputChannel.ChannelId + "'.", err);
                    throw;
                }

                myDuplexInputChannelId = duplexInputChannel.ChannelId;
            }
        }

        public void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                myDuplexInputChannelId = "";

                try
                {
                    myDuplexChannelUnwrapper.DetachDuplexInputChannel();
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to detach duplex input channel.", err);
                }

                myDuplexChannelUnwrapper.ResponseReceiverDisconnected -= OnSubscriberDisconnected;
            }
        }

        public bool IsDuplexInputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myDuplexChannelUnwrapper.IsDuplexInputChannelAttached;
                }
            }
        }

        public IDuplexInputChannel AttachedDuplexInputChannel { get { return myDuplexChannelUnwrapper.AttachedDuplexInputChannel; } }


        private void OnBrokerRequestReceived(object sender, TypedRequestReceivedEventArgs<BrokerRequestMessage> e)
        {
            using (EneterTrace.Entering())
            {
                if (e.ReceivingError != null)
                {
                    EneterTrace.Error(TracedObject + "detected an error during receiving a request to subscribe a client.", e.ReceivingError);
                    return;
                }

                lock (mySubscribtionManipulatorLock)
                {
                    if (e.RequestMessage.Request == EBrokerRequest.Subscribe)
                    {
                        Subscribe(e.ResponseReceiverId, e.RequestMessage.MessageTypes, myMessageSubscribtions);
                    }
                    else if (e.RequestMessage.Request == EBrokerRequest.SubscribeRegExp)
                    {
                        Subscribe(e.ResponseReceiverId, e.RequestMessage.MessageTypes, myRegExpSubscribtions);
                    }
                    else if (e.RequestMessage.Request == EBrokerRequest.Unsubscribe)
                    {
                        Unsubscribe(e.ResponseReceiverId, e.RequestMessage.MessageTypes, myMessageSubscribtions);
                    }
                    else if (e.RequestMessage.Request == EBrokerRequest.UnsubscribeRegExp)
                    {
                        Unsubscribe(e.ResponseReceiverId, e.RequestMessage.MessageTypes, myRegExpSubscribtions);
                    }
                    else if (e.RequestMessage.Request == EBrokerRequest.UnsubscribeAll)
                    {
                        Unsubscribe(e.ResponseReceiverId, null, myMessageSubscribtions);
                        Unsubscribe(e.ResponseReceiverId, null, myRegExpSubscribtions);
                    }
                }
            }
        }

        private void OnBrokerMessageReceived(object sender, TypedRequestReceivedEventArgs<BrokerNotifyMessage> e)
        {
            using (EneterTrace.Entering())
            {
                if (e.ReceivingError != null)
                {
                    EneterTrace.Error(TracedObject + "detected an error during receiving a message that should be forwarded to subscribed clients.", e.ReceivingError);
                    return;
                }

                lock (mySubscribtionManipulatorLock)
                {
                    List<TSubscriptionItem> anIncorrectRegExpCollector = new List<TSubscriptionItem>();

                    IEnumerable<TSubscriptionItem> aMessageSubscribers = myMessageSubscribtions.Where(x => x.MessageTypeId == e.RequestMessage.MessageTypeId);
                    IEnumerable<TSubscriptionItem> aRegExpSubscribers = myRegExpSubscribtions.Where(x =>
                        {
                            using (EneterTrace.Entering())
                            {
                                try
                                {
                                    return Regex.IsMatch(e.RequestMessage.MessageTypeId, x.MessageTypeId);
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
                        });

                    // Remove subscriptions with regular expresions causing exceptions.
                    anIncorrectRegExpCollector.ForEach(x => myRegExpSubscribtions.Remove(x));

                    IEnumerable<TSubscriptionItem> aSubscribers = aMessageSubscribers.Concat(aRegExpSubscribers);
                    if (aSubscribers != null)
                    {
                        foreach (TSubscriptionItem aSubscriber in aSubscribers)
                        {
                            try
                            {
                                myBrokerRequestReceiver.SendResponseMessage(aSubscriber.ReceiverId, e.RequestMessage);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + "failed to send a message to the subscriber '" + aSubscriber.ReceiverId + "'", err);
                            }
                        }
                    }
                }
            }
        }

        private void OnSubscriberDisconnected(object sender, ResponseReceiverEventArgs e)
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

        private void Subscribe(string responseReceiverId, string[] messageTypes, HashSet<TSubscriptionItem> subscribtions)
        {
            foreach (string aMessageType in messageTypes)
            {
                subscribtions.Add(new TSubscriptionItem(aMessageType, responseReceiverId));
            }
        }

        private void Unsubscribe(string responseReceiverId, string[] messageTypes, HashSet<TSubscriptionItem> subscribtions)
        {
            // If unsubscribe from all messages
            if (messageTypes == null || messageTypes.Length == 0)
            {
                subscribtions.RemoveWhere(x => x.ReceiverId == responseReceiverId);
            }
            // If unsubscribe from specified messages
            else
            {
                foreach (string aMessageType in messageTypes)
                {
                    subscribtions.RemoveWhere(x => x.ReceiverId == responseReceiverId && x.MessageTypeId == aMessageType);
                }
            }
        }


        private IDuplexChannelUnwrapper myDuplexChannelUnwrapper;

        // Receives requests to subscribe or unsubscribe.
        private IDuplexTypedMessageReceiver<BrokerNotifyMessage, BrokerRequestMessage> myBrokerRequestReceiver;

        // Receive messages to be forwarded to subscribers.
        private IDuplexTypedMessageReceiver<bool, BrokerNotifyMessage> myBrokerMessagesReceiver;

        private object mySubscribtionManipulatorLock = new object();

        private HashSet<TSubscriptionItem> myMessageSubscribtions = new HashSet<TSubscriptionItem>();

        private HashSet<TSubscriptionItem> myRegExpSubscribtions = new HashSet<TSubscriptionItem>();


        private string myDuplexInputChannelId = "";

        private string TracedObject
        {
            get
            {
                return "The Broker atached to the duplex input channel '" + myDuplexInputChannelId + "' ";
            }
        }
    }
}
