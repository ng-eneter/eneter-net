/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using System.Collections.Generic;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightInputConnector : IInputConnector
    {
        public SilverlightInputConnector(string inputConnectorAddress, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(inputConnectorAddress))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                myInputConnectorAddress = inputConnectorAddress;
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
                        myRequestMessageHandler = messageHandler;
                        myReceiver = new SilverlightReceiver(myInputConnectorAddress);
                        myReceiver.StartListening(HandleRequestMessage);
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
                using (ThreadLock.Lock(myListenerManipulatorLock))
                {
                    if (myReceiver != null)
                    {
                        myReceiver.StopListening();
                        myReceiver = null;
                    }
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (ThreadLock.Lock(myListenerManipulatorLock))
                {
                    return myReceiver != null && myReceiver.IsListening;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                SilverlightSender aClientSender;
                using (ThreadLock.Lock(myConnectedClients))
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientSender);
                }

                if (aClientSender == null)
                {
                    throw new InvalidOperationException("The connection with client '" + outputConnectorAddress + "' is not open.");
                }

                string anEncodedMessage = (string)myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                aClientSender.SendMessage(anEncodedMessage);
            }
        }

        // When service disconnects a client.
        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                SilverlightSender aClientSender;
                using (ThreadLock.Lock(myConnectedClients))
                {
                    myConnectedClients.TryGetValue(outputConnectorAddress, out aClientSender);
                    if (aClientSender != null)
                    {
                        myConnectedClients.Remove(outputConnectorAddress);
                    }
                }

                if (aClientSender != null)
                {
                    try
                    {
                        string anEncodedMessage = (string)myProtocolFormatter.EncodeCloseConnectionMessage(outputConnectorAddress);
                        aClientSender.SendMessage(anEncodedMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning("failed to send the close message.", err);
                    }
                }
            }
        }

        private void HandleRequestMessage(string message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(message);

                if (aProtocolMessage != null)
                {
                    // If it is open connection then add the new connected client.
                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            using (ThreadLock.Lock(myConnectedClients))
                            {
                                if (!myConnectedClients.ContainsKey(aProtocolMessage.ResponseReceiverId))
                                {
                                    TimeSpan aDummyOpenTimeout = TimeSpan.Zero;
                                    SilverlightSender aClientSender = new SilverlightSender(aProtocolMessage.ResponseReceiverId);

                                    myConnectedClients[aProtocolMessage.ResponseReceiverId] = aClientSender;
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
                    // When a client closes connection with the service.
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        if (!string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                        {
                            using (ThreadLock.Lock(myConnectedClients))
                            {
                                SilverlightSender aClientSender;
                                myConnectedClients.TryGetValue(aProtocolMessage.ResponseReceiverId, out aClientSender);

                                if (aClientSender != null)
                                {
                                    myConnectedClients.Remove(aProtocolMessage.ResponseReceiverId);
                                }
                            }
                        }
                    }

                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                    try
                    {
                        myRequestMessageHandler(aMessageContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private Action<MessageContext> myRequestMessageHandler;
        private IProtocolFormatter myProtocolFormatter;
        private string myInputConnectorAddress;
        private object myListenerManipulatorLock = new object();
        private SilverlightReceiver myReceiver;
        private Dictionary<string, SilverlightSender> myConnectedClients = new Dictionary<string, SilverlightSender>();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif