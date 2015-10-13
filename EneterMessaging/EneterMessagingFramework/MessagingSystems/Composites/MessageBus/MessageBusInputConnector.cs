/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;
using System;
using System.Collections.Generic;

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

                using (ThreadLock.Lock(myListeningManipulatorLock))
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
                using (ThreadLock.Lock(myListeningManipulatorLock))
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
                using (ThreadLock.Lock(myListeningManipulatorLock))
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

        public void SendBroadcast(object message)
        {
            using (EneterTrace.Entering())
            {
                List<string> aDisconnectedClients = new List<string>();

                using (ThreadLock.Lock(myConnectedClients))
                {
                    foreach (string aClientId in myConnectedClients.Keys)
                    {
                        try
                        {
                            // Send the response message.
                            SendResponseMessage(aClientId, message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                            aDisconnectedClients.Add(aClientId);

                            // Note: Exception is not rethrown because if sending to one client fails it should not
                            //       affect sending to other clients.
                        }
                    }
                }

                // Disconnect failed clients.
                foreach (String anOutputConnectorAddress in aDisconnectedClients)
                {
                    CloseConnection(anOutputConnectorAddress, true);
                }
            }
        }

        public void CloseConnection(string clientId)
        {
            using (EneterTrace.Entering())
            {
                CloseConnection(clientId, false);
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

                    IThreadDispatcher aDispatcher = myThreadDispatcherProvider.GetDispatcher();
                    using (ThreadLock.Lock(myConnectedClients))
                    {
                        myConnectedClients[aProtocolMessage.ResponseReceiverId] = aDispatcher;
                    }

                    aDispatcher.Invoke(() => NotifyMessageContext(aMessageContext));
                }
                else if (aMessageBusMessage.Request == EMessageBusRequest.DisconnectClient)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, aMessageBusMessage.Id, aMessageBusMessage.MessageData);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, e.SenderAddress);

                    IThreadDispatcher aDispatcher;
                    using (ThreadLock.Lock(myConnectedClients))
                    {
                        myConnectedClients.TryGetValue(aProtocolMessage.ResponseReceiverId, out aDispatcher);
                        if (aDispatcher != null)
                        {
                            myConnectedClients.Remove(aProtocolMessage.ResponseReceiverId);
                        }
                        else
                        {
                            aDispatcher = myThreadDispatcherProvider.GetDispatcher();
                        }
                    }
                    
                    aDispatcher.Invoke(() => NotifyMessageContext(aMessageContext));
                }
                else if (aMessageBusMessage.Request == EMessageBusRequest.SendRequestMessage)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, aMessageBusMessage.Id, aMessageBusMessage.MessageData);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, e.SenderAddress);

                    IThreadDispatcher aDispatcher;
                    using (ThreadLock.Lock(myConnectedClients))
                    {
                        myConnectedClients.TryGetValue(aProtocolMessage.ResponseReceiverId, out aDispatcher);
                        if (aDispatcher == null)
                        {
                            aDispatcher = myThreadDispatcherProvider.GetDispatcher();
                            myConnectedClients[aProtocolMessage.ResponseReceiverId] = aDispatcher;
                        }
                    }

                    aDispatcher.Invoke(() => NotifyMessageContext(aMessageContext));
                }
            }
        }

        private void CloseConnection(string clientId, bool notifyFlag)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    MessageBusMessage aMessage = new MessageBusMessage(EMessageBusRequest.DisconnectClient, clientId, null);
                    object aSerializedMessage = mySerializer.Serialize<MessageBusMessage>(aMessage);

                    myMessageBusOutputChannel.SendMessage(aSerializedMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                }

                if (notifyFlag)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, clientId, null);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

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

        private IThreadDispatcherProvider myThreadDispatcherProvider = new SyncDispatching();
        private Dictionary<string, IThreadDispatcher> myConnectedClients = new Dictionary<string, IThreadDispatcher>();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
