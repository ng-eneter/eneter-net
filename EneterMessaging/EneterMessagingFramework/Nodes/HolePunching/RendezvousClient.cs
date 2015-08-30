using Eneter.Messaging.DataProcessing.Serializing;
/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/


using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;

namespace Eneter.Messaging.Nodes.HolePunching
{
    internal class RendezvousClient : IRendezvousClient
    {
        public RendezvousClient(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                IDuplexTypedMessagesFactory aFactory = new DuplexTypedMessagesFactory(serializer);
                myRendezvousSender = aFactory.CreateSyncDuplexTypedMessageSender<RendezvousMessage, RendezvousMessage>();
            }
        }

        public string Register(string rendezvousId)
        {
            using (EneterTrace.Entering())
            {
                RendezvousMessage aRequest = new RendezvousMessage();
                aRequest.MessageType = ERendezvousMessage.RegisterRequest;
                aRequest.MessageData = new string[] { rendezvousId };

                RendezvousMessage aResponse = myRendezvousSender.SendRequestMessage(aRequest);

                if (aResponse.MessageType != ERendezvousMessage.AddressResponse)
                {
                    throw new InvalidOperationException(TracedObject + "failed to register in rendezvous service.");
                }

                return aResponse.MessageData[0];
            }
        }

        public string[] GetAddresses(string rendezvousId)
        {
            using (EneterTrace.Entering())
            {
                RendezvousMessage aRequest = new RendezvousMessage();
                aRequest.MessageType = ERendezvousMessage.GetAddressRequest;
                aRequest.MessageData = new string[] { rendezvousId };

                RendezvousMessage aResponse = myRendezvousSender.SendRequestMessage(aRequest);

                if (aResponse.MessageType != ERendezvousMessage.AddressResponse)
                {
                    throw new InvalidOperationException(TracedObject + "failed to register in rendezvous service.");
                }

                return aResponse.MessageData;
            }
        }
        

        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myRendezvousSender.AttachDuplexOutputChannel(duplexOutputChannel);
            }
        }

        public void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                myRendezvousSender.DetachDuplexOutputChannel();
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get { return myRendezvousSender.IsDuplexOutputChannelAttached; }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel
        {
            get { return myRendezvousSender.AttachedDuplexOutputChannel; }
        }


        private ISyncDuplexTypedMessageSender<RendezvousMessage, RendezvousMessage> myRendezvousSender;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}
