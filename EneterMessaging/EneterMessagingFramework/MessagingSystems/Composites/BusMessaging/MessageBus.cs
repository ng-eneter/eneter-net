using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using System.IO;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    internal class MessageBus
    {
        private class TServiceContext
        {
            public TServiceContext()
            {
                ConnectedClients = new Dictionary<string, string>();
            }

            public string LogicalServiceId { get; set; }
            public string PhysicalResponseReceiverId { get; set; }

            // Key is logical client id inside the message bus.
            // Value is physical client response receiver id.
            public Dictionary<string, string> ConnectedClients { get; private set; }
        }

        private class TClientContext
        {
            public string LogicalClientId { get; set; }
            public string LogicalServiceId { get; set; }
        }


        public MessageBus()
        {
            using (EneterTrace.Entering())
            {
            }
        }

        

        public void AttachDuplexInputChannel(IDuplexInputChannel serviceInputChannel, IDuplexInputChannel clientInputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    
                }
            }
        }

        public void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                   
                }
            }
        }



        private void OnServiceDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                DisconnectAllClients(e.ResponseReceiverId);
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
                        // A service connects to the message bus.
                        if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                        {
                            string aPhysicalServiceResponseReceiverId = e.ResponseReceiverId;
                            string aLogicalServiceId = aProtocolMessage.ResponseReceiverId;

                            bool anIsDuplicate = false;

                            lock (myConnectedServicesLock)
                            {
                                // If the service is not connected yet.
                                if (!myConnectedServicesLogical.ContainsKey(aLogicalServiceId))
                                {
                                    AddService(aPhysicalServiceResponseReceiverId, aLogicalServiceId);
                                }
                                else
                                {
                                    anIsDuplicate = true;
                                }
                            }

                            if (anIsDuplicate)
                            {
                                string anErrorMessage = TracedObject + "failed to connect the service because the service '" + aProtocolMessage.ResponseReceiverId + "' is already connected. The connection will be closed.";
                                EneterTrace.Warning(anErrorMessage);

                                // Close connection that tries to connect the duplicated service.
                                // Note: notice there are no connected clients in this case!
                                CloseConnection(myServiceChannel, aPhysicalServiceResponseReceiverId);
                            }
                        }
                        // A service wants to disconnect a client.
                        else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                        {
                            string aPhysicalServiceResponseReceiverId = e.ResponseReceiverId;
                            string aLogicalClientId = aProtocolMessage.ResponseReceiverId;
                            string aPhysicalClientResponseReceiverId = null;

                            // Get the service context.
                            TServiceContext aServiceContext;
                            lock (myConnectedServicesLock)
                            {
                                myConnectedServicesPhysical.TryGetValue(aPhysicalServiceResponseReceiverId, out aServiceContext);
                                if (aServiceContext != null)
                                {
                                    // Get response receiver id of the client which shall be disconnected.
                                    aServiceContext.ConnectedClients.TryGetValue(aLogicalClientId, out aPhysicalClientResponseReceiverId);

                                    // Remove the client from the list of clients connected to this service.
                                    aServiceContext.ConnectedClients.Remove(aLogicalClientId);

                                    // Remove the client from the list of clients.
                                    if (aPhysicalClientResponseReceiverId != null)
                                    {
                                        myConnectedClientsPhysical.Remove(aPhysicalClientResponseReceiverId);
                                    }
                                }
                            }

                            if (aServiceContext == null)
                            {
                                // Service context was not found. Something is wrong.
                                // This service connection will be closed.
                                string anErrorMessage = TracedObject + "failed to disconnect the client because the service was not found. The service will be disconnected..";
                                EneterTrace.Warning(anErrorMessage);

                                CloseConnection(myServiceChannel, aPhysicalServiceResponseReceiverId);
                            }

                            // Disconnect desired client.
                            if (!string.IsNullOrEmpty(aPhysicalClientResponseReceiverId))
                            {
                                SendCloseConnectionMessage(myClientChannel, aPhysicalClientResponseReceiverId, e.Message);
                                CloseConnection(myClientChannel, aPhysicalClientResponseReceiverId);
                            }
                        }
                        // A service sends a response message to a client.
                        else if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                        {
                            string aPhysicalServiceResponseReceiverId = e.ResponseReceiverId;
                            string aLogicalClientId = aProtocolMessage.ResponseReceiverId;
                            string aPhysicalClientResponseReceiverId = null;

                            // Get the physical response receiver id for the client.
                            TServiceContext aServiceContext;
                            lock (myConnectedServicesLock)
                            {
                                myConnectedServicesPhysical.TryGetValue(aPhysicalServiceResponseReceiverId, out aServiceContext);
                                if (aServiceContext != null)
                                {
                                    aServiceContext.ConnectedClients.TryGetValue(aLogicalClientId, out aPhysicalClientResponseReceiverId);
                                }
                            }

                            if (aServiceContext == null)
                            {
                                // Service context was not found. Something is wrong.
                                // This service connection will be closed.
                                string anErrorMessage = TracedObject + "failed to send the message because the service was not found. The service will be disconnected.";
                                EneterTrace.Warning(anErrorMessage);

                                CloseConnection(myServiceChannel, aPhysicalServiceResponseReceiverId);
                            }

                            if (!string.IsNullOrEmpty(aPhysicalClientResponseReceiverId))
                            {
                                try
                                {
                                    // Forward the message to the client.
                                    myClientChannel.SendResponseMessage(aPhysicalClientResponseReceiverId, e.Message);
                                }
                                catch (Exception err)
                                {
                                    string anErrorMessage = TracedObject + "failed to send the message to the client. The client will be disconnected.";
                                    EneterTrace.Error(anErrorMessage, err);

                                    lock (myConnectedServicesLock)
                                    {
                                        RemoveClient(aPhysicalClientResponseReceiverId, aLogicalClientId, aServiceContext);
                                    }

                                    CloseConnection(myClientChannel, aPhysicalClientResponseReceiverId);
                                }
                            }
                        }
                        else if (aProtocolMessage.MessageType == EProtocolMessageType.Unknown)
                        {
                            string anErrorMessage = TracedObject + "detected incorrect message format. The service will be disconnected.";
                            EneterTrace.Warning(anErrorMessage);

                            CloseConnection(myServiceChannel, e.ResponseReceiverId);
                            DisconnectAllClients(e.ResponseReceiverId);
                        }
                    }
                    else
                    {
                        // The ProtocolMessage was null. It means the connection was closed.
                        CloseConnection(myServiceChannel, e.ResponseReceiverId);
                        DisconnectAllClients(e.ResponseReceiverId);
                    }

                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to receive a message from the service. The service will be disconnected.", err);

                    CloseConnection(myServiceChannel, e.ResponseReceiverId);
                    DisconnectAllClients(e.ResponseReceiverId);
                }
            }
        }




        private void OnClientDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                string aPhysicalServiceResponseReceiverId = null;
                string aLogicalClientId = null;

                lock (myConnectedServicesLock)
                {
                    // Get the client context.
                    TClientContext aClientContext;
                    myConnectedClientsPhysical.TryGetValue(e.ResponseReceiverId, out aClientContext);

                    // Get the service context.
                    if (aClientContext != null)
                    {
                        // Get the logical client id.
                        aLogicalClientId = aClientContext.LogicalClientId;

                        // Remove disconnected client from the list of clients.
                        myConnectedClientsPhysical.Remove(e.ResponseReceiverId);

                        TServiceContext aServiceContext;
                        myConnectedServicesLogical.TryGetValue(aClientContext.LogicalServiceId, out aServiceContext);
                        if (aServiceContext != null)
                        {
                            // Get response receiver id of the service.
                            aPhysicalServiceResponseReceiverId = aServiceContext.PhysicalResponseReceiverId;

                            // Remove the client from the list of clients connected to this service.
                            aServiceContext.ConnectedClients.Remove(aLogicalClientId);
                        }
                    }
                }

                if (aLogicalClientId != null && aPhysicalServiceResponseReceiverId != null)
                {
                    // Notify the service the client is disconnected.
                    SendCloseConnectionMessage(myServiceChannel, aPhysicalServiceResponseReceiverId, aLogicalClientId);
                }
            }
        }

        private void OnMessageFromClientReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(e.Message);
                if (aProtocolMessage != null)
                {
                    // Client wants to connect to the service.
                    if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                    {
                        string aPhysicalClientResponseReceiverId = e.ResponseReceiverId;
                        string aLogicalClientId = aProtocolMessage.ResponseReceiverId;
                        string aLogicalServiceId = aProtocolMessage.
                        

                        bool anIsClientAdded;
                        lock (myConnectedServicesLock)
                        {
                            anIsClientAdded = AddClient(
                        }
                    }
                    // Client wants to close connection with the service.
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                    }
                    // Client sends a message to the service.
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                    {
                    }
                    else if (aProtocolMessage.MessageType == EProtocolMessageType.Unknown)
                    {
                    }
                }
            }
        }


        private void DisconnectAllClients(string physicalServiceResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                TServiceContext aServiceContext;
                lock (myConnectedServicesLock)
                {
                    aServiceContext = RemoveService(physicalServiceResponseReceiverId);
                }

                // Disconnect all connected clients.
                foreach (KeyValuePair<string, string> aClientContext in aServiceContext.ConnectedClients)
                {
                    SendCloseConnectionMessage(myClientChannel, aClientContext.Value, aClientContext.Key);
                    CloseConnection(myClientChannel, aClientContext.Value);
                }

                aServiceContext.ConnectedClients.Clear();
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
                aServiceContext.PhysicalResponseReceiverId = physicalServiceResponseReceiverId;

                myConnectedServicesLogical[logicalServiceId] = aServiceContext;
                myConnectedServicesPhysical[physicalServiceResponseReceiverId] = aServiceContext;
            }
        }

        private bool AddClient(string physicalClientResponseReceiverId, string logicalClientId, string logicalServiceId)
        {
            using (EneterTrace.Entering())
            {
                TServiceContext aServiceContext;
                myConnectedServicesLogical.TryGetValue(logicalServiceId, out aServiceContext);
                if (aServiceContext != null)
                {
                    if (!aServiceContext.ConnectedClients.ContainsKey(logicalClientId))
                    {
                        aServiceContext.ConnectedClients[logicalClientId] = physicalClientResponseReceiverId;
                        return true;
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

        private void RemoveClient(string physicalClientResponseReceiverId, string logicalClientId, TServiceContext serviceContext)
        {
            using (EneterTrace.Entering())
            {
                myConnectedClientsPhysical.Remove(physicalClientResponseReceiverId);
                serviceContext.ConnectedClients.Remove(logicalClientId);
            }
        }



        private object myConnectionManipulatorLock = new object();

        // Key is logical service id inside the message bus.
        private Dictionary<string, TServiceContext> myConnectedServicesLogical = new Dictionary<string, TServiceContext>();
        
        // Key is the physical response receiver id of the service.
        private Dictionary<string, TServiceContext> myConnectedServicesPhysical = new Dictionary<string, TServiceContext>();

        // Key is the physical response receiver id of the client.
        private Dictionary<string, TClientContext> myConnectedClientsPhysical = new Dictionary<string, TClientContext>();
        
        private object myConnectedServicesLock = new object();


        private IDuplexInputChannel myServiceChannel;
        private IDuplexInputChannel myClientChannel;
        private IProtocolFormatter myProtocolFormatter;

        protected string TracedObject { get { return GetType().Name + " "; } }
    }
}
