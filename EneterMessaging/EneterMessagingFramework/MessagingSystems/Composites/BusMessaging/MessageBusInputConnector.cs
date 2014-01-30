/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    internal class MessageBusInputConnector : IInputConnector
    {
        private class ResponseSender : ISender
        {
            public ResponseSender(string responseReceiverId, IDuplexOutputChannel messageBusOutputChannel, IProtocolFormatter protocolFormatter)
            {
                using (EneterTrace.Entering())
                {
                    myResponseReceiverId = responseReceiverId;
                    myMessageBusOutputChannel = messageBusOutputChannel;
                    myProtocolFormatter = protocolFormatter;
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
                    object anEncodedRequestMessage = myProtocolFormatter.EncodeMessage(myResponseReceiverId, message);
                    myMessageBusOutputChannel.SendMessage(anEncodedRequestMessage);
                }
            }

            public void SendMessage(Action<Stream> toStreamWritter)
            {
                throw new NotSupportedException();
            }

            private string myResponseReceiverId;
            private IDuplexOutputChannel myMessageBusOutputChannel;
            private IProtocolFormatter myProtocolFormatter;
        }

        public MessageBusInputConnector(string inputConnectorAddress, IDuplexOutputChannel messageBusOutputChannel, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myServiceAddressInMessageBus = inputConnectorAddress;
                myMessageBusOutputChannel = messageBusOutputChannel;
                myProtocolFormatter = protocolFormatter;
            }
        }

        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myMessageHandler = messageHandler;
                    myMessageBusOutputChannel.ResponseMessageReceived += OnMessageFromMessageBusReceived;
                    myMessageBusOutputChannel.OpenConnection();
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
                if (myMessageBusOutputChannel != null)
                {
                    myMessageBusOutputChannel.CloseConnection();
                    myMessageBusOutputChannel.ResponseMessageReceived -= OnMessageFromMessageBusReceived;
                }

                myMessageHandler = null;
            }
        }

        public bool IsListening
        {
            get { return myMessageBusOutputChannel.IsConnected; }
        }

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            using (EneterTrace.Entering())
            {
                ResponseSender aResponseSender = new ResponseSender(responseReceiverAddress, myMessageBusOutputChannel, myProtocolFormatter);
                return aResponseSender;
            }
        }

        private void OnMessageFromMessageBusReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                MessageContext aMessageContext = new MessageContext(e.Message, e.SenderAddress, null);
                if (myMessageHandler != null)
                {
                    try
                    {
                        myMessageHandler(aMessageContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private string myServiceAddressInMessageBus;
        private IDuplexOutputChannel myMessageBusOutputChannel;
        private IProtocolFormatter myProtocolFormatter;
        private Func<MessageContext, bool> myMessageHandler;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
