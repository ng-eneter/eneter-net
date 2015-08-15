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
using System.Linq;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Text;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    internal class MessageBus : IMessageBus
    {
        private class TClientContext
        {
            public TClientContext(string clientResponseReceiverId, string serviceId, string serviceResponseReceiverId)
            {
                ClientResponseReceiverId = clientResponseReceiverId;
                ServiceId = serviceId;
                ServiceResponseReceiverId = serviceResponseReceiverId;
                ForwardToClientThreadDispatcher = new SyncDispatching().GetDispatcher();
                ForwardToServiceThreadDispatcher = new SyncDispatching().GetDispatcher();
            }

            public string ClientResponseReceiverId { get; private set; }
            public string ServiceId { get; private set; }
            public string ServiceResponseReceiverId { get; private set; }
            public IThreadDispatcher ForwardToClientThreadDispatcher { get; private set; }
            public IThreadDispatcher ForwardToServiceThreadDispatcher { get; private set; }
        }

        private class TServiceContext
        {
            public TServiceContext(string serviceId, string serviceResponseReceiverId)
            {
                ServiceId = serviceId;
                ServiceResponseReceiverId = serviceResponseReceiverId;
            }

            public string ServiceId { get; private set; }
            public string ServiceResponseReceiverId { get; private set; }
        }

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
                get { return GetType().Name + " "; }
            }
        }

		public event EventHandler<MessageBusServiceEventArgs> ServiceRegistered;
		public event EventHandler<MessageBusServiceEventArgs> ServiceUnregistered;

        public event EventHandler<MessageBusClientEventArgs> ClientConnected;
        public event EventHandler<MessageBusClientEventArgs> ClientDisconnected;

        public event EventHandler<MessageBusMessageEventArgs> MessageToServiceSent;
        public event EventHandler<MessageBusMessageEventArgs> MessageToClientSent;



        public MessageBus(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
                myServiceConnector = new TConnector();
                myClientConnector = new TConnector();

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
                using (ThreadLock.Lock(myAttachDetachLock))
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
                using (ThreadLock.Lock(myConnectionLock))
                {
                    myConnectedClients.Clear();
                    myConnectedServices.Clear();
                }

                using (ThreadLock.Lock(myAttachDetachLock))
                {
                    myClientConnector.DetachDuplexInputChannel();
                    myServiceConnector.DetachDuplexInputChannel();
                }
            }
        }

		public IEnumerable<string> ConnectedServices
		{
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myConnectionLock))
                    {
                        List<string> aServices = new List<string>();
                        foreach (TServiceContext aServiceContext in myConnectedServices)
                        {
                            aServices.Add(aServiceContext.ServiceId);
                        }

                        return aServices;
                    }
                }
            }
		}

        public IEnumerable<string> GetConnectedClients(string serviceAddress)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionLock))
                {
                    List<string> aClients = new List<string>();
                    foreach (TClientContext aClientContext in myConnectedClients)
                    {
                        if (aClientContext.ServiceId == serviceAddress)
                        {
                            aClients.Add(aClientContext.ClientResponseReceiverId);
                        }
                    }
                    return aClients;
                }
            }
        }

        public int GetNumberOfConnectedClients(string serviceAddress)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionLock))
                {
                    int aCount = 0;
                    foreach (TClientContext aClientContext in myConnectedClients)
                    {
                        if (aClientContext.ServiceId == serviceAddress)
                        {
                            ++aCount;
                        }
                    }

                    return aCount;
                }
            }
        }

		public void DisconnectService(string serviceAddress)
		{
			using (EneterTrace.Entering())
			{
				UnregisterService(serviceAddress);
			}
		}

        // Connection with the client was closed.
        private void OnClientDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                UnregisterClient(e.ResponseReceiverId, true, false);
            }
        }

        // A message from the client was received.
        private void OnMessageFromClientReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                MessageBusMessage aMessageBusMessage;
                try
                {
                    aMessageBusMessage = mySerializer.Deserialize<MessageBusMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize message from service. The service will be disconnected.", err);
                    UnregisterClient(e.ResponseReceiverId, true, true);
                    return;
                }

                if (aMessageBusMessage.Request == EMessageBusRequest.ConnectClient)
                {
                    EneterTrace.Debug("CLIENT OPENS CONNECTION TO '" + aMessageBusMessage.Id + "'.");
                    RegisterClient(e.ResponseReceiverId, aMessageBusMessage.Id);
                }
                else if (aMessageBusMessage.Request == EMessageBusRequest.SendRequestMessage)
                {
                    ForwardMessageToService(e.ResponseReceiverId, aMessageBusMessage);
                }
            }
        }

        private void RegisterClient(string clientResponseReceiverId, string serviceId)
        {
            using (EneterTrace.Entering())
            {
                bool anIsNewClientConnected = false;
                TClientContext aClientContext = null;
                using (ThreadLock.Lock(myConnectionLock))
                {
                    aClientContext = myConnectedClients.FirstOrDefault(x => x.ClientResponseReceiverId == clientResponseReceiverId);

                    // If such client does not exist yet then create it.
                    if (aClientContext == null)
                    {
                        TServiceContext aServiceContext = myConnectedServices.FirstOrDefault(x => x.ServiceId == serviceId);

                        // If requested service exists.
                        if (aServiceContext != null)
                        {
                            aClientContext = new TClientContext(clientResponseReceiverId, serviceId, aServiceContext.ServiceResponseReceiverId);
                            myConnectedClients.Add(aClientContext);
                            anIsNewClientConnected = true;
                        }
                    }
                }

                if (anIsNewClientConnected)
                {
                    // Send open connection message to the service.
                    try
                    {
                        MessageBusMessage aMessage = new MessageBusMessage(EMessageBusRequest.ConnectClient, clientResponseReceiverId, null);
                        object aSerializedMessage = mySerializer.Serialize<MessageBusMessage>(aMessage);

                        IDuplexInputChannel anInputChannel = myServiceConnector.AttachedDuplexInputChannel;
                        if (anInputChannel != null)
                        {
                            anInputChannel.SendResponseMessage(aClientContext.ServiceResponseReceiverId, aSerializedMessage);

                            if (ClientConnected != null)
                            {
                                MessageBusClientEventArgs anEvent = new MessageBusClientEventArgs(serviceId, aClientContext.ServiceResponseReceiverId, clientResponseReceiverId);

                                try
                                {
                                    ClientConnected(this, anEvent);
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to send open connection message to the service '" + aClientContext.ServiceId + "'.", err);

                        // Note: The service should not be disconnected from the message bus when not available.
                        //       Because it can be "just" overloaded. So only this new client will be disconnected from the message bus.
                        UnregisterClient(clientResponseReceiverId, false, true);
                    }
                }
                else
                {
                    if (aClientContext != null)
                    {
                        EneterTrace.Warning(TracedObject + "failed to connect the client already exists. The connection will be closed.");
                        UnregisterClient(clientResponseReceiverId, false, true);
                    }
                    else
                    {
                        EneterTrace.Warning(TracedObject + "failed to connec the client because the service '" + serviceId + "' does not exist. The connection will be closed.");
                        UnregisterClient(clientResponseReceiverId, false, true);
                    }
                }
            }
        }

        private void UnregisterClient(string clientResponseReceiverId, bool sendCloseConnectionToServiceFlag, bool disconnectClientFlag)
        {
            using (EneterTrace.Entering())
            {
                // Unregistering client. 
                TClientContext aClientContext = null;
                using (ThreadLock.Lock(myConnectionLock))
                {
                    myConnectedClients.RemoveWhere(x =>
                        {
                            if (x.ClientResponseReceiverId == clientResponseReceiverId)
                            {
                                aClientContext = x;
                                return true;
                            }

                            return false;
                        });
                }

                if (aClientContext != null)
                {
                    if (sendCloseConnectionToServiceFlag)
                    {
                        try
                        {
                            // Send close connection message to the service.
                            MessageBusMessage aMessage = new MessageBusMessage(EMessageBusRequest.DisconnectClient, aClientContext.ClientResponseReceiverId, null);
                            object aSerializedMessage = mySerializer.Serialize<MessageBusMessage>(aMessage);

                            IDuplexInputChannel anInputChannel = myServiceConnector.AttachedDuplexInputChannel;
                            if (anInputChannel != null)
                            {
                                anInputChannel.SendResponseMessage(aClientContext.ServiceResponseReceiverId, aSerializedMessage);
                            }
                        }
                        catch (Exception err)
                        {
                            string anErrorMessage = TracedObject + ErrorHandler.FailedToCloseConnection;
                            EneterTrace.Warning(anErrorMessage, err);
                        }
                    }

                    // Disconnecting the client.
                    if (disconnectClientFlag)
                    {
                        IDuplexInputChannel anInputChannel1 = myClientConnector.AttachedDuplexInputChannel;
                        if (anInputChannel1 != null)
                        {
                            anInputChannel1.DisconnectResponseReceiver(aClientContext.ClientResponseReceiverId);
                        }
                    }

                    if (ClientDisconnected != null)
                    {
                        MessageBusClientEventArgs anEventArgs = new MessageBusClientEventArgs(aClientContext.ServiceId, aClientContext.ServiceResponseReceiverId, clientResponseReceiverId);
                        try
                        {
                            ClientDisconnected(this, anEventArgs);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }
            }
        }

        private void ForwardMessageToService(string clientResponseReceiverId, MessageBusMessage messageFromClient)
        {
            using (EneterTrace.Entering())
            {
                TClientContext aClientContext = null;
                using (ThreadLock.Lock(myConnectionLock))
                {
                    aClientContext = myConnectedClients.FirstOrDefault(x => x.ClientResponseReceiverId == clientResponseReceiverId);
                }

                if (aClientContext != null)
                {
                    // Forward the incoming message to the service.
                    IDuplexInputChannel anInputChannel = myServiceConnector.AttachedDuplexInputChannel;
                    if (anInputChannel != null)
                    {
                        aClientContext.ForwardToServiceThreadDispatcher.Invoke(
                            () =>
                            {
                                using (EneterTrace.Entering())
                                {
                                    try
                                    {
                                        // Add the client id into the message.
                                        // Note: Because of security reasons we do not expect Ids from the client but using Ids associated with the connection session.
                                        //       Otherwise it would be possible that some client could use id of another client to pretend a different client.
                                        messageFromClient.Id = clientResponseReceiverId;
                                        object aSerializedMessage = mySerializer.Serialize<MessageBusMessage>(messageFromClient);

                                        anInputChannel.SendResponseMessage(aClientContext.ServiceResponseReceiverId, aSerializedMessage);

                                        if (MessageToServiceSent != null)
                                        {
                                            MessageBusMessageEventArgs anEventArgs = new MessageBusMessageEventArgs(aClientContext.ServiceId, aClientContext.ServiceResponseReceiverId, clientResponseReceiverId, messageFromClient.MessageData);
                                            try
                                            {
                                                MessageToServiceSent(this, anEventArgs);
                                            }
                                            catch (Exception err)
                                            {
                                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                            }
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        string anErrorMessage = TracedObject + "failed to send message to the service '" + aClientContext.ServiceId + "'.";
                                        EneterTrace.Error(anErrorMessage, err);

                                        UnregisterService(aClientContext.ServiceResponseReceiverId);
                                    }
                                }
                            });
                    }
                }
                else
                {
                    string anErrorMessage = TracedObject + "failed to send message to the service because the client was not found.";
                    EneterTrace.Warning(anErrorMessage);
                }
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
                MessageBusMessage aMessageBusMessage;
                try
                {
                    aMessageBusMessage = mySerializer.Deserialize<MessageBusMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize message from service. The service will be disconnected.", err);
                    UnregisterService(e.ResponseReceiverId);
                    return;
                }

                if (aMessageBusMessage.Request == EMessageBusRequest.RegisterService)
                {
                    EneterTrace.Debug("REGISTER SERVICE: " + aMessageBusMessage.Id);
                    RegisterService(aMessageBusMessage.Id, e.ResponseReceiverId);
                }
                else if (aMessageBusMessage.Request == EMessageBusRequest.SendResponseMessage)
                {
                    // Note: forward the same message - it does not have to be serialized again.
                    ForwardMessageToClient(aMessageBusMessage.Id, e.ResponseReceiverId, e.Message, aMessageBusMessage.MessageData);
                }
                else if (aMessageBusMessage.Request == EMessageBusRequest.DisconnectClient)
                {
                    EneterTrace.Debug("SERVICE DISCONNECTs CLIENT");
                    UnregisterClient(aMessageBusMessage.Id, false, true);
                }
                else if (aMessageBusMessage.Request == EMessageBusRequest.ConfirmClient)
                {
                    EneterTrace.Debug("SERVICE CONFIRMS CLIENT");
                    ForwardMessageToClient(aMessageBusMessage.Id, e.ResponseReceiverId, e.Message, null);
                }
            }
        }

        private void RegisterService(string serviceId, string serviceResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                bool anIsNewServiceRegistered = false;
                TServiceContext aServiceContext = null;
                using (ThreadLock.Lock(myConnectionLock))
                {
                    aServiceContext = myConnectedServices.FirstOrDefault(x => x.ServiceId == serviceId || x.ServiceResponseReceiverId == serviceResponseReceiverId);
                    if (aServiceContext == null)
                    {
                        aServiceContext = new TServiceContext(serviceId, serviceResponseReceiverId);
                        myConnectedServices.Add(aServiceContext);
                        anIsNewServiceRegistered = true;
                    }
                }

                if (anIsNewServiceRegistered)
                {
                    if (ServiceRegistered != null)
                    {
                        try
                        {
                            MessageBusServiceEventArgs anEvent = new MessageBusServiceEventArgs(serviceId, serviceResponseReceiverId);
                            ServiceRegistered(this, anEvent);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }
                else
                {
                    // If this connection has registered the same service then do nothing.
                    if (aServiceContext.ServiceId == serviceId &&
                        aServiceContext.ServiceResponseReceiverId == serviceResponseReceiverId)
                    {
                    }
                    else if (aServiceContext.ServiceId != serviceId &&
                        aServiceContext.ServiceResponseReceiverId == serviceResponseReceiverId)
                    {
                        EneterTrace.Warning("The connection has already registered a different service '" + aServiceContext.ServiceId + "'. Connection will be disconnected.");
                        UnregisterService(serviceResponseReceiverId);
                    }
                    else if (aServiceContext.ServiceId == serviceId &&
                             aServiceContext.ServiceResponseReceiverId != serviceResponseReceiverId)
                    {
                        EneterTrace.Warning("Service '" + serviceId + "' is already registered. Connection will be disconnected.");
                        UnregisterService(serviceResponseReceiverId);
                    }
                }
            }
        }

        private void UnregisterService(string serviceResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                List<string> aClientsToDisconnect = new List<string>();

                string aServiceId = null;
                using (ThreadLock.Lock(myConnectionLock))
                {
                    // Remove the service.
                    myConnectedServices.RemoveWhere(x =>
                        {
                            if (x.ServiceResponseReceiverId == serviceResponseReceiverId)
                            {
                                aServiceId = x.ServiceId;
                                return true;
                            }

                            return false;
                        });

                    // Remove all clients connected to the service.
                    myConnectedClients.RemoveWhere(x =>
                        {
                            if (x.ServiceResponseReceiverId == serviceResponseReceiverId)
                            {
                                aClientsToDisconnect.Add(x.ClientResponseReceiverId);

                                // Indicate the item shall be removed.
                                return true;
                            }

                            return false;
                        });
                }

                // Close connections with clients.
                if (myClientConnector.IsDuplexInputChannelAttached)
                {
                    foreach (string aClientResponseReceiverId in aClientsToDisconnect)
                    {
                        IDuplexInputChannel anInputChannel = myClientConnector.AttachedDuplexInputChannel;
                        if (anInputChannel != null)
                        {
                            anInputChannel.DisconnectResponseReceiver(aClientResponseReceiverId);
                        }
                    }
                }

                IDuplexInputChannel anInputChannel2 = myServiceConnector.AttachedDuplexInputChannel;
                if (anInputChannel2 != null)
                {
                    anInputChannel2.DisconnectResponseReceiver(serviceResponseReceiverId);
                }

                if (ServiceUnregistered != null && !string.IsNullOrEmpty(aServiceId))
                {
                    EneterTrace.Debug("SERVICE '" + aServiceId + "' UNREGISTERED");

                    try
                    {
                        MessageBusServiceEventArgs anEvent = new MessageBusServiceEventArgs(aServiceId, serviceResponseReceiverId);
                        ServiceUnregistered(this, anEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void ForwardMessageToClient(string clientResponseReceiverId, string serviceResponseReceiverId, object serializedMessage, object originalMessage)
        {
            using (EneterTrace.Entering())
            {
                // Check if the requested client id has a connection with the service session which forwards the message.
                // Note: this is to prevent that a sevice sends a message to a client which is not connected to it.
                TClientContext aClientContext;
                using (ThreadLock.Lock(myConnectionLock))
                {
                    aClientContext = myConnectedClients.FirstOrDefault(x => x.ClientResponseReceiverId == clientResponseReceiverId && x.ServiceResponseReceiverId == serviceResponseReceiverId);
                }

                if (aClientContext == null)
                {
                    // The associated client does not exist and the message canno be sent.
                    EneterTrace.Warning(TracedObject + "failed to forward the message to client because the client was not found.");
                    return;
                }

                IDuplexInputChannel anInputChannel = myClientConnector.AttachedDuplexInputChannel;
                if (anInputChannel != null)
                {
                    // Invoke sending of the message in the client particular thread.
                    // So that e.g. if there are communication problems sending to other clients
                    // is not affected.
                    aClientContext.ForwardToClientThreadDispatcher.Invoke(
                        () =>
                        {
                            using (EneterTrace.Entering())
                            {
                                try
                                {
                                    anInputChannel.SendResponseMessage(clientResponseReceiverId, serializedMessage);

                                    if (originalMessage != null && MessageToClientSent != null)
                                    {
                                        MessageBusMessageEventArgs anEventArgs = new MessageBusMessageEventArgs(aClientContext.ServiceId, serviceResponseReceiverId, clientResponseReceiverId, originalMessage);
                                        try
                                        {
                                            MessageToClientSent(this, anEventArgs);
                                        }
                                        catch (Exception err)
                                        {
                                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                        }
                                    }
                                }
                                catch (Exception err)
                                {
                                    string anErrorMessage = TracedObject + "failed to send message to the client.";
                                    EneterTrace.Error(anErrorMessage, err);

                                    UnregisterClient(aClientContext.ClientResponseReceiverId, true, true);
                                }
                            }
                        });
                }
            }
        }


        private object myAttachDetachLock = new object();
        private object myConnectionLock = new object();
        private HashSet<TServiceContext> myConnectedServices = new HashSet<TServiceContext>();
        private HashSet<TClientContext> myConnectedClients = new HashSet<TClientContext>();

        private ISerializer mySerializer;
        private TConnector myServiceConnector;
        private TConnector myClientConnector;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif