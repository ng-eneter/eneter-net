/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    internal class MessageBus : IMessageBus
    {
        private class TConnector : AttachableDuplexInputChannelBase
        {
            public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
            public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
            public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

            protected override void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e)
            {
                if (MessageReceived != null)
                {
                    MessageReceived(sender, e);
                }
            }

            protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
            {
                if (ResponseReceiverConnected != null)
                {
                    ResponseReceiverConnected(sender, e);
                }
            }

            protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
            {
                if (ResponseReceiverDisconnected != null)
                {
                    ResponseReceiverDisconnected(sender, e);
                }
            }

            protected override string TracedObject
            {
                get { return GetType().Name + " ";  }
            }
        }


        public MessageBus(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;

                myServiceConnector = new TConnector();
                myClientConnector = new TConnector();

                myServiceConnector.ResponseReceiverConnected += OnServiceConnected;
                myServiceConnector.ResponseReceiverDisconnected += OnServiceDisconnected;
                myServiceConnector.MessageReceived += OnMessageFromServiceReceived;

                myClientConnector.ResponseReceiverDisconnected += OnClientDisconnected;
                myClientConnector.MessageReceived += OnMessageFromClientReceived;
            }
        }


        public void AttachDuplexInputChannels(IDuplexInputChannel serviceInputChannel, IDuplexInputChannel clientInputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myServiceConnector.AttachDuplexInputChannel(serviceInputChannel);
                    myClientConnector.AttachDuplexInputChannel(clientInputChannel);
                }
            }
        }

        public void DetachDuplexInputChannels()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myClientConnector.DetachDuplexInputChannel();
                    myServiceConnector.DetachDuplexInputChannel();
                }
            }
        }

        // Service connects to the message bus.
        private void OnServiceConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                RegisterService(e.ResponseReceiverId);
            }
        }

        // Service disconnected from the message bus.
        private void OnServiceDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                UnregisterService(e.ResponseReceiverId);
            }
        }
        
        private void OnMessageFromServiceReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(e.Message);
                    if (aProtocolMessage != null)
                    {
                        // A service sends a response message to a client.
                        if (aProtocolMessage.MessageType != EProtocolMessageType.Unknown)
                        {
                            SendMessageToClient(aProtocolMessage.ResponseReceiverId, e.Message);
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "detected incorrect message format. The service will be disconnected.";
                            EneterTrace.Warning(anErrorMessage);

                            CloseConnection(myServiceConnector, e.ResponseReceiverId);
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to receive a message from the service. The service will be disconnected.", err);

                    CloseConnection(myServiceConnector, e.ResponseReceiverId);
                }
            }
        }



        // Client has disconnected from the message bus.
        private void OnClientDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                UnregisterClient(e.ResponseReceiverId);
            }
        }

        private void OnMessageFromClientReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(e.Message);
                if (aProtocolMessage != null)
                {
                    // Client announces the name of the service that wants to connect.
                    // Or the client sends a message to the service.
                    if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                    {
                        ProcessMessageFromClient(e.ResponseReceiverId, aProtocolMessage, e.Message);
                    }
                    // Client sends the disconnection message before disconnecting.
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        SendMessageToService(e.ResponseReceiverId, e.Message);
                    }
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.Unknown)
                    {
                        string anErrorMessage = TracedObject + "detected incorrect message format. The client will be disconnected.";
                        EneterTrace.Warning(anErrorMessage);

                        UnregisterClient(e.ResponseReceiverId);
                        CloseConnection(myClientConnector, e.ResponseReceiverId);
                    }
                }
            }
        }


        private void RegisterService(string serviceId)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionsLock)
                {
                    myConnectedServices.Add(serviceId);
                }
            }
        }

        private void UnregisterService(string serviceId)
        {
            using (EneterTrace.Entering())
            {
                List<string> aClientsToDisconnect = new List<string>();

                lock (myConnectionsLock)
                {
                    // Remove the service.
                    myConnectedServices.Remove(serviceId);

                    // Remove all clients connected to the service.
                    foreach (KeyValuePair<string, string> aClient in myConnectedClients)
                    {
                        if (aClient.Value == serviceId)
                        {
                            aClientsToDisconnect.Add(aClient.Key);
                        }
                    }
                    foreach (string aClientId in aClientsToDisconnect)
                    {
                        myConnectedClients.Remove(aClientId);
                    }
                }

                // Close connections with clients.
                foreach (string aClientId in aClientsToDisconnect)
                {
                    CloseConnection(myClientConnector, aClientId);
                }
            }
        }

        private void SendMessageToClient(string clientId, object encodedProtocolMessage)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myClientConnector.AttachedDuplexInputChannel.SendResponseMessage(clientId, encodedProtocolMessage);
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to send message to the client.";
                    EneterTrace.Error(anErrorMessage, err);

                    UnregisterClient(clientId);
                    CloseConnection(myClientConnector, clientId);
                }
            }
        }

        private void ProcessMessageFromClient(string clientId, ProtocolMessage protocolMessage, object encodedProtocolMessage)
        {
            using (EneterTrace.Entering())
            {
                string aServiceId = null;
                bool aSuccessFlag = false;
                bool anIsConnectingClient = false;

                lock (myConnectionsLock)
                {
                    // If such client does not exist yet then this is an open connection message.
                    if (!myConnectedClients.ContainsKey(clientId))
                    {
                        // Get service id.
                        aServiceId = protocolMessage.Message as string;
                        if (!string.IsNullOrEmpty(aServiceId))
                        {
                            myConnectedClients[clientId] = aServiceId;

                            aSuccessFlag = true;
                            anIsConnectingClient = true;
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "failed to connect the client because service id was null or empty string.";
                            EneterTrace.Error(anErrorMessage);
                        }
                    }
                    else
                    // The client context exists so this must be a request message for the service.
                    {
                        aSuccessFlag = true;
                    }
                }

                if (aSuccessFlag)
                {
                    if (anIsConnectingClient)
                    {
                        SendOpenConnectionToService(aServiceId, clientId);
                    }
                    else
                    {
                        // Forward the incomming message to the service.
                        SendMessageToService(clientId, encodedProtocolMessage);
                    }
                }
                else
                {
                    CloseConnection(myClientConnector, clientId);
                }
            }
        }

        private bool RegisterClient(string serviceId, string clientId)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionsLock)
                {
                    if (myConnectedServices.Contains(serviceId))
                    {
                        if (!myConnectedClients.ContainsKey(clientId))
                        {
                            myConnectedClients[clientId] = serviceId;
                            return true;
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "could not register client because the client with the same id is already registered.";
                            EneterTrace.Warning(anErrorMessage);
                        }
                    }
                    else
                    {
                        string anErrorMessage = TracedObject + "could not register client because the service because the service '" + serviceId + "' is not registered.";
                        EneterTrace.Warning(anErrorMessage);
                    }

                    return false;
                }
            }
        }

        private void UnregisterClient(string clientId)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionsLock)
                {
                    myConnectedClients.Remove(clientId);
                }
            }
        }

        private void SendOpenConnectionToService(string serviceId, string clientId)
        {
            using (EneterTrace.Entering())
            {
                // Encode open connection message.
                object anOpenConnectionMessage = myProtocolFormatter.EncodeOpenConnectionMessage(clientId);
                try
                {
                    myServiceConnector.AttachedDuplexInputChannel.SendResponseMessage(serviceId, anOpenConnectionMessage);
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to send message to the service '" + serviceId + "'.";
                    EneterTrace.Error(anErrorMessage, err);

                    UnregisterService(serviceId);
                    CloseConnection(myServiceConnector, serviceId);
                }
            }
        }

        private void SendMessageToService(string clientId, object encodedProtocolMessage)
        {
            using (EneterTrace.Entering())
            {
                // Get the service id to which the client is connected.
                string aServiceId;
                lock (myConnectionsLock)
                {
                    myConnectedClients.TryGetValue(clientId, out aServiceId);
                }

                if (!string.IsNullOrEmpty(aServiceId))
                {
                    try
                    {
                        myServiceConnector.AttachedDuplexInputChannel.SendResponseMessage(aServiceId, encodedProtocolMessage);
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "failed to send message to the service '" + aServiceId + "'.";
                        EneterTrace.Error(anErrorMessage, err);

                        UnregisterService(aServiceId);
                        CloseConnection(myServiceConnector, aServiceId);

                        throw;
                    }
                }
                else
                {
                    string anErrorMessage = TracedObject + "failed to send message to the service because service id was null.";
                    EneterTrace.Error(anErrorMessage);
                }
            }
        }




        private void CloseConnection(TConnector connector, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    connector.AttachedDuplexInputChannel.DisconnectResponseReceiver(responseReceiverId);
                }
                catch
                {
                }
            }
        }




        private object myConnectionManipulatorLock = new object();

        private object myConnectionsLock = new object();

        // <service id>
        private HashSet<string> myConnectedServices = new HashSet<string>();

        // <client id, service id>
        private Dictionary<string, string> myConnectedClients = new Dictionary<string, string>();
        


        private TConnector myServiceConnector;
        private TConnector myClientConnector;
        private IProtocolFormatter myProtocolFormatter;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();

        protected string TracedObject { get { return GetType().Name + " "; } }
    }
}
