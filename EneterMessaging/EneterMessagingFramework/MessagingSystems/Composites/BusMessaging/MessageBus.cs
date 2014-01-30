/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
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

        private class TServiceContext
        {
            public TServiceContext()
            {
                ConnectedClients = new Dictionary<string, string>();
            }

            public string ServiceId { get; set; }

            // Key is logical client id inside the message bus.
            // Value is physical client response receiver id.
            public Dictionary<string, string> ConnectedClients { get; private set; }
        }

        private class TClientContext
        {
            public string LogicalClientId { get; set; }
            public string ServiceId { get; set; }
        }


        public MessageBus(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;

                myServiceConnector = new TConnector();
                myClientConnector = new TConnector();

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
                ConnectService(e.ResponseReceiverId);
            }
        }

        // Service disconnected from the message bus.
        private void OnServiceDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                DisconnectService(e.ResponseReceiverId);
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
                        if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                        {
                            ForwardResponseMessageToClient(e.ResponseReceiverId, aProtocolMessage.ResponseReceiverId, aProtocolMessage.Message);
                        }
                        else if (aProtocolMessage.MessageType == EProtocolMessageType.Unknown)
                        {
                            string anErrorMessage = TracedObject + "detected incorrect message format. The service will be disconnected.";
                            EneterTrace.Warning(anErrorMessage);

                            DisconnectService(e.ResponseReceiverId);
                        }
                    }
                    else
                    {
                        // The ProtocolMessage was null. It means the connection was closed.
                        DisconnectService(e.ResponseReceiverId);
                    }

                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to receive a message from the service. The service will be disconnected.", err);

                    DisconnectService(e.ResponseReceiverId);
                }
            }
        }



        // Client has sent the disconnection message.
        private void OnClientDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                ClientDisconnectsItselfByPhysical(e.ResponseReceiverId);
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
                        ProcessMessageFromClient(e.ResponseReceiverId, aProtocolMessage.ResponseReceiverId, aProtocolMessage.Message);
                    }
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.Unknown)
                    {
                        string anErrorMessage = TracedObject + "detected incorrect message format. The client will be disconnected.";
                        EneterTrace.Warning(anErrorMessage);

                        ClientDisconnectsItselfByPhysical(e.ResponseReceiverId);
                    }
                }
            }
        }


        private void ProcessMessageFromClient(string physicalClientResponseReceiverId, string logicalClientId, object message)
        {
            using (EneterTrace.Entering())
            {
                string aServiceId = null;
                bool aSuccessFlag = false;
                bool anIsConnectingClient = false;

                lock (myConnectionsLock)
                {
                    // Get the client context.
                    TClientContext aClientContext;
                    myConnectedClients.TryGetValue(physicalClientResponseReceiverId, out aClientContext);
                    
                    // If the client context does not exist then this is the open connection message.
                    if (aClientContext == null)
                    {
                        byte[] aMessageBuffer = message as byte[];
                        try
                        {
                            if (aMessageBuffer != null)
                            {
                                using (MemoryStream aDataStream = new MemoryStream(aMessageBuffer))
                                {
                                    aServiceId = (string) myEncoderDecoder.Decode(aDataStream);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            string anErrorMessage = TracedObject + "failed to decode the service logical id from the message.";
                            EneterTrace.Error(anErrorMessage, err);
                        }

                        // If service id was sucessfully decoded.
                        if (!string.IsNullOrEmpty(aServiceId))
                        {
                            if (AddClient(aServiceId, physicalClientResponseReceiverId, logicalClientId))
                            {
                                aSuccessFlag = true;
                            }
                        }
                    }
                    else
                    // The client context exists so this must be a request message for the service.
                    {
                        // The service is already included in the client context.
                        // Therefore the incomming message is a request message for the service.
                        aServiceId = aClientContext.ServiceId;

                        aSuccessFlag = true;
                    }
                }

                if (aSuccessFlag)
                {
                    if (anIsConnectingClient)
                    {
                        // Send the open connection message to the service.
                        ClientSendsOpenConnectionToService(aServiceId, logicalClientId);
                    }
                    else
                    {
                        // Forward the message to the service.
                        SendMessageToService(aServiceId, message);
                    }
                }
                else
                {
                    ClientDisconnectsItselfByPhysical(physicalClientResponseReceiverId);
                }
            }
        }

        // A client disconnect itself. 
        private void ClientDisconnectsItselfByPhysical(string physicalClientResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                string aPhysicalServiceResponseReceiverId = null;
                string aLogicalClientId = null;

                lock (myConnectionsLock)
                {
                    RemoveClientByPhysical(physicalClientResponseReceiverId, out aLogicalClientId, out aPhysicalServiceResponseReceiverId);
                }

                if (aLogicalClientId != null && aPhysicalServiceResponseReceiverId != null)
                {
                    // Notify service the client is disconnected.
                    ClientSendsCloseConnectionToService(aPhysicalServiceResponseReceiverId, aLogicalClientId);
                }

                // Close physical connection with the client.
                CloseConnection(myClientConnector, physicalClientResponseReceiverId);
            }
        }

        // A client disconnect itself. 
        private void ClientDisconnectsItselfByLogical(string serviceid, string logicalClientId)
        {
            using (EneterTrace.Entering())
            {
                string aPhysicalClientResponseReceiverId;

                lock (myConnectionsLock)
                {
                    RemoveClientByLogical(serviceid, logicalClientId, out aPhysicalClientResponseReceiverId);
                }

                if (!string.IsNullOrEmpty(aPhysicalClientResponseReceiverId))
                {
                    // Notify service the client is disconnected.
                    ClientSendsCloseConnectionToService(serviceid, logicalClientId);
                }

                // Close physical connection with the client.
                CloseConnection(myClientConnector, aPhysicalClientResponseReceiverId);
            }
        }

        // Service disconnects its client.
        private void ServiceDisconnectsClient(string serviceId, string logicalClientId, object message)
        {
            using (EneterTrace.Entering())
            {
                string aPhysicalClientResponseReceiverId;
                lock (myConnectionsLock)
                {
                    RemoveClientByLogical(serviceId, logicalClientId, out aPhysicalClientResponseReceiverId);
                }

                SendCloseConnectionMessage(myClientConnector, aPhysicalClientResponseReceiverId, message);
                CloseConnection(myClientConnector, aPhysicalClientResponseReceiverId);
            }
        }

        private void ServiceDisconnectsClient(string physicalClientResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                string aLogicalClientId;
                string aPhysicalClientResponseReceiverId;
                lock (myConnectionsLock)
                {
                    RemoveClientByPhysical(physicalClientResponseReceiverId, out aLogicalClientId, out aPhysicalClientResponseReceiverId);
                }

                if (aLogicalClientId != null && aPhysicalClientResponseReceiverId != null)
                {
                    SendCloseConnectionMessage(myClientConnector, aPhysicalClientResponseReceiverId, aLogicalClientId);
                }
                CloseConnection(myClientConnector, aPhysicalClientResponseReceiverId);
            }
        }

        private void ClientSendsOpenConnectionToService(string serviceId, string logicalClientId)
        {
            using (EneterTrace.Entering())
            {
                object anOpenConnectionMessage;
                try
                {
                    anOpenConnectionMessage = myProtocolFormatter.EncodeOpenConnectionMessage(logicalClientId);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to encode open connection message. The client will be disconnected.", err);

                    ClientDisconnectsItselfByLogical(serviceId, logicalClientId);
                    return;
                }

                SendMessageToService(serviceId, anOpenConnectionMessage);
            }
        }

        private void ClientSendsCloseConnectionToService(string serviceId, string logicalClientId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object aCloseConnectionMessage = myProtocolFormatter.EncodeOpenConnectionMessage(logicalClientId);
                    SendMessageToService(serviceId, aCloseConnectionMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to encode close connection message.", err);
                }
            }
        }

        private void SendMessageToService(string serviceId, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myServiceConnector.AttachedDuplexInputChannel.SendResponseMessage(serviceId, message);
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to send message to the service.";
                    EneterTrace.Error(anErrorMessage, err);

                    // The sending of a message to the service failed.
                    // Therefore consider the service as disconnected.
                    DisconnectService(serviceId);
                }
            }
        }





        private void ConnectService(string serviceId)
        {
            using (EneterTrace.Entering())
            {
                bool anIsDuplicate = false;

                lock (myConnectionsLock)
                {
                    // Connect service only if such service is not connected yet.
                    if (!myConnectedServices.ContainsKey(serviceId))
                    {
                        // Add service among connected services.
                        AddService(serviceId);
                    }
                    else
                    {
                        anIsDuplicate = true;
                    }
                }

                if (anIsDuplicate)
                {
                    string anErrorMessage = TracedObject + "failed to connect the service because the service '" + serviceId + "' is already connected. The connection will be closed.";
                    EneterTrace.Warning(anErrorMessage);

                    // Close connection that tries to connect the duplicated service.
                    // Note: notice there are no connected clients in this case!
                    CloseConnection(myServiceConnector, serviceId);
                }
            }
        }

        private void DisconnectService(string serviceId)
        {
            using (EneterTrace.Entering())
            {
                TServiceContext aServiceContext;
                lock (myConnectionsLock)
                {
                    aServiceContext = RemoveService(serviceId);
                }

                // Finaly close connection with the service.
                CloseConnection(myServiceConnector, serviceId);

                if (aServiceContext != null)
                {
                    // Close connection with all clients.
                    foreach (KeyValuePair<string, string> aClientContext in aServiceContext.ConnectedClients)
                    {
                        CloseConnection(myClientConnector, aClientContext.Value);
                    }

                    aServiceContext.ConnectedClients.Clear();
                }
            }
        }

        private void ForwardResponseMessageToClient(string serviceId, string logicalClientId, object message)
        {
            using (EneterTrace.Entering())
            {
                bool aDisconnectServiceFlag = false;

                string aPhysicalClientResponseReceiverId = null;
                lock (myConnectionsLock)
                {
                    // Get the service context.
                    TServiceContext aServiceContext;
                    myConnectedServices.TryGetValue(serviceId, out aServiceContext);
                    if (aServiceContext != null)
                    {
                        // Get response receiver id from clients connected to this service.
                        aServiceContext.ConnectedClients.TryGetValue(logicalClientId, out aPhysicalClientResponseReceiverId);
                    }
                    else
                    {
                        // A service context was not found. The service cannot operate without the context.
                        string anErrorMessage = TracedObject + "failed to send the message to the client because the service context was not found. The service will be disconnected.";
                        EneterTrace.Warning(anErrorMessage);

                        aDisconnectServiceFlag = true;
                    }
                }

                if (aDisconnectServiceFlag)
                {
                    DisconnectService(serviceId);
                }

                if (!string.IsNullOrEmpty(aPhysicalClientResponseReceiverId))
                {
                    SendMessageToClient(aPhysicalClientResponseReceiverId, message);
                }
                else
                {
                    string anErrorMessage = TracedObject + "failed to send the message to the client because the client was not found among connected clients.";
                    EneterTrace.Warning(anErrorMessage);
                }
            }
        }


        private void SendMessageToClient(string physicalClientResponseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myClientConnector.AttachedDuplexInputChannel.SendResponseMessage(physicalClientResponseReceiverId, message);
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to send message to the client.";
                    EneterTrace.Error(anErrorMessage, err);

                    ServiceDisconnectsClient(physicalClientResponseReceiverId);
                }

            }
        }


        private void SendCloseConnectionMessage(TConnector connector, string physicalResponseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    connector.AttachedDuplexInputChannel.SendResponseMessage(physicalResponseReceiverId, message);
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to send close connection message.";
                    EneterTrace.Warning(anErrorMessage, err);
                }
            }
        }

        private void CloseConnection(TConnector connector, string physicalResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    connector.AttachedDuplexInputChannel.DisconnectResponseReceiver(physicalResponseReceiverId);
                }
                catch
                {
                }
            }
        }


        private void AddService(string serviceId)
        {
            using (EneterTrace.Entering())
            {
                TServiceContext aServiceContext = new TServiceContext();
                aServiceContext.ServiceId = serviceId;

                myConnectedServices[serviceId] = aServiceContext;
            }
        }

        private bool AddClient(string serviceId, string physicalClientResponseReceiverId, string logicalClientId)
        {
            using (EneterTrace.Entering())
            {
                if (!string.IsNullOrEmpty(logicalClientId))
                {
                    // Only if such client does not exist then it can be created.
                    if (!myConnectedClients.ContainsKey(physicalClientResponseReceiverId))
                    {
                        TServiceContext aServiceContext;
                        myConnectedServices.TryGetValue(serviceId, out aServiceContext);
                        if (aServiceContext != null)
                        {
                            if (!aServiceContext.ConnectedClients.ContainsKey(logicalClientId))
                            {
                                // Add the new client.
                                TClientContext aClientContext = new TClientContext();
                                aClientContext.LogicalClientId = logicalClientId;
                                aClientContext.ServiceId = serviceId;
                                myConnectedClients[physicalClientResponseReceiverId] = aClientContext;

                                // Add the client among open connections inside the service.
                                aServiceContext.ConnectedClients[logicalClientId] = physicalClientResponseReceiverId;

                                return true;
                            }
                            else
                            {
                                string anErrorMessage = TracedObject + "failed to connect the client because the client with the same id '" + logicalClientId + "' is already connected to the service.";
                                EneterTrace.Error(anErrorMessage);
                            }
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "failed to connect the client because the service was not found.";
                            EneterTrace.Error(anErrorMessage);
                        }
                    }
                    else
                    {
                        string anErrorMessage = TracedObject + "failed to connect the client because the client with the same response receiver id already exist.";
                        EneterTrace.Error(anErrorMessage);
                    }
                }
                else
                {
                    string anErrorMessage = TracedObject + "failed to connect the client because the client id is null or empty string.";
                    EneterTrace.Error(anErrorMessage);
                }

                return false;
            }
        }

        private TServiceContext RemoveService(string serviceId)
        {
            using (EneterTrace.Entering())
            {
                TServiceContext aServiceContext;
                myConnectedServices.TryGetValue(serviceId, out aServiceContext);
                if (aServiceContext != null)
                {
                    myConnectedServices.Remove(serviceId);

                    foreach (string aPhysicalClientResponseReceiverId in aServiceContext.ConnectedClients.Values)
                    {
                        myConnectedClients.Remove(aPhysicalClientResponseReceiverId);
                    }
                }

                return aServiceContext;
            }
        }

        private void RemoveClientByPhysical(string physicalClientResponseReceiverId, out string logicalClientId, out string serviceId)
        {
            using (EneterTrace.Entering())
            {
                logicalClientId = null;
                serviceId = null;

                TClientContext aClientContext;
                myConnectedClients.TryGetValue(physicalClientResponseReceiverId, out aClientContext);
                if (aClientContext != null)
                {
                    logicalClientId = aClientContext.LogicalClientId;
                    serviceId = aClientContext.ServiceId;

                    // Remove lient from the list of clients.
                    myConnectedClients.Remove(physicalClientResponseReceiverId);

                    TServiceContext aServiceContext;
                    myConnectedServices.TryGetValue(aClientContext.ServiceId, out aServiceContext);
                    if (aServiceContext != null)
                    {
                        // Remove the client from the list of clients connected to this service.
                        aServiceContext.ConnectedClients.Remove(logicalClientId);
                    }
                }
            }
        }

        private void RemoveClientByLogical(string serviceId, string logicalClientId, out string physicalClientResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                physicalClientResponseReceiverId = null;

                TServiceContext aServiceContext;
                myConnectedServices.TryGetValue(serviceId, out aServiceContext);
                if (aServiceContext != null)
                {
                    aServiceContext.ConnectedClients.TryGetValue(logicalClientId, out physicalClientResponseReceiverId);
                    if (!string.IsNullOrEmpty(physicalClientResponseReceiverId))
                    {
                        // Remove the client.
                        myConnectedClients.Remove(physicalClientResponseReceiverId);
                    }

                    // Remove the client from the service.
                    aServiceContext.ConnectedClients.Remove(logicalClientId);
                }
            }
        }

        



        private object myConnectionManipulatorLock = new object();

        // Key is the logic service id (this id is also the physical response receiver id)
        private Dictionary<string, TServiceContext> myConnectedServices = new Dictionary<string, TServiceContext>();

        // Key is the physical response receiver id of the client.
        private Dictionary<string, TClientContext> myConnectedClients = new Dictionary<string, TClientContext>();
        
        private object myConnectionsLock = new object();


        private TConnector myServiceConnector;
        private TConnector myClientConnector;
        private IProtocolFormatter myProtocolFormatter;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();

        protected string TracedObject { get { return GetType().Name + " "; } }
    }
}
