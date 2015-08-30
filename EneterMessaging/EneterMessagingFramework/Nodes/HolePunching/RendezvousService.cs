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
using System.Linq;

namespace Eneter.Messaging.Nodes.HolePunching
{
    internal class RendezvousService : IRendezvousService
    {
        private class TRendezvousContext
        {
            public TRendezvousContext(string responseReceiverId, string rendezvousId, string ipAddressAndPort)
            {
                ResponseReceiverId = responseReceiverId;
                RendezvousId = rendezvousId;
                IpAddressAndPort = ipAddressAndPort;
            }

            public string RendezvousId { get; private set; }
            public string ResponseReceiverId { get; private set; }
            public string IpAddressAndPort { get; private set; }
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
                using (ThreadLock.Lock(myRegisteredEndPoints))
                {
                    int anIdx = myRegisteredEndPoints.FindIndex(x => x.ResponseReceiverId == e.ResponseReceiverId);
                    myRegisteredEndPoints.RemoveAt(anIdx);
                }
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

                if (e.RequestMessage.MessageType == ERendezvousMessage.RegisterRequest)
                {
                    Register(e.RequestMessage.MessageData[0], e.SenderAddress, e.ResponseReceiverId);
                }
                else if (e.RequestMessage.MessageType == ERendezvousMessage.GetAddressRequest)
                {
                    ProvideAddress(e.RequestMessage.MessageData[0], e.ResponseReceiverId);
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

                TRendezvousContext aContext = new TRendezvousContext(responseReceiverId, rendezvousId, ipAddressAndPort);
                using (ThreadLock.Lock(myRegisteredEndPoints))
                {
                    myRegisteredEndPoints.Add(aContext);
                }

                RendezvousMessage aResponse = new RendezvousMessage();
                aResponse.MessageType = ERendezvousMessage.AddressResponse;
                aResponse.MessageData = new string[] { ipAddressAndPort };
                myReceiver.SendResponseMessage(responseReceiverId, aResponse);
            }
        }

        private void ProvideAddress(string rendezvousId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                string[] anAvailableEndPoints;
                using (ThreadLock.Lock(myRegisteredEndPoints))
                {
                    anAvailableEndPoints = myRegisteredEndPoints.Where(x => x.ResponseReceiverId == responseReceiverId)
                        .Select(x => x.IpAddressAndPort).ToArray();
                }

                RendezvousMessage aResponse = new RendezvousMessage();
                aResponse.MessageType = ERendezvousMessage.AddressResponse;
                aResponse.MessageData = anAvailableEndPoints;
                myReceiver.SendResponseMessage(responseReceiverId, aResponse);
            }
        }


        private IDuplexTypedMessageReceiver<RendezvousMessage, RendezvousMessage> myReceiver;

        private List<TRendezvousContext> myRegisteredEndPoints = new List<TRendezvousContext>();

        private string TracedObject
        {
            get { return GetType().Name + " "; }
        }
    }
}
