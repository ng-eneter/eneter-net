/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    internal class MessageBusInputConnector : IInputConnector
    {
        public MessageBusInputConnector(IProtocolFormatter protocolFormatter, IDuplexOutputChannel messageBusOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;
                myMessageBusOutputChannel = messageBusOutputChannel;
            }
        }

        public void StartListening(Action<MessageContext> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                if (messageHandler == null)
                {
                    throw new ArgumentNullException("messageHandler is null.");
                }

                lock (myListeningManipulatorLock)
                {
                    try
                    {
                        myMessageHandler = messageHandler;
                        myMessageBusOutputChannel.ResponseMessageReceived += OnMessageFromMessageBusReceived;

                        // Register service in the message bus.
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
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    myMessageBusOutputChannel.CloseConnection();
                    myMessageBusOutputChannel.ResponseMessageReceived -= OnMessageFromMessageBusReceived;
                    myMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListeningManipulatorLock)
                {
                    return myMessageBusOutputChannel.IsConnected;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                object anEncodedMessage = myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                myMessageBusOutputChannel.SendMessage(anEncodedMessage);
            }
        }

        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress);
                myMessageBusOutputChannel.SendMessage(anEncodedMessage);
            }
        }

        private void OnMessageFromMessageBusReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(e.Message);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, e.SenderAddress);

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


        private IProtocolFormatter myProtocolFormatter;
        private IDuplexOutputChannel myMessageBusOutputChannel;
        private Action<MessageContext> myMessageHandler;
        private object myListeningManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
