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
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    internal class MessageBus : IMessageBus
    {
        private class TServiceContext
        {
            public TServiceContext()
            {
                ConnectedClients = new Dictionary<string, string>();
            }

            public string LogicalServiceId { get; set; }
            public string PhysicalServiceResponseReceiverId { get; set; }

            // Key is logical client id inside the message bus.
            // Value is physical client response receiver id.
            public Dictionary<string, string> ConnectedClients { get; private set; }
        }

        private class TClientContext
        {
            public string LogicalClientId { get; set; }
            public string LogicalServiceId { get; set; }
            public string PhysicalServiceResponseReceiverId { get; set; }
        }


        public void AttachDuplexInputChannel(IDuplexInputChannel serviceInputChannel, IDuplexInputChannel clientInputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myServiceChannel.StartListening();
                    myClientChannel.StartListening();
                }
            }
        }

        public void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myClientChannel.StopListening();
                    myServiceChannel.StopListening();
                }
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
                        // A service wants to connect to the message bus.
                        if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                        {
                            ConnectService(e.ResponseReceiverId, aProtocolMessage.ResponseReceiverId);
                        }
                        // A service wants to disconnect a particular client.
                        else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                        {
                            ServiceDisconnectsClient(e.ResponseReceiverId, aProtocolMessage.ResponseReceiverId, aProtocolMessage.Message);
                        }
                        // A service sends a response message to a client.
                        else if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                        {
                            SendResponseMessageToClient(e.ResponseReceiverId, aProtocolMessage.ResponseReceiverId, aProtocolMessage.Message);
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
                ClientDisconnectsItself(e.ResponseReceiverId);
            }
        }

        private void OnMessageFromClientReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(e.Message);
                if (aProtocolMessage != null)
                {
                    // Client wants to open the connection.
                    // Note: At this state the logical service id is not specified.
                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        BeginConnectingClient(e.ResponseReceiverId, aProtocolMessage.ResponseReceiverId);
                    }
                    // Client wants to close the connection with the service.
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        ClientDisconnectsItself(e.ResponseReceiverId);
                    }
                    // Client announces the name of the service that wants to connect.
                    // Or the client sends a message to the service.
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                    {
                        ProcessMessageFromClient(e.ResponseReceiverId, aProtocolMessage.ResponseReceiverId, aProtocolMessage.Message);
                    }
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.Unknown)
                    {
                        string anErrorMessage = TracedObject + "detected incorrect message format. The client will be disconnected.";
                        EneterTrace.Warning(anErrorMessage);

                        ClientDisconnectsItself(e.ResponseReceiverId);
                    }
                }
            }
        }




        private void BeginConnectingClient(string physicalClientResponseReceiverId, string logicalClientId)
        {
            using (EneterTrace.Entering())
            {
                bool anIsClientAdded;
                lock (myConnectionsLock)
                {
                    anIsClientAdded = AddClient(physicalClientResponseReceiverId, logicalClientId);
                }

                // If the client was not added then something is wrong and the connection will be closed.
                if (!anIsClientAdded)
                {
                    CloseConnection(myClientChannel, physicalClientResponseReceiverId);
                }
            }
        }

        private void ProcessMessageFromClient(string physicalClientResponseReceiverId, string logicalClientId, object message)
        {
            using (EneterTrace.Entering())
            {
                string aPhysicalServiceResponseReceiverId = null;
                bool anErrorFlag = true;
                bool anIsConnectingClient = false;

                lock (myConnectionsLock)
                {
                    // Get the client context.
                    TClientContext aClientContext;
                    myConnectedClientsPhysical.TryGetValue(physicalClientResponseReceiverId, out aClientContext);
                    if (aClientContext != null)
                    {
                        // If the client context does not contain the service then this is the 2nd phase of openning the connection
                        // and the received message must contain service logical id.
                        if (string.IsNullOrEmpty(aClientContext.PhysicalServiceResponseReceiverId))
                        {
                            byte[] aMessageBuffer = message as byte[];
                            if (aMessageBuffer != null)
                            {
                                try
                                {
                                    using (MemoryStream aDataStream = new MemoryStream(aMessageBuffer))
                                    {
                                        // Store service logical id.
                                        aClientContext.LogicalServiceId = myEncoderDecoder.Decode(aDataStream) as string;
                                    }
                                }
                                catch (Exception err)
                                {
                                    string anErrorMessage = TracedObject + "failed to decode the service logical id from the message.";
                                    EneterTrace.Error(anErrorMessage, err);
                                }

                                if (!string.IsNullOrEmpty(aClientContext.LogicalServiceId))
                                {
                                    TServiceContext aServiceContext;
                                    myConnectedServicesLogical.TryGetValue(aClientContext.LogicalServiceId, out aServiceContext);
                                    if (aServiceContext != null)
                                    {
                                        // Store physical service response receiver id.
                                        aClientContext.PhysicalServiceResponseReceiverId = aServiceContext.PhysicalServiceResponseReceiverId;
                                        aPhysicalServiceResponseReceiverId = aServiceContext.PhysicalServiceResponseReceiverId;

                                        // Add the client to service's open connections.
                                        if (!aServiceContext.ConnectedClients.ContainsKey(aClientContext.LogicalClientId))
                                        {
                                            aServiceContext.ConnectedClients[aClientContext.LogicalClientId] = physicalClientResponseReceiverId;

                                            // The client successfully opened the connection.
                                            anErrorFlag = false;

                                            // The method performed finalizing of opening the connection.
                                            anIsConnectingClient = true;
                                        }
                                        else
                                        {
                                            // The client with such id is already there and it is not good.
                                            string anErrorMessage = TracedObject + "failed to add the client among service open connections because the client with the same id already exists.";
                                            EneterTrace.Error(anErrorMessage);
                                        }
                                    }
                                    else
                                    {
                                        string anErrorMessage = TracedObject + "did not find the response receiver id for the service '" + aClientContext.LogicalServiceId + "'.";
                                        EneterTrace.Error(anErrorMessage);
                                    }
                                }
                            }
                            else
                            {
                                string anErrorMessage = TracedObject + "failed to connect the client because the service logical id was not byte[].";
                                EneterTrace.Error(anErrorMessage);
                            }

                            // If something was wrong then clean storages.
                            if (anErrorFlag)
                            {
                                myConnectedClientsPhysical.Remove(physicalClientResponseReceiverId);

                                // Note: it is not needed to clean TServiceContext because the client was not put to the service context yet.
                            }
                        }
                        else
                        {
                            // The service is already included in the client context.
                            // Therefore the incomming message is a request message for the service.
                            aPhysicalServiceResponseReceiverId = aClientContext.PhysicalServiceResponseReceiverId;

                            anErrorFlag = false;
                        }
                    }
                    else
                    {
                        // Client context does not exist. It means there is something wrong with connecting the client to the message bus.
                        string anErrorMessage = TracedObject + "failed to connect the client because the client did not oen the connection first.";
                        EneterTrace.Error(anErrorMessage);

                        anErrorFlag = true;
                    }
                }

                if (anErrorFlag)
                {
                    ClientDisconnectsItself(physicalClientResponseReceiverId);
                }
                else
                {
                    if (anIsConnectingClient)
                    {
                        // Send the open connection message to the service.
                        ClientSendsOpenConnectionToService(logicalClientId, aPhysicalServiceResponseReceiverId);
                    }
                    else
                    {
                        // Forward the message to the service.
                        SendMessageToService(aPhysicalServiceResponseReceiverId, message);
                    }
                }
            }
        }

        // A client disconnect itself. 
        private void ClientDisconnectsItself(string physicalClientResponseReceiverId)
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
                    ClientSendsCloseConnectionToService(aLogicalClientId, aPhysicalServiceResponseReceiverId);
                }

                // Close physical connection with the client.
                CloseConnection(myClientChannel, physicalClientResponseReceiverId);
            }
        }

        // Service disconnects its client.
        private void ServiceDisconnectsClient(string physicalServiceResponseReceiverId, string logicalClientId, object message)
        {
            using (EneterTrace.Entering())
            {
                string aPhysicalClientResponseReceiverId;
                lock (myConnectionsLock)
                {
                    RemoveClientByLogical(physicalServiceResponseReceiverId, logicalClientId, out aPhysicalClientResponseReceiverId);
                }

                SendCloseConnectionMessage(myClientChannel, aPhysicalClientResponseReceiverId, message);
                CloseConnection(myClientChannel, aPhysicalClientResponseReceiverId);
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
                    SendCloseConnectionMessage(myClientChannel, aPhysicalClientResponseReceiverId, aLogicalClientId);
                }
                CloseConnection(myClientChannel, aPhysicalClientResponseReceiverId);
            }
        }

        private void ClientSendsOpenConnectionToService(string logicalClientId, string physicalServiceResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object anOpenConnectionMessage = myProtocolFormatter.EncodeOpenConnectionMessage(logicalClientId);
                    SendMessageToService(physicalServiceResponseReceiverId, anOpenConnectionMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to encode open connection message. The client will be disconnected.", err);

                    ClientDisconnectsItself(physicalServiceResponseReceiverId);
                }
            }
        }

        private void ClientSendsCloseConnectionToService(string logicalClientId, string physicalServiceResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object aCloseConnectionMessage = myProtocolFormatter.EncodeOpenConnectionMessage(logicalClientId);
                    SendMessageToService(physicalServiceResponseReceiverId, aCloseConnectionMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to encode close connection message.", err);
                }
            }
        }

        private void SendMessageToService(string physicalServiceResponseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myServiceChannel.SendResponseMessage(physicalServiceResponseReceiverId, message);
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to send message to the service.";
                    EneterTrace.Error(anErrorMessage, err);

                    // The sending of a message to the service failed.
                    // Therefore consider the service as disconnected.
                    DisconnectService(physicalServiceResponseReceiverId);
                }
            }
        }





        private void ConnectService(string physicalServiceResponseReceiverId, string logicalServiceId)
        {
            using (EneterTrace.Entering())
            {
                bool anIsDuplicate = false;

                lock (myConnectionsLock)
                {
                    // Connect service only if such service is not connected yet.
                    if (!myConnectedServicesLogical.ContainsKey(logicalServiceId))
                    {
                        // Add service among connected services.
                        AddService(physicalServiceResponseReceiverId, logicalServiceId);
                    }
                    else
                    {
                        anIsDuplicate = true;
                    }
                }

                if (anIsDuplicate)
                {
                    string anErrorMessage = TracedObject + "failed to connect the service because the service '" + logicalServiceId + "' is already connected. The connection will be closed.";
                    EneterTrace.Warning(anErrorMessage);

                    // Close connection that tries to connect the duplicated service.
                    // Note: notice there are no connected clients in this case!
                    CloseConnection(myServiceChannel, physicalServiceResponseReceiverId);
                }
            }
        }

        private void DisconnectService(string physicalServiceResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                TServiceContext aServiceContext;
                lock (myConnectionsLock)
                {
                    aServiceContext = RemoveService(physicalServiceResponseReceiverId);
                }

                if (aServiceContext != null)
                {
                    // Disconnect all connected clients.
                    foreach (KeyValuePair<string, string> aClientContext in aServiceContext.ConnectedClients)
                    {
                        SendCloseConnectionMessage(myClientChannel, aClientContext.Value, aClientContext.Key);
                        CloseConnection(myClientChannel, aClientContext.Value);
                    }

                    aServiceContext.ConnectedClients.Clear();
                }

                CloseConnection(myServiceChannel, physicalServiceResponseReceiverId);
            }
        }

        private void SendResponseMessageToClient(string physicalServiceResponseReceiverId, string logicalClientId, object message)
        {
            using (EneterTrace.Entering())
            {
                bool aDisconnectServiceFlag = false;

                // Get the physical response receiver id for the client.
                string aPhysicalClientResponseReceiverId = null;
                lock (myConnectionsLock)
                {
                    TServiceContext aServiceContext;
                    myConnectedServicesPhysical.TryGetValue(physicalServiceResponseReceiverId, out aServiceContext);
                    if (aServiceContext != null)
                    {
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
                    DisconnectService(physicalServiceResponseReceiverId);
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
                    myClientChannel.SendResponseMessage(physicalClientResponseReceiverId, message);
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to send message to the client.";
                    EneterTrace.Error(anErrorMessage, err);

                    ServiceDisconnectsClient(physicalClientResponseReceiverId);
                }

            }
        }


        



        


        private void SendCloseConnectionMessage(IDuplexInputChannel channel, string physicalResponseReceiverId, string logicalId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object aCloseConnectionMessage = myProtocolFormatter.EncodeCloseConnectionMessage(logicalId);
                    channel.SendResponseMessage(physicalResponseReceiverId, aCloseConnectionMessage);
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to send close connection message.";
                    EneterTrace.Warning(anErrorMessage, err);
                }
            }
        }

        private void SendCloseConnectionMessage(IDuplexInputChannel channel, string physicalResponseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    channel.SendResponseMessage(physicalResponseReceiverId, message);
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to send close connection message.";
                    EneterTrace.Warning(anErrorMessage, err);
                }
            }
        }

        private void CloseConnection(IDuplexInputChannel channel, string physicalResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    channel.DisconnectResponseReceiver(physicalResponseReceiverId);
                }
                catch
                {
                }
            }
        }


        private void AddService(string physicalServiceResponseReceiverId, string logicalServiceId)
        {
            using (EneterTrace.Entering())
            {
                TServiceContext aServiceContext = new TServiceContext();
                aServiceContext.LogicalServiceId = logicalServiceId;
                aServiceContext.PhysicalServiceResponseReceiverId = physicalServiceResponseReceiverId;

                myConnectedServicesPhysical[physicalServiceResponseReceiverId] = aServiceContext;
                myConnectedServicesLogical[logicalServiceId] = aServiceContext;
            }
        }

        private bool AddClient(string physicalClientResponseReceiverId, string logicalClientId)
        {
            using (EneterTrace.Entering())
            {
                if (!string.IsNullOrEmpty(logicalClientId))
                {
                    // If such client does not exist.
                    TClientContext aClientContext;
                    myConnectedClientsPhysical.TryGetValue(physicalClientResponseReceiverId, out aClientContext);
                    if (aClientContext == null)
                    {
                        aClientContext = new TClientContext();
                        aClientContext.LogicalClientId = logicalClientId;

                        myConnectedClientsPhysical.Add(physicalClientResponseReceiverId, aClientContext);
                        return true;
                    }
                    else
                    {
                        string anErrorMessage = TracedObject + "failed to open the connection for the client because the client with the same response receiver id already exist.";
                        EneterTrace.Warning(anErrorMessage);
                    }
                }

                return false;
            }
        }

        private TServiceContext RemoveService(string physicalServiceResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                TServiceContext aServiceContext;
                myConnectedServicesPhysical.TryGetValue(physicalServiceResponseReceiverId, out aServiceContext);
                if (aServiceContext != null)
                {
                    myConnectedServicesPhysical.Remove(physicalServiceResponseReceiverId);
                    myConnectedServicesLogical.Remove(aServiceContext.LogicalServiceId);

                    foreach (string aPhysicalClientResponseReceiverId in aServiceContext.ConnectedClients.Values)
                    {
                        myConnectedClientsPhysical.Remove(aPhysicalClientResponseReceiverId);
                    }
                }

                return aServiceContext;
            }
        }

        private void RemoveClientByPhysical(string physicalClientResponseReceiverId, out string logicalClientId, out string physicalServiceResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                logicalClientId = null;
                physicalServiceResponseReceiverId = null;

                TClientContext aClientContext;
                myConnectedClientsPhysical.TryGetValue(physicalClientResponseReceiverId, out aClientContext);
                if (aClientContext != null)
                {
                    logicalClientId = aClientContext.LogicalClientId;
                    physicalServiceResponseReceiverId = aClientContext.PhysicalServiceResponseReceiverId;

                    // Remove lient from the list of clients.
                    myConnectedClientsPhysical.Remove(physicalClientResponseReceiverId);

                    TServiceContext aServiceContext;
                    myConnectedServicesPhysical.TryGetValue(aClientContext.PhysicalServiceResponseReceiverId, out aServiceContext);
                    if (aServiceContext != null)
                    {
                        // Remove the client from the list of clients connected to this service.
                        aServiceContext.ConnectedClients.Remove(logicalClientId);
                    }
                }
            }
        }

        private void RemoveClientByLogical(string physicalServiceResponseReceiverId, string logicalClientId, out string physicalClientResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                physicalClientResponseReceiverId = null;

                TServiceContext aServiceContext;
                myConnectedServicesPhysical.TryGetValue(physicalServiceResponseReceiverId, out aServiceContext);
                if (aServiceContext != null)
                {
                    aServiceContext.ConnectedClients.TryGetValue(logicalClientId, out physicalClientResponseReceiverId);
                    if (!string.IsNullOrEmpty(physicalClientResponseReceiverId))
                    {
                        // Remove the client.
                        myConnectedClientsPhysical.Remove(physicalClientResponseReceiverId);
                    }

                    // Remove the client from the service.
                    aServiceContext.ConnectedClients.Remove(logicalClientId);
                }
            }
        }

        



        private object myConnectionManipulatorLock = new object();

        // Key is the physical response receiver id of the service.
        private Dictionary<string, TServiceContext> myConnectedServicesPhysical = new Dictionary<string, TServiceContext>();

        // Key is the logic service id
        private Dictionary<string, TServiceContext> myConnectedServicesLogical = new Dictionary<string, TServiceContext>();

        // Key is the physical response receiver id of the client.
        private Dictionary<string, TClientContext> myConnectedClientsPhysical = new Dictionary<string, TClientContext>();
        
        private object myConnectionsLock = new object();


        private IDuplexInputChannel myServiceChannel;
        private IDuplexInputChannel myClientChannel;
        private IProtocolFormatter myProtocolFormatter;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();

        protected string TracedObject { get { return GetType().Name + " "; } }
    }
}
