﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Text.RegularExpressions;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Broker
{
    internal class DuplexBrokerClient : AttachableDuplexOutputChannelBase, IDuplexBrokerClient
    {
        public event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        public DuplexBrokerClient(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        public void SendMessage(string eventId, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    BrokerMessage aBrokerMessage = new BrokerMessage(eventId, message);
                    Send(aBrokerMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send the message to the broker.", err);
                    throw;
                }
            }
        }

        public void Subscribe(string eventId)
        {
            using (EneterTrace.Entering())
            {
                string[] aMessageType = { eventId };
                Subscribe(aMessageType);
            }
        }


        public void Subscribe(string[] eventIds)
        {
            using (EneterTrace.Entering())
            {
                if (eventIds == null)
                {
                    string anErrorMessage = TracedObject + "cannot subscribe to null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new ArgumentNullException(anErrorMessage);
                }
            
                // Check input items.
                foreach (String anInputItem in eventIds)
                {
                    if (string.IsNullOrEmpty(anInputItem))
                    {
                        string anErrorMessage = TracedObject + "cannot subscribe to null.";
                        EneterTrace.Error(anErrorMessage);
                        throw new ArgumentException(anErrorMessage);
                    }
                }

                Send(EBrokerRequest.Subscribe, eventIds);
            }
        }

        public void SubscribeRegExp(string regularExpression)
        {
            using (EneterTrace.Entering())
            {
                string[] aRegularExpression = { regularExpression };
                SubscribeRegExp(aRegularExpression);
            }
        }

        public void SubscribeRegExp(string[] regularExpressions)
        {
            using (EneterTrace.Entering())
            {
                if (regularExpressions == null)
                {
                    string anErrorMessage = TracedObject + "cannot subscribe to null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new ArgumentNullException(anErrorMessage);
                }

                // Check if the client has a correct regular expression.
                // If not, then the exception will be thrown here on the client side and not in the broker during the evaluation.
                foreach (string aRegExpression in regularExpressions)
                {
                    try
                    {
                        Regex.IsMatch("", aRegExpression);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed to subscribe the regular expression because the regular expression '"
                            + aRegExpression + "' is incorrect.", err);
                        throw;
                    }
                }

                Send(EBrokerRequest.SubscribeRegExp, regularExpressions);
            }
        }

        public void Unsubscribe(string eventId)
        {
            using (EneterTrace.Entering())
            {
                string[] aMessageType = { eventId };
                Unsubscribe(aMessageType);
            }
        }

        public void Unsubscribe(string[] eventIds)
        {
            using (EneterTrace.Entering())
            {
                Send(EBrokerRequest.Unsubscribe, eventIds);
            }
        }

        public void UnsubscribeRegExp(string regularExpression)
        {
            using (EneterTrace.Entering())
            {
                string[] aRegularExpression = { regularExpression };
                UnsubscribeRegExp(aRegularExpression);
            }
        }

        public void UnsubscribeRegExp(string[] regularExpressions)
        {
            using (EneterTrace.Entering())
            {
                Send(EBrokerRequest.UnsubscribeRegExp, regularExpressions);
            }
        }

        public void Unsubscribe()
        {
            using (EneterTrace.Entering())
            {
                // Unsubscribe from all messages.
                string[] anEmpty = new string[0];
                Send(EBrokerRequest.UnsubscribeAll, anEmpty);
            }
        }

        protected override void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (BrokerMessageReceived == null)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                    return;
                }

                BrokerMessageReceivedEventArgs anEvent = null;
                try
                {
                    BrokerMessage aMessage = mySerializer.Deserialize<BrokerMessage>(e.Message);
                    anEvent = new BrokerMessageReceivedEventArgs(aMessage.MessageTypes[0], aMessage.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to deserialize the request message.", err);
                    anEvent = new BrokerMessageReceivedEventArgs(err);
                }

                try
                {
                    BrokerMessageReceived(this, anEvent);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        private void Send(EBrokerRequest request, string[] messageTypes)
        {
            using (EneterTrace.Entering())
            {
                BrokerMessage aBrokerMessage = new BrokerMessage(request, messageTypes);
                Send(aBrokerMessage);
            }
        }

        private void Send(BrokerMessage message)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedDuplexOutputChannel == null)
                {
                    string anError = TracedObject + "failed to send the message because it is not attached to any duplex output channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    object aSerializedMessage = mySerializer.Serialize<BrokerMessage>(message);
                    AttachedDuplexOutputChannel.SendMessage(aSerializedMessage);
                }
                catch (Exception err)
                {
                    string anError = TracedObject + "failed to send a message to the Broker.";
                    EneterTrace.Error(anError, err);
                    throw;
                }
            }
        }


        private ISerializer mySerializer;
        private string myDuplexOutputChannelId = "";


        protected override string TracedObject
        {
            get
            {
                return GetType().Name + " '" + myDuplexOutputChannelId + "' ";
            }
        }

    }
}
