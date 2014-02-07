﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using System.IO;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    internal class MessageBusInputConnector : IInputConnector
    {
        private class ResponseSender : ISender
        {
            public ResponseSender(string clientId, IDuplexOutputChannel messageBusOutputChannel)
            {
                using (EneterTrace.Entering())
                {
                    myClientId = clientId;
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

            private string myClientId;
            private IDuplexOutputChannel myMessageBusOutputChannel;
        }

        public MessageBusInputConnector(IDuplexOutputChannel messageBusOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myMessageBusOutputChannel = messageBusOutputChannel;
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
                    myStartListeningConfirmed.Reset();
                    myMessageBusOutputChannel.OpenConnection();

                    if (!myStartListeningConfirmed.WaitOne(5000))
                    {
                        throw new TimeoutException(TracedObject + "failed to start the listening within the timeout.");
                    }
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
            using (EneterTrace.Entering())
            {
                ResponseSender aResponseSender = new ResponseSender(clientId, myMessageBusOutputChannel);
                return aResponseSender;
            }
        }

        private void OnMessageFromMessageBusReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // If it is a confirmation message that the connection was really open.
                if (e.Message is string && ((string)e.Message) == "OK")
                {
                    // Indicate the listening is started and relase the waiting in the OpenConnection().
                    myStartListeningConfirmed.Set();
                }
                else if (myMessageHandler != null)
                {
                    try
                    {
                        MessageContext aMessageContext = new MessageContext(e.Message, e.SenderAddress, null);
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
                // In case the StartListening() is waiting until the listening is started relase it.
                myStartListeningConfirmed.Set();

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


        private IDuplexOutputChannel myMessageBusOutputChannel;
        private Func<MessageContext, bool> myMessageHandler;
        private ManualResetEvent myStartListeningConfirmed = new ManualResetEvent(false);

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
