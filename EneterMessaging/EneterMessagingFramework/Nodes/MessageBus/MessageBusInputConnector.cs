/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Nodes.Broker;

namespace Eneter.Messaging.Nodes.MessageBus
{
    internal class MessageBusInputConnector : IInputConnector
    {
        private class ResponseSender : ISender
        {
            public ResponseSender(string responseReceiverId, IDuplexBrokerClient brokerClient)
            {
                using (EneterTrace.Entering())
                {
                    myResponseReceiverId = responseReceiverId;
                    myBrokerClient = brokerClient;
                }
            }

            public bool IsStreamWritter
            {
                get { return false; }
            }

            public void SendMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    myBrokerClient.SendMessage(myResponseReceiverId, message);
                }
            }

            public void SendMessage(Action<Stream> toStreamWritter)
            {
                throw new NotSupportedException();
            }

            private string myResponseReceiverId;
            private IDuplexBrokerClient myBrokerClient;
        }

        public MessageBusInputConnector(string inputConnectorAddress, IDuplexBrokerClient brokerClient, IDuplexOutputChannel brokerOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myBrokerClient = brokerClient;
                myBrokerClientOutputChannel = brokerOutputChannel;
            }
        }

        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myMessageHandler = messageHandler;
                    myBrokerClient.BrokerMessageReceived += OnBrokerMessageReceived;
                    myBrokerClient.AttachDuplexOutputChannel(myBrokerClientOutputChannel);
                    myBrokerClient.Subscribe(myInputConnectorAddress);
                }
                catch
                {
                    StopListening();
                    throw;
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                if (myBrokerClient != null)
                {
                    try
                    {
                        myBrokerClient.Unsubscribe();
                    }
                    catch
                    {
                    }

                    myBrokerClient.DetachDuplexOutputChannel();
                    myBrokerClient.BrokerMessageReceived -= OnBrokerMessageReceived;
                    myMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get { return myBrokerClient.IsDuplexOutputChannelAttached && myBrokerClientOutputChannel.IsConnected; }
        }

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            using (EneterTrace.Entering())
            {
                ResponseSender aResponseSender = new ResponseSender(responseReceiverAddress, myBrokerClient);
                return aResponseSender;
            }
        }


        private void OnBrokerMessageReceived(object sender, BrokerMessageReceivedEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (e.ReceivingError != null)
                {
                    EneterTrace.Warning(TracedObject + "detected error when receiving the message from the message bus.", e.ReceivingError);
                    return;
                }

                MessageContext aMessageContext = new MessageContext(e.Message, "", null);
                if (myMessageHandler != null)
                {
                    myMessageHandler(aMessageContext);
                }
            }
        }


        private string myInputConnectorAddress;
        private IDuplexBrokerClient myBrokerClient;
        private IDuplexOutputChannel myBrokerClientOutputChannel;
        private Func<MessageContext, bool> myMessageHandler;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
