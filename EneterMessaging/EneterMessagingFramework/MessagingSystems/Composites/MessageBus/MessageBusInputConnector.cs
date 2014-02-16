﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    internal class MessageBusInputConnector : IInputConnector
    {
        private class ResponseSender : ISender
        {
            public ResponseSender(IDuplexOutputChannel messageBusOutputChannel)
            {
                using (EneterTrace.Entering())
                {
                    myMessageBusOutputChannel = messageBusOutputChannel;
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
                    myMessageBusOutputChannel.SendMessage(message);
                }
            }

            public void SendMessage(Action<Stream> toStreamWritter)
            {
                throw new NotSupportedException();
            }

            private IDuplexOutputChannel myMessageBusOutputChannel;
        }

        public MessageBusInputConnector(IDuplexOutputChannel messageBusOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myMessageBusOutputChannel = messageBusOutputChannel;
                myResponseSender = new ResponseSender(myMessageBusOutputChannel);
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
                    myMessageBusOutputChannel.ConnectionClosed += OnConnectionWithMessageBusClosed;

                    // Open the connection with the service.
                    // Note: the response receiver id of this output channel represents the service id inside the message bus.
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
                    myMessageBusOutputChannel.ConnectionClosed -= OnConnectionWithMessageBusClosed;
                }

                myMessageHandler = null;
            }
        }

        public bool IsListening
        {
            get { return myMessageBusOutputChannel.IsConnected; }
        }

        public ISender CreateResponseSender(string clientId)
        {
            throw new NotSupportedException("CreateResponseSender is not supported.");
        }

        private void OnMessageFromMessageBusReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (myMessageHandler != null)
                {
                    try
                    {
                        MessageContext aMessageContext = new MessageContext(e.Message, e.SenderAddress, myResponseSender);
                        myMessageHandler(aMessageContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void OnConnectionWithMessageBusClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (myMessageHandler != null)
                {
                    try
                    {
                        myMessageHandler(null);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private ISender myResponseSender;
        private IDuplexOutputChannel myMessageBusOutputChannel;
        private Func<MessageContext, bool> myMessageHandler;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}