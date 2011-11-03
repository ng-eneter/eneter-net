/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.Infrastructure.ConnectionProvider;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Nodes.ChannelWrapper;
using System.Text.RegularExpressions;

namespace Eneter.Messaging.Nodes.Broker
{
    internal class DuplexBrokerClient : IDuplexBrokerClient
    {
        public event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        public DuplexBrokerClient(IConnectionProvider connectionProvider,
                            IChannelWrapperFactory channelWrapperFactory,
                            IDuplexTypedMessagesFactory typedRequestResponseFactory)
        {
            using (EneterTrace.Entering())
            {
                myDuplexChannelWrapper = channelWrapperFactory.CreateDuplexChannelWrapper();

                myBrokerRequestSender = typedRequestResponseFactory.CreateDuplexTypedMessageSender<BrokerNotifyMessage, BrokerRequestMessage>();
                myBrokerRequestSender.ResponseReceived += OnBrokerMessageReceived;
                connectionProvider.Connect(myDuplexChannelWrapper, myBrokerRequestSender, "BrokerRequestChannel");

                myBrokerMessagesSender = typedRequestResponseFactory.CreateDuplexTypedMessageSender<bool, BrokerNotifyMessage>();
                connectionProvider.Connect(myDuplexChannelWrapper, myBrokerMessagesSender, "BrokerMessageChannel");
            }
        }


        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myDuplexChannelWrapper.AttachDuplexOutputChannel(duplexOutputChannel);
                    myDuplexOutputChannelId = duplexOutputChannel.ChannelId;
                }
                catch (Exception err)
                {
                    string aChannelId = (duplexOutputChannel != null) ? duplexOutputChannel.ChannelId : string.Empty;
                    EneterTrace.Error(TracedObject + "failed to attach the duplex output channel '" + duplexOutputChannel.ChannelId + "' and open connection.", err);
                    throw;
                }
            }
        }

        public void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myDuplexChannelWrapper.DetachDuplexOutputChannel();
                    myDuplexOutputChannelId = "";
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to detach duplex output channel.", err);
                    throw;
                }
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myDuplexChannelWrapper.IsDuplexOutputChannelAttached;
                }
            }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myDuplexChannelWrapper.AttachedDuplexOutputChannel;
                }
            }
        }

        public void SendMessage(string messageTypeId, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    BrokerNotifyMessage aBrokerMessage = new BrokerNotifyMessage(messageTypeId, message);
                    myBrokerMessagesSender.SendRequestMessage(aBrokerMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send the message to the broker.", err);
                    throw;
                }
            }
        }

        public void Subscribe(string messageType)
        {
            using (EneterTrace.Entering())
            {
                string[] aMessageType = { messageType };
                SendRequest(EBrokerRequest.Subscribe, aMessageType);
            }
        }


        public void Subscribe(string[] messageTypes)
        {
            using (EneterTrace.Entering())
            {
                SendRequest(EBrokerRequest.Subscribe, messageTypes);
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

                SendRequest(EBrokerRequest.SubscribeRegExp, regularExpressions);
            }
        }

        public void Unsubscribe(string messageType)
        {
            using (EneterTrace.Entering())
            {
                string[] aMessageType = { messageType };
                SendRequest(EBrokerRequest.Unsubscribe, aMessageType);
            }
        }

        public void Unsubscribe(string[] messageTypes)
        {
            using (EneterTrace.Entering())
            {
                SendRequest(EBrokerRequest.Unsubscribe, messageTypes);
            }
        }

        public void UnsubscribeRegExp(string regularExpression)
        {
            using (EneterTrace.Entering())
            {
                string[] aRegularExpression = { regularExpression };
                SendRequest(EBrokerRequest.UnsubscribeRegExp, aRegularExpression);
            }
        }

        public void UnsubscribeRegExp(string[] regularExpressions)
        {
            using (EneterTrace.Entering())
            {
                SendRequest(EBrokerRequest.UnsubscribeRegExp, regularExpressions);
            }
        }

        public void Unsubscribe()
        {
            using (EneterTrace.Entering())
            {
                // Unsubscribe from all messages.
                string[] anEmpty = new string[0];
                SendRequest(EBrokerRequest.UnsubscribeAll, anEmpty);
            }
        }


        private void OnBrokerMessageReceived(object sender, TypedResponseReceivedEventArgs<BrokerNotifyMessage> e)
        {
            using (EneterTrace.Entering())
            {
                if (BrokerMessageReceived != null)
                {
                    try
                    {
                        BrokerMessageReceivedEventArgs anEvent = null;

                        if (e.ReceivingError == null)
                        {
                            anEvent = new BrokerMessageReceivedEventArgs(e.ResponseMessage.MessageTypeId, e.ResponseMessage.Message);
                        }
                        else
                        {
                            anEvent = new BrokerMessageReceivedEventArgs(e.ReceivingError);
                        }

                        BrokerMessageReceived(this, anEvent);
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


        private void SendRequest(EBrokerRequest request, string[] messageTypes)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    BrokerRequestMessage aBrokerRequestMessage = new BrokerRequestMessage(request, messageTypes);
                    myBrokerRequestSender.SendRequestMessage(aBrokerRequestMessage);
                }
                catch (Exception err)
                {
                    string anError = (request == EBrokerRequest.Subscribe || request == EBrokerRequest.SubscribeRegExp) ?
                        TracedObject + "failed to subscribe in the Broker." :
                        TracedObject + "failed to unsubscribe in the Broker.";

                    EneterTrace.Error(anError, err);
                    throw;
                }
            }
        }

        private IDuplexChannelWrapper myDuplexChannelWrapper;

        private IDuplexTypedMessageSender<BrokerNotifyMessage, BrokerRequestMessage> myBrokerRequestSender;
        private IDuplexTypedMessageSender<bool, BrokerNotifyMessage> myBrokerMessagesSender;

        private string myDuplexOutputChannelId = "";

        private string TracedObject
        {
            get
            {
                return "The DuplexBrokerClient atached to the duplex output channel '" + myDuplexOutputChannelId + "' ";
            }
        }
    }
}
