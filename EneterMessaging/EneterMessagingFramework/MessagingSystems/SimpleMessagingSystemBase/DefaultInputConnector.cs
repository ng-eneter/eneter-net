/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using System.Collections.Generic;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultInputConnector : IInputConnector
    {
        public DefaultInputConnector(string inputConnectorAddress, IMessagingProvider messagingProvider, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myMessagingProvider = messagingProvider;
                myProtocolFormatter = protocolFormatter;
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

                using (ThreadLock.Lock(myListenerManipulatorLock))
                {
                    try
                    {
                        myMessageHandler = messageHandler;
                        myMessagingProvider.RegisterMessageHandler(myInputConnectorAddress, OnRequestMessageReceived);
                        myIsListeningFlag = true;
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
                using (ThreadLock.Lock(myConnectedClients))
                {
                    foreach (string anOutputConnectorAddress in myConnectedClients)
                    {
                        SendCloseConnection(anOutputConnectorAddress);
                    }

                    myConnectedClients.Clear();
                }

                using (ThreadLock.Lock(myListenerManipulatorLock))
                {
                    myIsListeningFlag = false;
                    myMessagingProvider.UnregisterMessageHandler(myInputConnectorAddress);
                    myMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (ThreadLock.Lock(myListenerManipulatorLock))
                {
                    return myIsListeningFlag;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                    myMessagingProvider.SendMessage(outputConnectorAddress, anEncodedMessage);
                }
                catch
                {
                    CloseConnection(outputConnectorAddress, true);
                    throw;
                }
            }
        }

        public void SendBroadcast(object message)
        {
            using (EneterTrace.Entering())
            {
                List<string> aDisconnectedClients = new List<string>();

                using (ThreadLock.Lock(myConnectedClients))
                {
                    // Send the response message to all connected clients.
                    foreach (string aResponseReceiverId in myConnectedClients)
                    {
                        try
                        {
                            // Send the response message.
                            SendResponseMessage(aResponseReceiverId, message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                            aDisconnectedClients.Add(aResponseReceiverId);

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

        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                CloseConnection(outputConnectorAddress, false);
            }
        }


        private void OnRequestMessageReceived(object message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(message);
                if (aProtocolMessage != null)
                {
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        bool aNewConnectionAddedFlag = false;

                        // If the connection is not open yet.
                        using (ThreadLock.Lock(myConnectedClients))
                        {
                            aNewConnectionAddedFlag = myConnectedClients.Add(aProtocolMessage.ResponseReceiverId);
                        }

                        if (aNewConnectionAddedFlag)
                        {
                            NotifyMessageContext(aMessageContext);
                        }
                    }
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        bool aConnectionRemovedFlag = false;
                        using (ThreadLock.Lock(myConnectedClients))
                        {
                            aConnectionRemovedFlag = myConnectedClients.Remove(aProtocolMessage.ResponseReceiverId);
                        }

                        if (aConnectionRemovedFlag)
                        {
                            NotifyMessageContext(aMessageContext);
                        }
                    }
                    else
                    {
                        NotifyMessageContext(aMessageContext);
                    }
                }
            }
        }

        private void CloseConnection(string outputConnectorAddress, bool notifyFlag)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectedClients))
                {
                    myConnectedClients.Remove(outputConnectorAddress);
                }

                SendCloseConnection(outputConnectorAddress);

                if (notifyFlag)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, outputConnectorAddress, null);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                    NotifyMessageContext(aMessageContext);
                }
            }
        }

        private void SendCloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress);
                    myMessagingProvider.SendMessage(outputConnectorAddress, anEncodedMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                }
            }
        }

        private void NotifyMessageContext(MessageContext messageContext)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    Action<MessageContext> aMessageHandler = myMessageHandler;
                    if (aMessageHandler != null)
                    {
                        aMessageHandler(messageContext);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }


        private string myInputConnectorAddress;
        private IMessagingProvider myMessagingProvider;
        private IProtocolFormatter myProtocolFormatter;
        private bool myIsListeningFlag;
        private Action<MessageContext> myMessageHandler;
        private object myListenerManipulatorLock = new object();
        private HashSet<string> myConnectedClients = new HashSet<string>();

        private string TracedObject { get { return GetType().Name; } }
    }
}
