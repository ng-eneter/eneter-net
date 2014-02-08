/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/


// Note: It is possible to compile for Silverlight but it probably does not have too much sense
//       therefore in order to keep dll smaller it is not included in Silverlight platforms.
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    internal class MessageBus : IMessageBus
    {
        // Helper class to wrap basic input channel functionality.
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

        // Attaches input channels for services and their channels and starts listening.
        public void AttachDuplexInputChannels(IDuplexInputChannel serviceInputChannel, IDuplexInputChannel clientInputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myAttachDetachLock)
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
                lock (myAttachDetachLock)
                {
                    myClientConnector.DetachDuplexInputChannel();
                    myServiceConnector.DetachDuplexInputChannel();
                }
            }
        }


        // Connection with the client was closed.
        private void OnClientDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                UnregisterClient(e.ResponseReceiverId);
            }
        }

        // A message from the client was received.
        private void OnMessageFromClientReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // If the message content is string then this message contains the service to which the client
                // wants to connect. Client is supposed to send this message immediatelly after OpenConnection().
                if (e.Message is string)
                {
                    string aServiceId = (string)e.Message;
                    RegisterClient(e.ResponseReceiverId, aServiceId);
                }
                else
                {
                    ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(e.Message);
                    if (aProtocolMessage != null && aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                    {
                        EneterTrace.Debug(e.Message.ToString());
                        ForwardMessageToService(e.ResponseReceiverId, e.Message);
                    }
                }
            }
        }

        // Adds the client to the list of connected clients and sends open connection message to the service.
        // If the service does not exist the client is disconnected.
        private void RegisterClient(string clientId, string serviceId)
        {
            using (EneterTrace.Entering())
            {
                bool anIsRegistered = false;
                lock (myConnectionsLock)
                {
                    // If such client does not exist yet then this is an open connection message.
                    if (!myConnectedClients.ContainsKey(clientId))
                    {
                        myConnectedClients[clientId] = serviceId;
                        anIsRegistered = true;
                    }
                }

                if (anIsRegistered)
                {
                    // Encode open connection message and send it to the service.
                    object anOpenConnectionMessage = myProtocolFormatter.EncodeOpenConnectionMessage(clientId);
                    try
                    {
                        myServiceConnector.AttachedDuplexInputChannel.SendResponseMessage(serviceId, anOpenConnectionMessage);
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "failed to send message to the service '" + serviceId + "'.";
                        EneterTrace.Error(anErrorMessage, err);

                        lock (myConnectionsLock)
                        {
                            myConnectedClients.Remove(clientId);
                        }
                        CloseConnection(myClientConnector, clientId);

                        UnregisterService(serviceId);
                        CloseConnection(myServiceConnector, serviceId);

                        throw;
                    }

                    // Confirm the connection was open.
                    try
                    {
                        myClientConnector.AttachedDuplexInputChannel.SendResponseMessage(clientId, "OK");
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "failed to confirm the connection was open.";
                        EneterTrace.Error(anErrorMessage, err);

                        lock (myConnectionsLock)
                        {
                            myConnectedClients.Remove(clientId);
                        }
                        CloseConnection(myClientConnector, clientId);

                        throw;
                    }
                }
                else
                {
                    string anErrorMessage = TracedObject + "did not register the client because the client with the same id already exists.";
                    EneterTrace.Warning(anErrorMessage);
                }
            }
        }

        private void UnregisterClient(string clientId)
        {
            using (EneterTrace.Entering())
            {
                string aServiceId;
                lock (myConnectionsLock)
                {
                    myConnectedClients.TryGetValue(clientId, out aServiceId);
                    myConnectedClients.Remove(clientId);
                }

                if (!string.IsNullOrEmpty(aServiceId))
                {
                    try
                    {
                        // Send close connection message to the service.
                        object aCloseConnectionMessage = myProtocolFormatter.EncodeCloseConnectionMessage(clientId);
                        myServiceConnector.AttachedDuplexInputChannel.SendResponseMessage(aServiceId, aCloseConnectionMessage);
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + ErrorHandler.CloseConnectionFailure;
                        EneterTrace.Warning(anErrorMessage, err);
                    }
                }
            }
        }

        private void ForwardMessageToService(string clientId, object encodedProtocolMessage)
        {
            using (EneterTrace.Entering())
            {
                string aServiceId = null;
                lock (myConnectionsLock)
                {
                    myConnectedClients.TryGetValue(clientId, out aServiceId);
                }

                if (!string.IsNullOrEmpty(aServiceId))
                {
                    // Forward the incomming message to the service.
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
                    // The client is not registered. Maybe it was closed meanwhile. So  clean it.
                    string anErrorMessage = TracedObject + "failed to send message to the service because the client has not had open connection with the message bus.";
                    EneterTrace.Warning(anErrorMessage);

                    CloseConnection(myClientConnector, clientId);
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
                            ForwardMessageToClient(aProtocolMessage.ResponseReceiverId, e.Message);
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

        private void ForwardMessageToClient(string clientId, object encodedProtocolMessage)
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

                    throw;
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




        private object myAttachDetachLock = new object();

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

#endif