﻿/*
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
    internal class DefaultOutputConnector : IOutputConnector
    {
        public DefaultOutputConnector(string inputConnectorAddress, string outputConnectorAddress, IMessagingProvider messagingProvider, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myOutputConnectorAddress = outputConnectorAddress;
                myMessagingProvider = messagingProvider;
                myProtocolFormatter = protocolFormatter;
            }
        }

        public void OpenConnection(Action<MessageContext> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (responseMessageHandler == null)
                    {
                        throw new ArgumentNullException("responseMessageHandler is null.");
                    }

                    myResponseMessageHandler = responseMessageHandler;
                    myMessagingProvider.RegisterMessageHandler(myOutputConnectorAddress, HandleResponseMessage);

                    myIsResponseListenerRegistered = true;

                    // Send the open connection request.
                    object anEncodedMessage = myProtocolFormatter.EncodeOpenConnectionMessage(myOutputConnectorAddress);
                    myMessagingProvider.SendMessage(myInputConnectorAddress, anEncodedMessage);

                    myIsConnected = true;
                }
            }
        }

        public void CloseConnection(bool sendCloseMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (myIsConnected)
                    {
                        if (sendCloseMessageFlag)
                        {
                            // Send close connection message.
                            try
                            {
                                object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(myOutputConnectorAddress);
                                myMessagingProvider.SendMessage(myInputConnectorAddress, anEncodedMessage);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + "failed to send close connection message.", err);
                            }
                        }

                        myIsConnected = false;
                    }

                    if (myIsResponseListenerRegistered)
                    {
                        myMessagingProvider.UnregisterMessageHandler(myOutputConnectorAddress);
                        myResponseMessageHandler = null;
                        myIsResponseListenerRegistered = false;
                    }
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulatorLock)
                {
                    return myIsConnected;
                }
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeMessage(myOutputConnectorAddress, message);
                    myMessagingProvider.SendMessage(myInputConnectorAddress, anEncodedMessage);
                }
            }
        }

        private void HandleResponseMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(message);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                try
                {
                    myResponseMessageHandler(aMessageContext);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        private string myInputConnectorAddress;
        private string myOutputConnectorAddress;
        private IMessagingProvider myMessagingProvider;
        private IProtocolFormatter myProtocolFormatter;
        private bool myIsConnected;
        private bool myIsResponseListenerRegistered;
        private object myConnectionManipulatorLock = new object();
        private Action<MessageContext> myResponseMessageHandler;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}
