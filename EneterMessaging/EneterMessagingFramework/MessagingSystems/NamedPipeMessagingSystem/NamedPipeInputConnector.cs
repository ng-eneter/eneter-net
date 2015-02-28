/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


#if !SILVERLIGHT && !MONO && !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeInputConnector : IInputConnector
    {
        public NamedPipeInputConnector(string inputConnectorAddress, IProtocolFormatter protocolFormatter, int connectionTimeout, int numberOfListeningInstances, PipeSecurity security)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myProtocolFormatter = protocolFormatter;
                myConnectionTimeout = connectionTimeout;
                myNumberOfListeningInstances = numberOfListeningInstances;
                mySecurity = security;
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
                        myReceiver = new NamedPipeReceiver(myInputConnectorAddress, myNumberOfListeningInstances, myConnectionTimeout, mySecurity);
                        myReceiver.StartListening(OnRequestMessageReceived);
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
                    if (myReceiver != null)
                    {
                        myReceiver.StopListening();
                        myReceiver = null;
                    }

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
                    return myReceiver != null && myReceiver.IsListening;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                NamedPipeSender aClientContext;
                lock (myConnectedClients)
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientContext);
                }

                if (aClientContext == null)
                {
                    throw new InvalidOperationException("The connection with client '" + outputConnectorAddress + "' is not open.");
                }

                object anEncodedMessage = myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                aClientContext.SendMessage(anEncodedMessage);
            }
        }

        // When service disconnects a client.
        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                NamedPipeSender aClientContext;
                lock (myConnectedClients)
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientContext);
                    if (aClientContext != null)
                    {
                        myConnectedClients.Remove(outputConnectorAddress);
                    }
                }

                if (aClientContext != null)
                {
                    try
                    {
                        object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress);
                        aClientContext.SendMessage(anEncodedMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning("failed to send the close message.", err);
                    }

                    aClientContext.Dispose();
                }
            }
        }

        private void OnRequestMessageReceived(Stream message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)message);
                if (aProtocolMessage != null)
                {
                    MessageContext aMessageContext = null;

                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            lock (myConnectedClients)
                            {
                                if (!myConnectedClients.ContainsKey(aProtocolMessage.ResponseReceiverId))
                                {
                                    NamedPipeSender aClientContext = new NamedPipeSender(aProtocolMessage.ResponseReceiverId, myConnectionTimeout);
                                    myConnectedClients[aProtocolMessage.ResponseReceiverId] = aClientContext;
                                }
                                else
                                {
                                    EneterTrace.Warning(TracedObject + "could not open connection for client '" + aProtocolMessage.ResponseReceiverId + "' because the client with same id is already connected.");
                                }
                            }
                        }
                        else
                        {
                            EneterTrace.Warning(TracedObject + "could not connect a client because response recevier id was not available in open connection message.");
                        }
                    }
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            lock (myConnectedClients)
                            {
                                myConnectedClients.Remove(aProtocolMessage.ResponseReceiverId);
                            }
                        }
                    }

                    try
                    {
                        aMessageContext = new MessageContext(aProtocolMessage, "");
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
        private string myInputConnectorAddress;
        private int myNumberOfListeningInstances;
        private int myConnectionTimeout;
        private PipeSecurity mySecurity;
        private NamedPipeReceiver myReceiver;
        private Action<MessageContext> myMessageHandler;

        private object myListeningManipulatorLock = new object();
        private Dictionary<string, NamedPipeSender> myConnectedClients = new Dictionary<string, NamedPipeSender>();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif