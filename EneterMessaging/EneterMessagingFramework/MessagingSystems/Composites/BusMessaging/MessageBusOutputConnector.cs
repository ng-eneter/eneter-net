/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Nodes.Broker;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    internal class MessageBusOutputConnector : IOutputConnector
    {
        public MessageBusOutputConnector(string serviceAddressInMessageBus, string clientIdInMessageBus,
            IDuplexOutputChannel messageBusOutputChannel, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myServiceAddressInMessageBus = serviceAddressInMessageBus;
                myClientIdInMessageBus = clientIdInMessageBus;
                myMessageBusOutputChannel = messageBusOutputChannel;
                myProtocolFormatter = protocolFormatter;
            }
        }

        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulator)
                {
                    try
                    {
                        myResponseMessageHandler = responseMessageHandler;
                        myMessageBusOutputChannel.ResponseMessageReceived += OnMessageFromMessageBusReceived;
                        myMessageBusOutputChannel.OpenConnection();

                        object anOpenConnectionMessage = myProtocolFormatter.EncodeMessage(myClientIdInMessageBus, myServiceAddressInMessageBus);
                        myMessageBusOutputChannel.SendMessage(anOpenConnectionMessage);
                    }
                    catch
                    {
                        CloseConnection();
                        throw;
                    }
                }
            }
        }


        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulator)
                {
                    if (myMessageBusOutputChannel != null)
                    {
                        myMessageBusOutputChannel.CloseConnection();
                        myMessageBusOutputChannel.ResponseMessageReceived -= OnMessageFromMessageBusReceived;
                    }

                    myResponseMessageHandler = null;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulator)
                {
                    return myMessageBusOutputChannel.IsConnected;
                }
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
                object anEncodedMessage = myProtocolFormatter.EncodeMessage(myClientIdInMessageBus, message);
                myMessageBusOutputChannel.SendMessage(anEncodedMessage);
            }
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException();
        }


        private void OnMessageFromMessageBusReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (myResponseMessageHandler != null)
                {
                    try
                    {
                        MessageContext aMessageContext = new MessageContext(e.Message, e.SenderAddress, null);
                        myResponseMessageHandler(aMessageContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private IDuplexOutputChannel myMessageBusOutputChannel;
        private IProtocolFormatter myProtocolFormatter;
        private string myServiceAddressInMessageBus;
        private string myClientIdInMessageBus;
        private Func<MessageContext, bool> myResponseMessageHandler;
        private object myConnectionManipulator = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
