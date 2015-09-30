/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Collections.Generic;

namespace Eneter.Messaging.Nodes.HolePunching
{
    internal class RendezvousService : IRendezvousService
    {
        private class TRendezvousContext
        {
            public TRendezvousContext(string rendezvousId, string ipAddressAndPort, string responseReceiverId)
            {
                RendezvousId = rendezvousId;
                IpAddressAndPort = ipAddressAndPort;
                ResponseReceiverId = responseReceiverId;
            }

            public string RendezvousId { get; private set; }
            public string IpAddressAndPort { get; private set; }
            public string ResponseReceiverId { get; private set; }
        }

        public RendezvousService(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                DuplexTypedMessagesFactory aFactory = new DuplexTypedMessagesFactory(serializer);
                myReceiver = aFactory.CreateDuplexTypedMessageReceiver<RendezvousMessage, RendezvousMessage>();
                myReceiver.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                myReceiver.MessageReceived += OnMessageReceived;
            }
        }


        public void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                myReceiver.AttachDuplexInputChannel(duplexInputChannel);
            }
        }

        public void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                myReceiver.DetachDuplexInputChannel();
            }
        }

        public bool IsDuplexInputChannelAttached
        {
            get { return myReceiver.IsDuplexInputChannelAttached; }
        }

        public IDuplexInputChannel AttachedDuplexInputChannel
        {
            get { return myReceiver.AttachedDuplexInputChannel; }
        }


        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                UnregisterResponseReceiver(e.ResponseReceiverId);
            }
        }

        private void OnMessageReceived(object sender, TypedRequestReceivedEventArgs<RendezvousMessage> e)
        {
            using (EneterTrace.Entering())
            {
                if (e.ReceivingError != null)
                {
                    EneterTrace.Warning(TracedObject + "could not process the request.", e.ReceivingError);
                    return;
                }

                // If service registers in Rendezvous service.
                if (e.RequestMessage.MessageType == ERendezvousMessage.RegisterRequest)
                {
                    Register(e.RequestMessage.MessageData, e.SenderAddress, e.ResponseReceiverId);
                }
                // If client asks Rendezvous service for serivice IP address and port.
                else if (e.RequestMessage.MessageType == ERendezvousMessage.GetAddressRequest)
                {
                    ProvideAddress(e.RequestMessage.MessageData, e.SenderAddress, e.ResponseReceiverId);
                }
            }
        }

        private void Register(string rendezvousId, string ipAddressAndPort, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(rendezvousId))
                {
                    EneterTrace.Error(TracedObject + "could not register empty string of rendezvousId.");
                    return;
                }

                if (string.IsNullOrEmpty(ipAddressAndPort))
                {
                    EneterTrace.Error(TracedObject + "could not register empty string of ipAddressAndPort.");
                    return;
                }

                using (ThreadLock.Lock(myRendezvousManipulatorLock))
                {
                    // If the rendezvous id already exists but is associated with another response receiver id then disconnect
                    // this response receiver.
                    TRendezvousContext aContext;
                    myRendezvousIds.TryGetValue(rendezvousId, out aContext);
                    if (aContext != null && aContext.ResponseReceiverId != responseReceiverId)
                    {
                        UnregisterResponseReceiver(responseReceiverId);
                        myReceiver.AttachedDuplexInputChannel.DisconnectResponseReceiver(responseReceiverId);
                        return;
                    }

                    aContext = new TRendezvousContext(rendezvousId, ipAddressAndPort, responseReceiverId);

                    // Associate the rendezvous context with rendezvous id.
                    myRendezvousIds[rendezvousId] = aContext;

                    // Associate the rendezvous context with response receiver id.
                    List<TRendezvousContext> aResponseReceiverContexts;
                    myRendezvousResponseReceiverIds.TryGetValue(responseReceiverId, out aResponseReceiverContexts);
                    if (aResponseReceiverContexts == null)
                    {
                        aResponseReceiverContexts = new List<TRendezvousContext>();
                        myRendezvousResponseReceiverIds[responseReceiverId] = aResponseReceiverContexts;
                    }
                    aResponseReceiverContexts.Add(aContext);
                }

                RendezvousMessage aResponse = new RendezvousMessage();
                aResponse.MessageType = ERendezvousMessage.RegisterResponse;
                aResponse.MessageData = ipAddressAndPort;
                myReceiver.SendResponseMessage(responseReceiverId, aResponse);
            }
        }

        private void UnregisterResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myRendezvousManipulatorLock))
                {
                    // Get all rendezvous ids associted with response receiver id.
                    List<TRendezvousContext> aRegisteredIds;
                    myRendezvousResponseReceiverIds.TryGetValue(responseReceiverId, out aRegisteredIds);
                    if (aRegisteredIds != null)
                    {
                        // Go via rendezvous ids and release them.
                        foreach (TRendezvousContext aRendezvousContext in aRegisteredIds)
                        {
                            myRendezvousIds.Remove(aRendezvousContext.RendezvousId);
                        }

                        // And finaly remove the response receiver id.
                        myRendezvousResponseReceiverIds.Remove(responseReceiverId);
                    }
                }
            }
        }

        private void ProvideAddress(string rendezvousId, string clientIpAddressAndPort, string clientResponseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                string aServiceIpAddressAndPort = "";
                string aServiceResponseReceiverId = null;
                using (ThreadLock.Lock(myRendezvousManipulatorLock))
                {
                    TRendezvousContext aContext;
                    myRendezvousIds.TryGetValue(rendezvousId, out aContext);
                    if (aContext != null)
                    {
                        aServiceIpAddressAndPort = aContext.IpAddressAndPort;
                        aServiceResponseReceiverId = aContext.ResponseReceiverId;
                    }
                }

                // If the service is connected.
                if (!string.IsNullOrEmpty(aServiceResponseReceiverId))
                {
                    // 1st send client's public IP address and port to the service.
                    // So that service can drill the hole for the upcoming connection from the client.
                    RendezvousMessage aResponseToService = new RendezvousMessage();
                    aResponseToService.MessageType = ERendezvousMessage.Drill;
                    aResponseToService.MessageData = clientIpAddressAndPort;
                    myReceiver.SendResponseMessage(aServiceResponseReceiverId, aResponseToService);
                }
                
                // Send service's public IP address and port to the client.
                RendezvousMessage aResponse = new RendezvousMessage();
                aResponse.MessageType = ERendezvousMessage.GetAddressResponse;
                aResponse.MessageData = aServiceIpAddressAndPort;
                myReceiver.SendResponseMessage(clientResponseReceiverId, aResponse);
            }
        }

        


        private IDuplexTypedMessageReceiver<RendezvousMessage, RendezvousMessage> myReceiver;

        private object myRendezvousManipulatorLock = new object();
        private Dictionary<string, TRendezvousContext> myRendezvousIds = new Dictionary<string, TRendezvousContext>();
        private Dictionary<string, List<TRendezvousContext>> myRendezvousResponseReceiverIds = new Dictionary<string, List<TRendezvousContext>>();

        private string TracedObject
        {
            get { return GetType().Name + " "; }
        }
    }
}
