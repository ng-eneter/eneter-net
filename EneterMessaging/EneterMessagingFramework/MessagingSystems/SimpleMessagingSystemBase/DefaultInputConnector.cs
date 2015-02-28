/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

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
                lock (myListeningManipulatorLock)
                {
                    if (messageHandler == null)
                    {
                        throw new ArgumentNullException("messageHandler is null.");
                    }

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
                lock (myListeningManipulatorLock)
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
                lock (myListeningManipulatorLock)
                {
                    return myIsListeningFlag;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                object anEncodedMessage = myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                myMessagingProvider.SendMessage(outputConnectorAddress, anEncodedMessage);
            }
        }

        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress);
                myMessagingProvider.SendMessage(outputConnectorAddress, anEncodedMessage);
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

        private string myInputConnectorAddress;
        private IMessagingProvider myMessagingProvider;
        private IProtocolFormatter myProtocolFormatter;
        private object myListeningManipulatorLock = new object();
        private bool myIsListeningFlag;
        private Action<MessageContext> myMessageHandler;

        private string TracedObject { get { return GetType().Name; } }
    }
}
