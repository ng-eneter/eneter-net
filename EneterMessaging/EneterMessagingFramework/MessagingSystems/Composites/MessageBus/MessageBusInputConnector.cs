/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    internal class MessageBusInputConnector : IInputConnector
    {
        public MessageBusInputConnector(ISerializer serializer, IDuplexOutputChannel messageBusOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myServiceId = messageBusOutputChannel.ResponseReceiverId;
                mySerializer = serializer;
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

                        // Open connection with the message bus.
                        myMessageBusOutputChannel.OpenConnection();

                        // Register service in the message bus.
                        MessageBusMessage aMessage = new MessageBusMessage(EMessageBusRequest.RegisterService, myServiceId, null);
                        object aSerializedMessage = mySerializer.Serialize<MessageBusMessage>(aMessage);

                        myMessageBusOutputChannel.SendMessage(aSerializedMessage);
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

        public void SendResponseMessage(string clientId, object message)
        {
            using (EneterTrace.Entering())
            {
                MessageBusMessage aMessage = new MessageBusMessage(EMessageBusRequest.SendResponseMessage, clientId, message);
                object aSerializedMessage = mySerializer.Serialize<MessageBusMessage>(aMessage);

                myMessageBusOutputChannel.SendMessage(aSerializedMessage);
            }
        }

        public void CloseConnection(string clientId)
        {
            using (EneterTrace.Entering())
            {
                MessageBusMessage aMessage = new MessageBusMessage(EMessageBusRequest.DisconnectClient, clientId, null);
                object aSerializedMessage = mySerializer.Serialize<MessageBusMessage>(aMessage);

                myMessageBusOutputChannel.SendMessage(aSerializedMessage);
            }
        }

        private void OnMessageFromMessageBusReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                MessageBusMessage aMessageBusMessage;
                try
                {
                    aMessageBusMessage = mySerializer.Deserialize<MessageBusMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize message.", err);
                    return;
                }

                if (aMessageBusMessage.Request == EMessageBusRequest.ConnectClient)
                {
                    MessageBusMessage aResponseMessage = new MessageBusMessage(EMessageBusRequest.ConfirmClient, aMessageBusMessage.Id, null);
                    object aSerializedResponse = mySerializer.Serialize<MessageBusMessage>(aResponseMessage);
                    myMessageBusOutputChannel.SendMessage(aSerializedResponse);

                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.OpenConnectionRequest, aMessageBusMessage.Id, null);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, e.SenderAddress);
                    NotifyMessageContext(aMessageContext);
                }
                else if (aMessageBusMessage.Request == EMessageBusRequest.DisconnectClient)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, aMessageBusMessage.Id, aMessageBusMessage.MessageData);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, e.SenderAddress);
                    NotifyMessageContext(aMessageContext);
                }
                else if (aMessageBusMessage.Request == EMessageBusRequest.SendRequestMessage)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, aMessageBusMessage.Id, aMessageBusMessage.MessageData);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, e.SenderAddress);
                    NotifyMessageContext(aMessageContext);
                }
            }
        }

        private void NotifyMessageContext(MessageContext messageContext)
        {
            using (EneterTrace.Entering())
            {
                if (myMessageHandler != null)
                {
                    try
                    {
                        myMessageHandler(messageContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private string myServiceId;
        private ISerializer mySerializer;
        private IDuplexOutputChannel myMessageBusOutputChannel;
        private Action<MessageContext> myMessageHandler;
        private object myListeningManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
