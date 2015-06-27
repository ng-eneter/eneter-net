/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

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

        public event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        public DuplexBroker(bool isPublisherNotified, ISerializer serializer, GetSerializerCallback getSerializerCallback)
        {
            using (EneterTrace.Entering())
            {
                myIsPublisherSelfnotified = isPublisherNotified;
                mySerializer = serializer;
                myGetSerializerCallback = getSerializerCallback;
            }
        }

        public void SendMessage(string eventId, object serializedMessage)
        {
            using (EneterTrace.Entering())
            {
                BrokerMessage aNotifyMessage = new BrokerMessage(eventId, serializedMessage);

                // If one serializer is used for the whole commeunication then pre-serialize the message to increase the performance.
                // If there is SerializerProvider callback then the serialization must be performed before sending individualy
                // for each client.
                object aSerializedNotifyMessage = (myGetSerializerCallback == null) ?
                    mySerializer.Serialize<BrokerMessage>(aNotifyMessage) :
                    null;

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
                Unsubscribe(myLocalReceiverId, aEventsToUnsubscribe);
            }
        }

        public void Unsubscribe(string[] eventIds)
        {
            using (EneterTrace.Entering())
            {
                Unsubscribe(myLocalReceiverId, eventIds);
            }
        }

        public void Unsubscribe()
        {
            using (EneterTrace.Entering())
            {
                Unsubscribe(myLocalReceiverId, null);
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
                    ISerializer aSerializer = (myGetSerializerCallback == null) ? mySerializer : myGetSerializerCallback(e.ResponseReceiverId);
                    aBrokerMessage = aSerializer.Deserialize<BrokerMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize the message.", err);
                    return;
                }


                if (aBrokerMessage.Request == EBrokerRequest.Publish)
                {
                    if (myGetSerializerCallback == null)
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
                    Unsubscribe(e.ResponseReceiverId, aBrokerMessage.MessageTypes);
                }
                else if (aBrokerMessage.Request == EBrokerRequest.UnsubscribeAll)
                {
                    Unsubscribe(e.ResponseReceiverId, null);
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
                Unsubscribe(e.ResponseReceiverId, null);
            }
        }

        private void Publish(string publisherResponseReceiverId, BrokerMessage message, object originalSerializedMessage)
        {
            using (EneterTrace.Entering())
            {
                List<TSubscription> anIdetifiedSubscriptions = new List<TSubscription>();

                lock (mySubscribtions)
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
                        }
                    }
                    else
                    {
                        object aSerializedMessage = originalSerializedMessage;
                        if (aSerializedMessage == null)
                        {
                            try
                            {
                                ISerializer aSerializer = myGetSerializerCallback(aSubscription.ReceiverId);
                                aSerializedMessage = aSerializer.Serialize<BrokerMessage>(message);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Error(TracedObject + "failed to serialize BrokerMessage using GetSerializeCallback.", err);
                            }
                        }

                        if (aSerializedMessage != null)
                        {
                            Send(aSubscription.ReceiverId, aSerializedMessage);
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
                    Unsubscribe(responseReceiverId, null);
                }
            }
        }

        private void Subscribe(string responseReceiverId, string[] messageTypes)
        {
            using (EneterTrace.Entering())
            {
                lock (mySubscribtions)
                {
                    // Subscribe only messages that are not subscribed yet.
                    List<string> aMessagesToSubscribe = new List<string>(messageTypes);
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
            }
        }

        private void Unsubscribe(string responseReceiverId, string[] messageTypes)
        {
            using (EneterTrace.Entering())
            {
                lock (mySubscribtions)
                {
                    // If unsubscribe from all messages
                    if (messageTypes == null || messageTypes.Length == 0)
                    {
                        mySubscribtions.RemoveWhere(x => x.ReceiverId == responseReceiverId);
                    }
                    // If unsubscribe from specified messages
                    else
                    {
                        mySubscribtions.RemoveWhere(x => x.ReceiverId == responseReceiverId && messageTypes.Any(y => x.MessageTypeId == y));
                    }
                }
            }
        }


        private HashSet<TSubscription> mySubscribtions = new HashSet<TSubscription>();
        private bool myIsPublisherSelfnotified;
        private ISerializer mySerializer;
        private GetSerializerCallback myGetSerializerCallback;

        private readonly string myLocalReceiverId = "Eneter.Broker.LocalReceiver";

        protected override string TracedObject { get { return GetType().Name + " "; } }

    }
}
